#if !NET40
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NewLife.IO
{
    /// <summary>轻量级Excel读取器，仅用于导入数据</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/excel_reader
    /// 仅支持xlsx格式，本质上是压缩包，内部xml。
    /// 可根据xml格式扩展读取自己想要的内容。
    /// </remarks>
    public class ExcelReader : DisposeBase
    {
        #region 属性
        /// <summary>文件名</summary>
        public String FileName { get; }

        /// <summary>工作表</summary>
        public ICollection<String> Sheets => _entries.Keys;

        private ZipArchive _zip;
        private String[] _sharedStrings;
        private String[] _styles;
        private IDictionary<String, ZipArchiveEntry> _entries;
        #endregion

        #region 构造
        /// <summary>实例化读取器</summary>
        /// <param name="fileName"></param>
        public ExcelReader(String fileName)
        {
            if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(fileName));

            FileName = fileName;

            _zip = ZipFile.OpenRead(fileName.GetFullPath());

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
                _entries = _zip.Entries.Where(e =>
                    e.FullName.StartsWithIgnoreCase("xl/worksheets/") &&
                    e.Name.EndsWithIgnoreCase(".xml"))
                    .ToDictionary(e => e.Name.TrimEnd(".xml"), e => e);
            }
        }

        private static DateTime _1900 = new DateTime(1900, 1, 1);

        /// <summary>逐行读取数据，第一行很可能是表头</summary>
        /// <param name="sheet">工作表名。一般是sheet1/sheet2/sheet3，默认空，使用第一个数据表</param>
        /// <returns></returns>
        public IEnumerable<Object[]> ReadRows(String sheet = null)
        {
            if (sheet.IsNullOrEmpty()) sheet = Sheets.FirstOrDefault();
            if (!_entries.TryGetValue(sheet, out var entry)) throw new ArgumentOutOfRangeException(nameof(sheet), "找不到工作表");

            var doc = XDocument.Load(entry.Open());
            var data = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName.EqualIgnoreCase("sheetData"));

            foreach (var row in data.Elements())
            {
                var vs = new List<String>();
                var c = 'A';
                foreach (var col in row.Elements())
                {
                    // 值
                    var val = col.Value;

                    // 某些列没有数据，被跳过。r=CellReference
                    var r = col.Attribute("r");
                    if (r != null)
                    {
                        var c2 = r.Value[0];
                        while (c2 != c) { vs.Add(null); c++; }
                    }

                    // t=DataType, s=SharedString, b=Boolean, n=Number, d=Date
                    var t = col.Attribute("t");
                    if (t != null && t.Value == "s")
                        val = _sharedStrings[val.ToInt()];
                    else if (_styles != null)
                    {
                        // 特殊支持时间日期，s=StyleIndex
                        var s = col.Attribute("s");
                        if (s != null)
                        {
                            var si = s.Value.ToInt();
                            if (si < _styles.Length && _styles[si] != null && _styles[si].StartsWith("yy"))
                            {
                                if (val.Contains("."))
                                {
                                    var ss = val.Split(".");
                                    var dt = _1900.AddDays(ss[0].ToInt() - 2);
                                    dt = dt.AddSeconds(ss[1].ToLong() / 115740);
                                    val = dt.ToFullString();
                                }
                                else
                                {
                                    val = _1900.AddDays(val.ToInt() - 2).ToString("yyyy-MM-dd");
                                }
                            }
                        }
                    }

                    vs.Add(val);

                    c++;
                }

                yield return vs.ToArray();
            }
        }

        private String[] ReadStrings(Stream ms)
        {
            var doc = XDocument.Load(ms);

            var list = new List<String>();
            foreach (var item in doc.Root.Elements())
            {
                list.Add(item.Value);
            }

            return list.ToArray();
        }

        private String[] ReadStyles(Stream ms)
        {
            var doc = XDocument.Load(ms);

            var fmts = new Dictionary<Int32, String>();
            var numFmts = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "numFmts");
            foreach (var item in numFmts.Elements())
            {
                var id = item.Attribute("numFmtId").Value.ToInt();
                var code = item.Attribute("formatCode").Value;
                fmts.Add(id, code);
            }

            var list = new List<String>();
            var xfs = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "cellXfs");
            foreach (var item in xfs.Elements())
            {
                var fid = item.Attribute("numFmtId").Value.ToInt();
                if (fmts.TryGetValue(fid, out var code))
                    list.Add(code);
                else
                    list.Add(null);
            }

            return list.ToArray();
        }
        #endregion
    }
}
#endif