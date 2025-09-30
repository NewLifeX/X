using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using NewLife.Collections;

namespace NewLife.IO;

/// <summary>轻量级Excel写入器，支持多个工作表</summary>
/// <remarks>
/// 目标：快速导出简单数据，支持多工作表的列头与多行数据；识别常见数据类型并使用合适样式，避免长数字（如身份证、长整型）被 Excel / WPS 显示为科学计数。
/// 仅生成最小必要的 xlsx 结构：ContentTypes / workbook / worksheets / styles / sharedStrings。
/// 不支持：合并单元格、富文本、超链接、公式等高级特性。读取可使用 <see cref="ExcelReader"/>。
/// </remarks>
public class ExcelWriter : DisposeBase
{
    #region 属性
    /// <summary>文件路径（Save 时写入）</summary>
    public String? FileName { get; }

    /// <summary>目标流（若提供则写入该流，调用方负责生命周期）</summary>
    public Stream? Stream { get; }

    /// <summary>默认工作表名称（当调用 API 未指定 sheet 时使用）</summary>
    public String SheetName { get; set; } = "Sheet1";

    /// <summary>文本编码</summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    // 多 sheet：保持插入顺序，写 workbook.xml 时用于 sheetId 顺序
    private readonly List<String> _sheetNames = [];
    private readonly Dictionary<String, List<String>> _sheetRows = new(StringComparer.OrdinalIgnoreCase); // sheet -> 行XML集合
    private readonly Dictionary<String, Int32> _sheetRowIndex = new(StringComparer.OrdinalIgnoreCase);     // sheet -> 当前行号（1基）

    private readonly Dictionary<String, Int32> _shared = new(StringComparer.Ordinal); // 共享字符串去重
    private Int32 _sharedCount; // 总引用次数（含重复）

    // 样式索引（cellXfs 顺序）—— 与 styles.xml 内顺序保持一致
    private const Int32 StyleGeneral = 0; // 通用
    private const Int32 StyleDate = 1;    // 日期 mm-dd-yy (14)
    private const Int32 StyleDateTime = 2; // 日期时间 m/d/yy h:mm (22)
    private const Int32 StyleTime = 3;    // 时间 h:mm:ss (21)
    private const Int32 StyleDecimal = 4; // 小数 0.00 (2)
    private const Int32 StylePercent = 5; // 百分比 0.00% (10)
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

        // 数据流时需要调用保存，文件则不需要
        if (Stream == null)
        {
            Save();
        }
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
    #endregion

    #region 内部写入
    private void EnsureSheet(String sheet)
    {
        if (!_sheetRows.ContainsKey(sheet))
        {
            _sheetRows[sheet] = [];
            _sheetRowIndex[sheet] = 0;
            _sheetNames.Add(sheet);
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
            var style = StyleGeneral;
            String? tAttr = null; // t="s" / "b"
            String? inner = null; // <v>值</v>

            switch (val)
            {
                case String str:
                    {
                        // 百分比：形如 "12.3%" / "45%"
                        if (str.Length > 0 && str.EndsWith("%") && TryParsePercent(str, out var pct))
                        {
                            style = StylePercent;
                            inner = (pct / 100).ToString("0.##########", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            // 纯数字且位数较长（>=12）或前导0，强制当文本，避免科学计数；身份证可能含 X -> 直接文本
                            if (NeedsText(str))
                            {
                                tAttr = "s"; // 共享字符串
                                inner = GetSharedStringIndex(str).ToString();
                            }
                            else
                            {
                                // 普通字符串走共享字符串，减少体积 & 避免被推断
                                tAttr = "s";
                                inner = GetSharedStringIndex(str).ToString();
                            }
                        }
                        break;
                    }
                case Boolean b:
                    {
                        tAttr = "b";
                        inner = b ? "1" : "0";
                        break;
                    }
                case DateTime dt:
                    {
                        // Excel 序列值：1=1900/1/1（含闰年Bug），读取时减2，这里写入需补2
                        var baseDate = new DateTime(1900, 1, 1);
                        var serial = (dt - baseDate).TotalDays + 2; // 包含时间小数
                        var hasTime = dt.TimeOfDay.TotalSeconds > 0.1;
                        style = hasTime ? StyleDateTime : StyleDate;
                        inner = serial.ToString("0.###############", CultureInfo.InvariantCulture);
                        break;
                    }
                case TimeSpan ts:
                    {
                        var serial = ts.TotalDays; // 纯时间按天小数
                        style = StyleTime;
                        inner = serial.ToString("0.###############", CultureInfo.InvariantCulture);
                        break;
                    }
                case Int16 or Int32 or Int64 or Byte or SByte or UInt16 or UInt32 or UInt64:
                    {
                        inner = Convert.ToString(val, CultureInfo.InvariantCulture);
                        break;
                    }
                case Decimal dec:
                    {
                        inner = dec.ToString(CultureInfo.InvariantCulture);
                        if (dec != Math.Truncate(dec)) style = StyleDecimal; // 有小数部分
                        break;
                    }
                case Double d:
                    {
                        inner = d.ToString("0.###############", CultureInfo.InvariantCulture);
                        if (Math.Abs(d - Math.Truncate(d)) > Double.Epsilon) style = StyleDecimal;
                        break;
                    }
                case Single f:
                    {
                        inner = f.ToString("0.###############", CultureInfo.InvariantCulture);
                        if (Math.Abs(f - Math.Truncate(f)) > Single.Epsilon) style = StyleDecimal;
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

            sb.Append("<c r=\"").Append(cellRef).Append('\"');
            if (tAttr != null) sb.Append(' ').Append("t=\"").Append(tAttr).Append('\"');

            // 若是非共享字符串/布尔（即 tAttr==null），统一写入样式属性以便读取端按样式解析类型（包括 General 情况）
            if (tAttr == null)
            {
                // 若此前标记 forceStyleAttr 或 style 非 General 已经满足，也一并覆盖式写入（不重复判断）
                sb.Append(' ').Append("s=\"").Append(style).Append('\"');
            }
            sb.Append("><v>").Append(inner).Append("</v></c>");
        }

        sb.Append("</row>");
        _sheetRows[sheet].Add(sb.Return(true));
    }

    private static Boolean TryParsePercent(String str, out Decimal value)
    {
        value = 0m;
        var txt = str.Trim().TrimEnd('%');
        if (Decimal.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            value = d;
            return true;
        }
        return false;
    }

    private static Boolean NeedsText(String str)
    {
        if (str.IsNullOrEmpty()) return true;
        // 只含数字且长度>=12 或 以0开头（不丢前导0）
        var allDigit = true;
        for (var i = 0; i < str.Length; i++)
        {
            if (str[i] < '0' || str[i] > '9') { allDigit = false; break; }
        }
        if (allDigit && (str.Length >= 12 || str[0] == '0')) return true;

        // 含非数字但整体应保持文本（如身份证最后一位 X/x）
        if (str.Length >= 15 && str.Any(e => e == 'X' || e == 'x')) return true;

        return false;
    }

    private Int32 GetSharedStringIndex(String str)
    {
        _sharedCount++;
        if (_shared.TryGetValue(str, out var idx)) return idx;
        idx = _shared.Count; // 新索引
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

        // [Content_Types].xml
        {
            using var sw = new StreamWriter(za.CreateEntry("[Content_Types].xml").Open(), Encoding);
            sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"xml\" ContentType=\"application/xml\"/>");
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
        {
            using var sw = new StreamWriter(za.CreateEntry("xl/workbook.xml").Open(), Encoding);
            sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheets>");
            for (var i = 0; i < _sheetNames.Count; i++)
            {
                var name = SecurityElement.Escape(_sheetNames[i]) ?? _sheetNames[i];
                sw.Write("<sheet name=\"");
                sw.Write(name);
                sw.Write("\" sheetId=\"");
                sw.Write(i + 1);
                sw.Write("\" r:id=\"rId");
                sw.Write(i + 1);
                sw.Write("\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"/>");
            }
            sw.Write("</sheets></workbook>");
        }

        // styles.xml
        {
            using var sw = new StreamWriter(za.CreateEntry("xl/styles.xml").Open(), Encoding);
            sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><cellXfs count=\"6\">");
            sw.Write("<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>"); // General
            sw.Write("<xf numFmtId=\"14\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>"); // Date
            sw.Write("<xf numFmtId=\"22\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>"); // DateTime
            sw.Write("<xf numFmtId=\"21\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>"); // 时间 h:mm:ss (21)
            sw.Write("<xf numFmtId=\"2\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>"); // Decimal
            sw.Write("<xf numFmtId=\"10\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>"); // Percent
            sw.Write("</cellXfs></styleSheet>");
        }

        // sharedStrings
        if (_shared.Count > 0)
        {
            using var sw = new StreamWriter(za.CreateEntry("xl/sharedStrings.xml").Open(), Encoding);
            sw.Write($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"{_sharedCount}\" uniqueCount=\"{_shared.Count}\">");
            foreach (var kv in _shared.OrderBy(e => e.Value))
            {
                var txt = SecurityElement.Escape(kv.Key) ?? String.Empty;
                sw.Write("<si><t>");
                sw.Write(txt);
                sw.Write("</t></si>");
            }
            sw.Write("</sst>");
        }

        // 每个 sheetX.xml
        for (var i = 0; i < _sheetNames.Count; i++)
        {
            var entry = za.CreateEntry("xl/worksheets/sheet" + (i + 1) + ".xml");
            using var sw = new StreamWriter(entry.Open(), Encoding);
            sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            var sheet = _sheetNames[i];
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
