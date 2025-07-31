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
/// </remarks>
public class ExcelReader : DisposeBase
{
    #region 属性
    /// <summary>文件名</summary>
    public String? FileName { get; }

    /// <summary>工作表</summary>
    public ICollection<String>? Sheets => _entries?.Keys;

    private ZipArchive _zip;
    private String[]? _sharedStrings;
    private ExcelNumberFormat?[]? _styles;
    private IDictionary<String, ZipArchiveEntry>? _entries;
    #endregion

    #region 构造
    /// <summary>实例化读取器</summary>
    /// <param name="fileName"></param>
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
    /// <param name="stream"></param>
    /// <param name="encoding"></param>
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
        _zip.TryDispose();
    }
    #endregion

    #region 方法
    private void Parse()
    {
        // 读取共享字符串
        {
            var entry = _zip.GetEntry("xl/sharedStrings.xml");
            if (entry != null) _sharedStrings = ReadStrings(entry.Open());
        }

        // 读取样式
        {
            var entry = _zip.GetEntry("xl/styles.xml");
            if (entry != null) _styles = ReadStyles(entry.Open());
        }

        // 读取sheet
        {
            _entries = ReadSheets(_zip);
        }
    }

    private static DateTime _1900 = new(1900, 1, 1);

    /// <summary>逐行读取数据，第一行很可能是表头</summary>
    /// <param name="sheet">工作表名。一般是sheet1/sheet2/sheet3，默认空，使用第一个数据表</param>
    /// <returns></returns>
    public IEnumerable<Object?[]> ReadRows(String? sheet = null)
    {
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
            var c = 'A';
            foreach (var col in row.Elements())
            {
                // 值
                Object? val = col.Value;

                // 某些列没有数据，被跳过。r=CellReference
                var r = col.Attribute("r");
                if (r != null)
                {
                    // 按最后一个字母递增，最多支持25个空列
                    var c2 = r.Value.Last(Char.IsLetter);
                    while (c2 != c)
                    {
                        vs.Add(null);
                        if (c == 'Z')
                            c = 'A';
                        else
                            c++;
                    }
                }

                // t=DataType, s=SharedString, b=Boolean, n=Number, d=Date
                var t = col.Attribute("t");
                if (t != null && t.Value == "s")
                {
                    val = _sharedStrings?[val.ToInt()];
                }
                else if (styles != null)
                {
                    // 特殊支持时间日期，s=StyleIndex
                    var s = col.Attribute("s");
                    if (s != null)
                    {
                        var si = s.Value.ToInt();
                        if (si < styles.Length)
                        {
                            // 按引用格式转换数值，没有引用格式时不转换
                            var st = styles[si];
                            if (st != null) val = ChangeType(val, st);
                        }
                        else
                        {
                            foreach (var colElement in col.Elements())
                            {
                                if (colElement.Name.LocalName.Equals("v"))
                                {
                                    val = colElement.Value;
                                }
                            }
                        }
                    }
                }

                vs.Add(val);

                // 循环判断，用最简单的办法兼容超过26列的表格
                if (c == 'Z')
                    c = 'A';
                else
                    c++;
            }

            yield return vs.ToArray();
        }
    }

    private Object? ChangeType(Object? val, ExcelNumberFormat st)
    {
        // 日期格式。1900-1-1依赖的天数，例如1900-1-1时为1
        if (st.Format.Contains("yy") || st.Format.Contains("mmm") || st.NumFmtId >= 14 && st.NumFmtId <= 17 || st.NumFmtId == 22)
        {
            if (val is String str)
            {
                // 暂时不明白为何要减2，实际上这么做就对了
                //val = _1900.AddDays(str.ToDouble() - 2);
                // 取整，剔除毫秒部分
                val = _1900.AddSeconds(Math.Round((str.ToDouble() - 2) * 24 * 3600));
                //var ss = str.Split('.');
                //var dt = _1900.AddDays(ss[0].ToInt() - 2);
                //dt = dt.AddSeconds(ss[1].ToLong() / 115740);
                //val = dt.ToFullString();
            }
        }
        else if (st.NumFmtId is >= 18 and <= 21 or >= 45 and <= 47)
        {
            if (val is String str)
            {
                val = TimeSpan.FromSeconds(Math.Round(str.ToDouble() * 24 * 3600));
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

    #region 内嵌类
    class ExcelNumberFormat(Int32 numFmtId, String format)
    {
        public Int32 NumFmtId { get; set; } = numFmtId;
        public String Format { get; set; } = format;
    }
    #endregion
}
