using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace NewLife.IO;

/// <summary>轻量级Excel读取器，仅用于导入数据</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/excel_reader
/// 仅支持xlsx格式，本质上是压缩包，内部xml。
/// 可根据xml格式扩展读取自己想要的内容。
/// 本类做了最小化实现，仅解析共享字符串、样式与工作表数据。
/// </remarks>
public class ExcelReader : DisposeBase
{
    #region 属性
    /// <summary>文件名</summary>
    public String? FileName { get; }

    /// <summary>工作表集合（键为工作表名称）</summary>
    public ICollection<String>? Sheets => _entries?.Keys;

    private ZipArchive _zip;
    private String[]? _sharedStrings;
    private ExcelNumberFormat?[]? _styles;
    private IDictionary<String, ZipArchiveEntry>? _entries;
    #endregion

    #region 构造
    /// <summary>实例化读取器</summary>
    /// <param name="fileName">Excel文件路径（xlsx）</param>
    public ExcelReader(String fileName)
    {
        if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(fileName));

        FileName = fileName;

        //_zip = ZipFile.OpenRead(fileName.GetFullPath());
        // 共享访问，避免文件被其它进程打开时再次访问抛出异常
        var fs = new FileStream(fileName.GetFullPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _zip = new ZipArchive(fs, ZipArchiveMode.Read, true);

        Parse();
    }

    /// <summary>实例化读取器</summary>
    /// <param name="stream">Excel数据流（需保持可读，调用方负责其生命周期）</param>
    /// <param name="encoding">压缩文件内各xml条目的编码（一般为UTF-8）</param>
    public ExcelReader(Stream stream, Encoding encoding)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        if (stream is FileStream fs) FileName = fs.Name;

        _zip = new ZipArchive(stream, ZipArchiveMode.Read, true, encoding);

        Parse();
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _entries?.Clear();
        _zip?.Dispose();
    }
    #endregion

    #region 方法
    private void Parse()
    {
        // 读取共享字符串（可缺失）
        {
            var entry = _zip.GetEntry("xl/sharedStrings.xml");
            if (entry != null) _sharedStrings = ReadStrings(entry.Open());
        }

        // 读取样式（包含内置 & 自定义数字格式）
        {
            var entry = _zip.GetEntry("xl/styles.xml");
            if (entry != null) _styles = ReadStyles(entry.Open());
        }

        // 读取sheet条目索引
        {
            _entries = ReadSheets(_zip);
        }
    }

    private static DateTime _1900 = new(1900, 1, 1);

    /// <summary>逐行读取数据，第一行通常是表头。支持超过26列（AA/AB等）以及缺失列自动补 null。</summary>
    /// <param name="sheet">工作表名。默认 null 取第一个数据表</param>
    /// <returns>按行返回对象数组。根据样式尝试转换为 DateTime / TimeSpan / 数值 / 布尔，否则为字符串</returns>
    public IEnumerable<Object?[]> ReadRows(String? sheet = null)
    {
        ThrowIfDisposed();

        if (Sheets == null || _entries == null) yield break;

        if (sheet.IsNullOrEmpty()) sheet = Sheets.FirstOrDefault();
        if (sheet.IsNullOrEmpty()) throw new ArgumentNullException(nameof(sheet));

        if (!_entries.TryGetValue(sheet, out var entry)) throw new ArgumentOutOfRangeException(nameof(sheet), "Unable to find worksheet");

        var doc = XDocument.Load(entry.Open());
        if (doc.Root == null) yield break;

        var data = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName.EqualIgnoreCase("sheetData"));
        if (data == null) yield break;

        // 加快样式判断速度
        var styles = _styles;
        if (styles != null && styles.Length == 0) styles = null;

        foreach (var row in data.Elements())
        {
            var vs = new List<Object?>();
            var curIndex = 0; // 当前列（0基）
            foreach (var col in row.Elements())
            {
                // 单元格引用。例如 A1 / AB23
                var r = col.Attribute("r")?.Value;
                if (!r.IsNullOrEmpty())
                {
                    var targetIndex = GetColumnIndex(r!); // 0基
                    // 补齐缺失列
                    while (curIndex < targetIndex)
                    {
                        vs.Add(null);
                        curIndex++;
                    }
                }

                // 默认原始值。优先取 <v> 子节点（统一行为），否则使用节点聚合值
                Object? val = null;
                var vNode = col.Elements().FirstOrDefault(e => e.Name.LocalName == "v");
                if (vNode != null)
                    val = vNode.Value;
                else
                    val = col.Value; // inlineStr 等情况会走这里

                // t=DataType: s=SharedString, b=Boolean, n=Number(默认), d=Date(较少出现), str=公式结果文本, inlineStr=内联字符串
                var t = col.Attribute("t")?.Value;
                if (t == "s")
                {
                    // 共享字符串
                    if (val is String s2 && Int32.TryParse(s2, out var sharedIndex)) val = _sharedStrings != null && sharedIndex >= 0 && sharedIndex < _sharedStrings.Length ? _sharedStrings[sharedIndex] : null;
                }
                else if (t == "b")
                {
                    // 布尔：0 / 1
                    if (val is String sb) val = sb == "1" || sb.EqualIgnoreCase("true");
                }
                else if (t == "inlineStr")
                {
                    // 已经在 col.Value 中
                }
                else if (t == "str")
                {
                    // 公式结果文本，不再特别处理
                }

                // 样式转换（日期 / 时间 / 数字）。仅当未被布尔/共享字符串提前转换
                if (val is String && styles != null)
                {
                    var sAttr = col.Attribute("s"); // StyleIndex
                    if (sAttr != null)
                    {
                        var si = sAttr.Value.ToInt();
                        if (si >= 0 && si < styles.Length)
                        {
                            // 按引用格式转换数值，没有引用格式时不转换
                            var st = styles[si];
                            if (st != null) val = ChangeType(val, st);
                        }
                    }
                }

                vs.Add(val);
                curIndex++; // 移动到下一列
            }

            yield return vs.ToArray();
        }
    }

    /// <summary>按 Excel 数字格式尝试转换值</summary>
    private Object? ChangeType(Object? val, ExcelNumberFormat st)
    {
        // 日期格式。Excel 以 1900-1-1 为基准(含虚构闰年Bug)，序列值 1 = 1900-01-01。这里减2 与历史实现保持兼容。
        if (st.Format.Contains("yy") || st.Format.Contains("mmm") || st.NumFmtId >= 14 && st.NumFmtId <= 17 || st.NumFmtId == 22)
        {
            if (val is String str && Double.TryParse(str, out var d))
            {
                // 暂时不明白为何要减2，实际上这么做就对了
                //val = _1900.AddDays(str.ToDouble() - 2);
                // 取整秒，剔除毫秒部分，避免浮点误差
                val = _1900.AddSeconds(Math.Round((d - 2) * 24 * 3600));
                //var ss = str.Split('.');
                //var dt = _1900.AddDays(ss[0].ToInt() - 2);
                //dt = dt.AddSeconds(ss[1].ToLong() / 115740);
                //val = dt.ToFullString();
            }
        }
        else if (st.NumFmtId is >= 18 and <= 21 or >= 45 and <= 47)
        {
            if (val is String str && Double.TryParse(str, out var d2))
            {
                val = TimeSpan.FromSeconds(Math.Round(d2 * 24 * 3600));
            }
        }
        // 自动处理0/General
        else if (st.NumFmtId == 0)
        {
            if (val is String str)
            {
                if (Int32.TryParse(str, out var n)) return n;
                if (Int64.TryParse(str, out var m)) return m;
                if (Decimal.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
                if (Double.TryParse(str, out var d2)) return d2;
            }
        }
        else if (st.NumFmtId is 1 or 3 or 37 or 38)
        {
            if (val is String str)
            {
                if (Int32.TryParse(str, out var n)) return n;
                if (Int64.TryParse(str, out var m)) return m;
            }
        }
        else if (st.NumFmtId is 2 or 4 or 11 or 39 or 40)
        {
            if (val is String str)
            {
                if (Decimal.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
                if (Double.TryParse(str, out var d2)) return d2;
            }
        }
        else if (st.NumFmtId is 9 or 10)
        {
            if (val is String str)
            {
                if (Double.TryParse(str, out var d2)) return d2;
            }
        }
        // 文本Text
        else if (st.NumFmtId == 49)
        {
            if (val is String str)
            {
                if (Decimal.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d.ToString();
                if (Double.TryParse(str, out var d2)) return d2.ToString();
            }
        }

        return val;
    }

    private String[]? ReadStrings(Stream ms)
    {
        var doc = XDocument.Load(ms);
        if (doc?.Root == null) return null;

        var list = new List<String>();
        foreach (var item in doc.Root.Elements())
        {
            list.Add(item.Value);
        }

        return list.ToArray();
    }

    private ExcelNumberFormat?[]? ReadStyles(Stream ms)
    {
        var doc = XDocument.Load(ms);
        if (doc?.Root == null) return null;

        // 内置默认样式
        var fmts = new Dictionary<Int32, String>
        {
            [0] = "General",
            [1] = "0",
            [2] = "0.00",
            [3] = "#,##0",
            [4] = "#,##0.00",
            [9] = "0%",
            [10] = "0.00%",
            [11] = "0.00E+00",
            [12] = "# ?/?",
            [13] = "# ??/??",
            [14] = "mm-dd-yy",
            [15] = "d-mmm-yy",
            [16] = "d-mmm",
            [17] = "mmm-yy",
            [18] = "h:mm AM/PM",
            [19] = "h:mm:ss AM/PM",
            [20] = "h:mm",
            [21] = "h:mm:ss",
            [22] = "m/d/yy h:mm",
            [37] = "#,##0 ;(#,##0)",
            [38] = "#,##0 ;[Red](#,##0)",
            [39] = "#,##0.00;(#,##0.00)",
            [40] = "#,##0.00;[Red](#,##0.00)",
            [45] = "mm:ss",
            [46] = "[h]:mm:ss",
            [47] = "mmss.0",
            [48] = "##0.0E+0",
            [49] = "@"
        };

        // 自定义样式
        var numFmts = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "numFmts");
        if (numFmts != null)
        {
            foreach (var item in numFmts.Elements())
            {
                var id = item.Attribute("numFmtId");
                var code = item.Attribute("formatCode");
                if (id != null && code != null) fmts[id.Value.ToInt()] = code.Value;
            }
        }

        var list = new List<ExcelNumberFormat?>();
        var xfs = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "cellXfs");
        if (xfs != null)
        {
            foreach (var item in xfs.Elements())
            {
                var fid = item.Attribute("numFmtId");
                if (fid == null) continue;

                var id = fid.Value.ToInt();
                if (fmts.TryGetValue(id, out var code))
                    list.Add(new ExcelNumberFormat(id, code));
                else
                    list.Add(null);
            }
        }

        return list.ToArray();
    }

    private IDictionary<String, ZipArchiveEntry> ReadSheets(ZipArchive zip)
    {
        var dic = new Dictionary<String, String?>();

        var entry = _zip.GetEntry("xl/workbook.xml");
        if (entry != null)
        {
            var doc = XDocument.Load(entry.Open());
            if (doc?.Root != null)
            {
                //var list = new List<String>();
                var sheets = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "sheets");
                if (sheets != null)
                {
                    foreach (var item in sheets.Elements())
                    {
                        var id = item.Attribute("sheetId");
                        var name = item.Attribute("name");
                        if (id != null) dic[id.Value] = name?.Value;
                    }
                }
            }
        }

        //_entries = _zip.Entries.Where(e =>
        //    e.FullName.StartsWithIgnoreCase("xl/worksheets/") &&
        //    e.Name.EndsWithIgnoreCase(".xml"))
        //    .ToDictionary(e => e.Name.TrimEnd(".xml"), e => e);

        var dic2 = new Dictionary<String, ZipArchiveEntry>();
        foreach (var item in zip.Entries)
        {
            if (item.FullName.StartsWithIgnoreCase("xl/worksheets/") && item.Name.EndsWithIgnoreCase(".xml"))
            {
                var name = item.Name.TrimEnd(".xml");
                if (dic.TryGetValue(name.TrimStart("sheet"), out var str)) name = str;
                name ??= String.Empty;

                dic2[name] = item;
            }
        }

        return dic2;
    }
    #endregion

    #region 辅助
    /// <summary>解析单元格引用（如 A1 / AB23）得到列索引（0基）。失败返回 0。</summary>
    private static Int32 GetColumnIndex(String cellRef)
    {
        // 提取前导字母部分
        var len = 0;
        for (var i = 0; i < cellRef.Length; i++)
        {
            var ch = cellRef[i];
            if (ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z') len++;
            else break;
        }
        if (len == 0) return 0;

        var index = 0;
        for (var i = 0; i < len; i++)
        {
            var ch = cellRef[i];
            if (ch is >= 'a' and <= 'z') ch = (Char)(ch - 'a' + 'A');
            index = index * 26 + (ch - 'A' + 1);
        }
        return index - 1; // 转为0基
    }
    #endregion

    #region 内嵌类
    class ExcelNumberFormat(Int32 numFmtId, String format)
    {
        public Int32 NumFmtId { get; set; } = numFmtId;
        public String Format { get; set; } = format;
    }
    #endregion
}
