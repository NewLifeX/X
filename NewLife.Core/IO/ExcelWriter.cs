using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using NewLife.Collections;

namespace NewLife.IO;

/// <summary>轻量级Excel写入器，支持多个工作表</summary>
/// <remarks>
/// 目标：快速导出简单数据，支持多工作表的列头与多行数据；识别常见数据类型并使用合适样式，避免长数字（如身份证、长整型）被 Excel / WPS 显示为科学计数。
/// 仅生成最小必要的 xlsx 结构：ContentTypes / workbook / worksheets / styles / sharedStrings（以及规范要求的关系 _rels/.rels、xl/_rels/workbook.xml.rels）。
/// 不支持：合并单元格、富文本、超链接、公式等高级特性。读取可使用 <see cref="ExcelReader"/>。
/// </remarks>
public class ExcelWriter : DisposeBase
{
    #region 内部类型
    /// <summary>单元格样式（值为 Excel 内置 numFmtId）。</summary>
    private enum ExcelCellStyle : Int32
    {
        General = 0,  // General
        Integer = 1,  // 0 （整数，避免长整型使用科学计数）
        Decimal = 2,  // 0.00
        Percent = 10, // 0.00%
        Date = 14,    // mm-dd-yy
        Time = 21,    // h:mm:ss
        DateTime = 22 // m/d/yy h:mm
    }

    private static readonly ExcelCellStyle[] _cellStyles = (ExcelCellStyle[])Enum.GetValues(typeof(ExcelCellStyle));
    #endregion

    #region 属性
    /// <summary>文件路径（Save 时写入）</summary>
    public String? FileName { get; }

    /// <summary>目标流（若提供则写入该流，调用方负责生命周期）</summary>
    public Stream? Stream { get; }

    /// <summary>默认工作表名称（当调用 API 未指定 sheet 时使用）</summary>
    public String SheetName { get; set; } = "Sheet1";

    /// <summary>文本编码</summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>超过该数字有效位数阈值（或极小值有大量前导0小数）则写为文本以避免科学计数法。默认 11。</summary>
    private const Int32 LongNumberAsTextThreshold = 11;

    /// <summary>是否自动根据数据内容估算列宽，并写入 <c>&lt;cols&gt;</c> 来避免 WPS/Excel 出现########。默认 true。</summary>
    public Boolean AutoFitColumnWidth { get; set; } = true;

    // 多 sheet：保持插入顺序，写 workbook.xml 时用于 sheetId 顺序
    private readonly List<String> _sheetNames = [];
    private readonly Dictionary<String, List<String>> _sheetRows = new(StringComparer.OrdinalIgnoreCase); // sheet -> 行XML集合
    private readonly Dictionary<String, Int32> _sheetRowIndex = new(StringComparer.OrdinalIgnoreCase);     // sheet -> 当前行号（1基）

    // 每个 sheet 的列最大显示宽度（字符数估算），下标 0 基，对应 Excel 列 1 基
    private readonly Dictionary<String, List<Double>> _sheetColWidths = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<String, Int32> _shared = new(StringComparer.Ordinal); // 共享字符串去重
    private Int32 _sharedCount; // 总引用次数（含重复）
    #endregion

    #region 构造
    /// <summary>使用文件路径实例化写入器</summary>
    /// <param name="fileName">目标 xlsx 文件</param>
    public ExcelWriter(String fileName) => FileName = fileName.GetFullPath();

    /// <summary>使用外部流实例化写入器</summary>
    /// <param name="stream">目标可写流</param>
    public ExcelWriter(Stream stream) => Stream = stream ?? throw new ArgumentNullException(nameof(stream));

    /// <summary>销毁释放</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);
        if (Stream == null) Save();
    }
    #endregion

    #region 写入接口
    /// <summary>写入列头到指定工作表</summary>
    /// <param name="sheet">工作表名称（可空，空时使用 <see cref="SheetName"/>）</param>
    /// <param name="headers">列头文本集合</param>
    public void WriteHeader(String sheet, IEnumerable<String> headers)
    {
        if (sheet.IsNullOrEmpty()) sheet = SheetName;
        if (headers == null) throw new ArgumentNullException(nameof(headers));

        EnsureSheet(sheet);

        var arr = headers as String[] ?? headers.ToArray();
        AddRow(sheet, arr.Select(e => (Object?)e).ToArray());
    }

    /// <summary>写入多行数据到指定工作表</summary>
    /// <param name="sheet">工作表名称（可空，空时使用 <see cref="SheetName"/>）</param>
    /// <param name="data">数据集合，每行一个对象数组</param>
    public void WriteRows(String? sheet, IEnumerable<Object?[]> data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        if (sheet.IsNullOrEmpty())
            sheet = SheetName;
        else
            SheetName = sheet; // 同步默认值为最近使用

        EnsureSheet(sheet);

        foreach (var row in data)
        {
            AddRow(sheet, row);
        }
    }

    /// <summary>手工设置列宽（字符宽度，近似），0 基列序号。需在 Save 之前调用。</summary>
    public void SetColumnWidth(String? sheet, Int32 columnIndex, Double width)
    {
        if (columnIndex < 0) throw new ArgumentOutOfRangeException(nameof(columnIndex));
        if (sheet.IsNullOrEmpty()) sheet = SheetName;
        EnsureSheet(sheet!);

        var list = _sheetColWidths[sheet!];
        while (list.Count <= columnIndex) list.Add(0);
        if (width > list[columnIndex]) list[columnIndex] = width;
    }
    #endregion

    #region 内部写入
    private void EnsureSheet(String sheet)
    {
        if (!_sheetRows.ContainsKey(sheet))
        {
            _sheetRows[sheet] = [];
            _sheetRowIndex[sheet] = 0;
            _sheetNames.Add(sheet);
            _sheetColWidths[sheet] = [];
        }
    }

    private void AddRow(String sheet, Object?[]? values)
    {
        EnsureSheet(sheet);

        var rowIndex = ++_sheetRowIndex[sheet];
        values ??= [];

        var sb = Pool.StringBuilder.Get();
        sb.Append("<row r=\"").Append(rowIndex).Append("\">");

        for (var i = 0; i < values.Length; i++)
        {
            var val = values[i];
            if (val == null) continue; // 缺失列：解析时自动补 null

            var cellRef = GetColumnName(i) + rowIndex; // A1 / B2 ...

            // 识别类型
            var style = ExcelCellStyle.General;
            String? tAttr = null; // t="s" / "b"
            String? inner = null; // <v>值</v>
            var displayLen = 0;   // 估算显示长度用于列宽

            switch (val)
            {
                case String str:
                    {
                        // 百分比：形如 "12.3%" / "45%"
                        if (str.Length > 0 && str.EndsWith("%") && TryParsePercent(str, out var pct))
                        {
                            style = ExcelCellStyle.Percent;
                            inner = (pct / 100).ToString("0.##########", CultureInfo.InvariantCulture);
                            //displayLen = inner.Length + 1;
                            break;
                        }
                        else
                        {
                            // 普通字符串走共享字符串，减少体积 & 避免被推断
                            tAttr = "s";
                            inner = GetSharedStringIndex(str).ToString();
                        }
                        break;
                    }
                case Boolean b:
                    {
                        tAttr = "b";
                        inner = b ? "1" : "0";
                        //displayLen = 5;
                        break;
                    }
                case DateTime dt:
                    {
                        var baseDate = new DateTime(1900, 1, 1);
                        if (dt < baseDate)
                        {
                            // Excel 无法表示 1900-01-01 之前（或无效）日期，这里写入空字符串
                            tAttr = "s";
                            inner = GetSharedStringIndex(String.Empty).ToString();
                            break;
                        }
                        // Excel 序列值：1=1900/1/1（含闰年Bug），读取时减2，这里写入需补2
                        var serial = (dt - baseDate).TotalDays + 2; // 包含时间小数
                        var hasTime = dt.TimeOfDay.Ticks != 0;
                        style = hasTime ? ExcelCellStyle.DateTime : ExcelCellStyle.Date;
                        inner = serial.ToString("0.###############", CultureInfo.InvariantCulture);
                        // 为避免 WPS 显示 ########，这里按常见完整格式长度估算：yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss
                        //displayLen = hasTime ? 16 - 1 : 10 - 1;
                        displayLen = hasTime ? 14 : 0;
                        break;
                    }
                case TimeSpan ts:
                    style = ExcelCellStyle.Time;
                    inner = ts.TotalDays.ToString("0.###############", CultureInfo.InvariantCulture);
                    //displayLen = inner.Length;
                    break;
                case Int16 or Int32 or Int64 or Byte or SByte or UInt16 or UInt32 or UInt64:
                    {
                        // 如果太长，为了避免出现科学计数法，改用字符串表示
                        var numStr = Convert.ToString(val, CultureInfo.InvariantCulture)!;
                        if (ShouldWriteAsText(numStr, 15))
                        {
                            tAttr = "s";
                            inner = GetSharedStringIndex(numStr).ToString();
                        }
                        else
                        {
                            style = ExcelCellStyle.Integer;
                            inner = numStr; // 使用 General，避免两位截断
                        }
                        displayLen = numStr.Length < 8 ? 0 : numStr.Length;
                        break;
                    }
                case Decimal dec:
                    {
                        var numStr = dec.ToString(CultureInfo.InvariantCulture);
                        if (ShouldWriteAsText(numStr, LongNumberAsTextThreshold))
                        {
                            tAttr = "s";
                            inner = GetSharedStringIndex(numStr).ToString();
                        }
                        else
                        {
                            inner = numStr; // 使用 General，避免两位截断
                        }
                        displayLen = numStr.Length < 8 ? 0 : numStr.Length;
                        break;
                    }
                case Double d:
                    {
                        var numStr = d.ToString("0.###############", CultureInfo.InvariantCulture);
                        if (ShouldWriteAsText(numStr, LongNumberAsTextThreshold))
                        {
                            tAttr = "s";
                            inner = GetSharedStringIndex(numStr).ToString();
                        }
                        else
                        {
                            inner = numStr; // General
                        }
                        displayLen = numStr.Length < 8 ? 0 : numStr.Length;
                        break;
                    }
                case Single f:
                    {
                        var numStr = f.ToString("0.###############", CultureInfo.InvariantCulture);
                        if (ShouldWriteAsText(numStr, LongNumberAsTextThreshold))
                        {
                            tAttr = "s";
                            inner = GetSharedStringIndex(numStr).ToString();
                        }
                        else
                        {
                            inner = numStr; // General
                        }
                        displayLen = numStr.Length < 8 ? 0 : numStr.Length;
                        break;
                    }
                default:
                    {
                        // 其它类型调用 ToString() 后按字符串处理
                        var str = val + "";
                        tAttr = "s";
                        inner = GetSharedStringIndex(str).ToString();
                        break;
                    }
            }

            sb.Append("<c r=\"").Append(cellRef).Append('"');
            if (tAttr != null) sb.Append(' ').Append("t=\"").Append(tAttr).Append('"');

            // 若是非共享字符串/布尔（即 tAttr==null），统一写入样式属性（General / 日期/时间等）
            if (tAttr == null)
            {
                // 依据枚举数值升序确定索引（反射生成 styles.xml 时使用相同顺序）
                var index = Array.IndexOf(_cellStyles, style);
                sb.Append(' ').Append("s=\"").Append(index).Append('"');
            }
            sb.Append("><v>").Append(inner).Append("</v></c>");

            // 自动列宽
            if (AutoFitColumnWidth && displayLen > 0)
            {
                var list = _sheetColWidths[sheet];
                while (list.Count <= i) list.Add(0);
                // Excel 列宽：字符数 + 2 边距（粗略），限制最大值适度（如 80）
                var w = displayLen + 2; // 经验值
                if (w > 80) w = 80;
                if (w > list[i]) list[i] = w;
            }
        }

        sb.Append("</row>");
        _sheetRows[sheet].Add(sb.Return(true));
    }

    /// <summary>判断一个数值字符串是否应转为文本以避免被 Excel 自动显示为科学计数法。</summary>
    private static Boolean ShouldWriteAsText(String numStr, Int32 maxLength)
    {
        if (numStr.IsNullOrEmpty()) return false;

        var digits = 0;
        for (var i = 0; i < numStr.Length; i++)
        {
            var ch = numStr[i];
            if (ch >= '0' && ch <= '9') digits++;
        }
        if (digits > maxLength) return true;         // 有效数字过长（>11）
        if (numStr.StartsWith("0.0000000")) return true;            // 很小的数值（大量前导0）
        return false;
    }

    private static Boolean TryParsePercent(String str, out Decimal value)
    {
        value = 0m;
        var txt = str.Trim().TrimEnd('%');
        if (Decimal.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) { value = d; return true; }
        return false;
    }

    private Int32 GetSharedStringIndex(String str)
    {
        _sharedCount++;
        if (_shared.TryGetValue(str, out var idx)) return idx;
        idx = _shared.Count;
        _shared[str] = idx;
        return idx;
    }

    private static String GetColumnName(Int32 index)
    {
        // 0 -> A
        index++; // 转为 1 基
        var sb = Pool.StringBuilder.Get();
        while (index > 0)
        {
            var mod = (index - 1) % 26;
            sb.Insert(0, (Char)('A' + mod));
            index = (index - 1) / 26;
        }
        return sb.Return(true);
    }
    #endregion

    #region 保存
    /// <summary>保存到文件或目标流</summary>
    public void Save()
    {
        // 若未写任何 sheet，创建一个空的默认工作表，避免生成非法 workbook
        if (_sheetNames.Count == 0) EnsureSheet(SheetName);

        var target = Stream;
        if (target == null)
        {
            if (FileName.IsNullOrEmpty()) throw new InvalidOperationException("未指定输出位置");

            var file = FileName.EnsureDirectory(true).GetFullPath();
            target = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        using var za = new ZipArchive(target, ZipArchiveMode.Create, leaveOpen: Stream != null, entryNameEncoding: Encoding);

        // _rels/.rels
        using (var sw = new StreamWriter(za.CreateEntry("_rels/.rels").Open(), Encoding))
        {
            sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
        }

        // [Content_Types].xml
        using (var sw = new StreamWriter(za.CreateEntry("[Content_Types].xml").Open(), Encoding))
        {
            sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"xml\" ContentType=\"application/xml\"/><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            sw.Write("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            for (var i = 0; i < _sheetNames.Count; i++)
            {
                sw.Write("<Override PartName=\"/xl/worksheets/sheet");
                sw.Write(i + 1);
                sw.Write(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            }
            if (_shared.Count > 0)
            {
                sw.Write("<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>");
            }
            sw.Write("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            sw.Write("</Types>");
        }

        // workbook.xml
        using (var sw = new StreamWriter(za.CreateEntry("xl/workbook.xml").Open(), Encoding))
        {
            sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
            for (var i = 0; i < _sheetNames.Count; i++)
            {
                var name = SecurityElement.Escape(_sheetNames[i]) ?? _sheetNames[i];
                sw.Write($"<sheet name=\"{name}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
            }
            sw.Write("</sheets></workbook>");
        }

        // workbook 关系
        using (var sw = new StreamWriter(za.CreateEntry("xl/_rels/workbook.xml.rels").Open(), Encoding))
        {
            sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (var i = 0; i < _sheetNames.Count; i++) sw.Write($"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
            var nextId = _sheetNames.Count + 1;
            sw.Write($"<Relationship Id=\"rId{nextId++}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
            if (_shared.Count > 0) sw.Write($"<Relationship Id=\"rId{nextId}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/>");
            sw.Write("</Relationships>");
        }

        // styles.xml （按枚举数值升序）
        using (var sw = new StreamWriter(za.CreateEntry("xl/styles.xml").Open(), Encoding))
        {
            sw.Write($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><cellXfs count=\"{_cellStyles.Length}\">");
            foreach (var st in _cellStyles)
            {
                sw.Write($"<xf numFmtId=\"{(Int32)st}\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>");
            }
            sw.Write("</cellXfs></styleSheet>");
        }

        // sharedStrings.xml
        if (_shared.Count > 0)
        {
            using var sw = new StreamWriter(za.CreateEntry("xl/sharedStrings.xml").Open(), Encoding);
            sw.Write($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"{_sharedCount}\" uniqueCount=\"{_shared.Count}\">");
            foreach (var kv in _shared.OrderBy(e => e.Value))
            {
                var txt = SecurityElement.Escape(kv.Key) ?? String.Empty;
                sw.Write("<si><t>");
                sw.Write(txt);
                sw.Write("</t></si>");
            }
            sw.Write("</sst>");
        }

        // worksheets
        for (var i = 0; i < _sheetNames.Count; i++)
        {
            var entry = za.CreateEntry($"xl/worksheets/sheet{i + 1}.xml");
            using var sw = new StreamWriter(entry.Open(), Encoding);
            sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" xmlns:x14=\"http://schemas.microsoft.com/office/spreadsheetml/2009/9/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:etc=\"http://www.wps.cn/officeDocument/2017/etCustomData\">");
            var sheet = _sheetNames[i];
            if (AutoFitColumnWidth && _sheetColWidths.TryGetValue(sheet, out var widths) && widths.Count > 0)
            {
                // 仅写入有值的列（>0）
                if (widths.Any(e => e > 0))
                {
                    sw.Write("<cols>");
                    for (var c = 0; c < widths.Count; c++)
                    {
                        var w = widths[c];
                        if (w <= 0) continue;
                        // Excel 列宽数值为字符宽度近似，可保留 2 位小数
                        sw.Write($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{w:0.##}\" customWidth=\"1\"/>");
                    }
                    sw.Write("</cols>");
                }
            }
            sw.Write("<sheetData>");
            if (_sheetRows.TryGetValue(sheet, out var list))
            {
                foreach (var r in list) sw.Write(r);
            }
            sw.Write("</sheetData></worksheet>");
        }

        target.Flush();
    }
    #endregion
}
