using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Collections;

namespace NewLife.IO
{
    /// <summary>Csv文件</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/csv_file
    /// 支持整体读写以及增量式读写，目标是读写超大Csv文件
    /// </remarks>
    public class CsvFile : DisposeBase
    {
        #region 属性
        /// <summary>文件编码</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        private readonly Stream _stream;
        private readonly Boolean _leaveOpen;

        /// <summary>分隔符。默认逗号</summary>
        public Char Separator { get; set; } = ',';
        #endregion

        #region 构造
        /// <summary>数据流实例化</summary>
        /// <param name="stream"></param>
        public CsvFile(Stream stream) => _stream = stream;

        /// <summary>数据流实例化</summary>
        /// <param name="stream"></param>
        /// <param name="leaveOpen">保留打开</param>
        public CsvFile(Stream stream, Boolean leaveOpen)
        {
            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        /// <summary>Csv文件实例化</summary>
        /// <param name="file"></param>
        /// <param name="write"></param>
        public CsvFile(String file, Boolean write = false)
        {
            file = file.GetFullPath();
            if (write)
                _stream = new FileStream(file.EnsureDirectory(true), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            else
                _stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            // 必须刷新写入器，否则可能丢失一截数据
            _writer?.Flush();

            if (!_leaveOpen && _stream != null)
            {
                _reader.TryDispose();

                _writer.TryDispose();

                _stream.Close();
            }
        }
        #endregion

        #region 读取
        /// <summary>读取一行</summary>
        /// <returns></returns>
        public String[] ReadLine()
        {
            EnsureReader();

            var line = _reader.ReadLine();
            if (line == null) return null;

            var list = new List<String>();

            // 直接分解，引号合并
            var arr = line.Split(Separator);
            for (var i = 0; i < arr.Length; i++)
            {
                var str = (arr[i] + "").Trim();
                if (str.StartsWith("\""))
                {
                    var txt = "";
                    if (str.EndsWith("\"") && !str.EndsWith("\"\""))
                        txt = str.Trim('\"');
                    else
                    {
                        // 找到下一个以引号结尾的项
                        for (var j = i + 1; j < arr.Length; j++)
                        {
                            if (arr[j].EndsWith("\""))
                            {
                                txt = arr.Skip(i).Take(j - i + 1).Join(Separator + "").Trim('\"');

                                // 跳过去一大步
                                i = j;
                                break;
                            }
                        }
                    }

                    // 两个引号是一个引号的转义
                    txt = txt.Replace("\"\"", "\"");
                    list.Add(txt);
                }
                else
                    list.Add(str);
            }

            return list.ToArray();
        }

        /// <summary>读取所有行</summary>
        /// <returns></returns>
        public String[][] ReadAll()
        {
            var list = new List<String[]>();

            while (true)
            {
                var line = ReadLine();
                if (line == null) break;

                list.Add(line);
            }

            return list.ToArray();
        }

        private StreamReader _reader;
        private void EnsureReader()
        {
            if (_reader == null) _reader = new StreamReader(_stream, Encoding);
        }
        #endregion

        #region 写入
        /// <summary>写入全部</summary>
        /// <param name="data"></param>
        public void WriteAll(IEnumerable<IEnumerable<Object>> data)
        {
            foreach (var line in data)
            {
                WriteLine(line);
            }
        }

        /// <summary>写入一行</summary>
        /// <param name="line"></param>
        public void WriteLine(IEnumerable<Object> line)
        {
            EnsureWriter();

            var str = BuildLine(line);

            _writer.WriteLine(str);
        }

#if !NET4
        /// <summary>异步写入一行</summary>
        /// <param name="line"></param>
        public async Task WriteLineAsync(IEnumerable<Object> line)
        {
            EnsureWriter();

            var str = BuildLine(line);

            await _writer.WriteLineAsync(str);
        }
#endif

        /// <summary>构建一行</summary>
        /// <param name="line"></param>
        /// <returns></returns>
        protected virtual String BuildLine(IEnumerable<Object> line)
        {
            var sb = Pool.StringBuilder.Get();

            foreach (var item in line)
            {
                if (sb.Length > 0) sb.Append(Separator);

                var str = item switch
                {
                    String str2 => str2,
                    DateTime dt => dt.ToFullString(""),
                    Boolean b => b ? "1" : "0",
                    _ => item + "",
                };

                if (str.Contains("\""))
                    sb.AppendFormat("\"{0}\"", str.Replace("\"", "\"\""));
                else if (str.Contains(Separator) || str.Contains("\r") || str.Contains("\n"))
                    sb.AppendFormat("\"{0}\"", str);
                else
                    sb.Append(str);
            }

            return sb.Put(true);
        }

        private StreamWriter _writer;
        private void EnsureWriter()
        {
#if NET4
            if (_writer == null) _writer = new StreamWriter(_stream, Encoding);
#else
            if (_writer == null) _writer = new StreamWriter(_stream, Encoding, 1024, true);
#endif
        }
        #endregion
    }
}