using System.Text;
using NewLife.Collections;

namespace NewLife.IO;

/// <summary>Csv文件</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/csv_file
/// 支持整体读写以及增量式读写，目标是读写超大Csv文件。
/// 读取解析实现遵循 RFC4180 基本规则：
/// 1. 字段之间使用 <see cref="Separator"/> 分隔；
/// 2. 含分隔符、换行、双引号的字段使用双引号包裹；
/// 3. 字段内的双引号以两个双引号转义；
/// 4. 允许字段内出现换行（位于成对引号内）。
/// 旧版本按行 ReadLine + Split 方式无法正确处理含分隔符/换行的被引号包裹字段，现已改为流式逐字符状态机解析。
/// </remarks>
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
public class CsvFile : IDisposable, IAsyncDisposable
#else
public class CsvFile : IDisposable
#endif
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
    /// <param name="file">文件路径</param>
    /// <param name="write">是否写入模式；写入模式用 <see cref="FileAccess.ReadWrite"/> 打开，不自动截断</param>
    public CsvFile(String file, Boolean write = false)
    {
        file = file.GetFullPath();
        if (write)
            _stream = new FileStream(file.EnsureDirectory(true), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        else
            _stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    private Boolean _disposed;
    /// <summary>销毁</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(Boolean disposing)
    {
        if (_disposed) return;
        _disposed = true;

        // 必须刷新写入器，否则可能丢失一截数据
        _writer?.Flush();

        if (!_leaveOpen && _stream != null)
        {
            _reader?.Dispose();

            _writer?.Dispose();

            _stream.Close();
        }
    }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    /// <summary>异步销毁</summary>
    /// <returns></returns>
    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // 必须刷新写入器，否则可能丢失一截数据
        if (_writer != null) await _writer.FlushAsync().ConfigureAwait(false);

        if (!_leaveOpen && _stream != null)
        {
            _reader?.Dispose();

            if (_writer != null) await _writer.DisposeAsync().ConfigureAwait(false);

            await _stream.DisposeAsync().ConfigureAwait(false);
        }

        GC.SuppressFinalize(this);
    }
#endif
    #endregion

    #region 读取
    private Int32 _columnCount; // 首行列数，用于后续可能的列数校验（保持向后兼容）
    private StreamReader? _reader;

    /// <summary>读取一行（一个记录/Record）</summary>
    /// <remarks>
    /// 使用逐字符状态机解析，正确处理：
    /// - 被引号包裹且内部含分隔符/CRLF
    /// - 转义双引号 "" -> "
    /// - 尾部空字段与空行
    /// EOF 返回 null。
    /// </remarks>
    /// <returns>字段数组；EOF 返回 null</returns>
    public String[]? ReadLine()
    {
        EnsureReader();
        if (_reader == null) return null;

        var fields = ReadRecord();
        if (fields == null) return null;

        // 记录首行列数，仅在首行赋值，向后兼容旧逻辑（不强制验证）
        if (_columnCount == 0 && fields.Length > 0) _columnCount = fields.Length;

        return fields;
    }

    /// <summary>读取所有行</summary>
    /// <returns>枚举器</returns>
    public IEnumerable<String[]> ReadAll()
    {
        while (true)
        {
            var line = ReadLine();
            if (line == null) break;

            yield return line;
        }
    }

    /// <summary>核心逐字符解析。返回一条记录（字段集合）</summary>
    /// <returns></returns>
    private String[]? ReadRecord()
    {
        // EOF 情况：若尚未读取任何字符则返回 null
        var reader = _reader!;

        var fields = new List<String>();
        var sb = Pool.StringBuilder.Get();
        var inQuotes = false;   // 当前是否位于字段引号内
        var firstCharInField = true; // 用于识别字段起始的引号
        var anyChar = false;    // 本记录是否读取过任何字符

        while (true)
        {
            var c = reader.Read();
            if (c == -1)
            {
                // EOF
                if (!anyChar)
                {
                    sb.Return();
                    return null; // 完全没有数据
                }
                // 结束最后一个字段
                fields.Add(sb.Return(true));
                break;
            }
            anyChar = true;
            var ch = (Char)c;

            if (inQuotes)
            {
                if (ch == '"')
                {
                    // 可能的转义或结束
                    var next = reader.Peek();
                    if (next == '"')
                    {
                        reader.Read(); // 消费第二个引号
                        sb.Append('"');
                    }
                    else
                    {
                        // 结束引号字段
                        inQuotes = false;
                        firstCharInField = false; // 字段已结束，引号后可能跟分隔符
                    }
                }
                else
                {
                    sb.Append(ch);
                }
                continue;
            }

            // 不在引号内
            if (firstCharInField && ch == '"')
            {
                inQuotes = true;
                firstCharInField = false;
                continue;
            }

            if (ch == Separator)
            {
                fields.Add(sb.Return(true));
                sb = Pool.StringBuilder.Get();
                firstCharInField = true;
                continue;
            }

            if (ch == '\r')
            {
                // 兼容 CRLF。若下一个是 \n 则消费。
                if (reader.Peek() == '\n') reader.Read();
                fields.Add(sb.Return(true));
                break;
            }
            if (ch == '\n')
            {
                fields.Add(sb.Return(true));
                break;
            }

            sb.Append(ch);
            firstCharInField = false;
        }

        return fields.ToArray();
    }

    private void EnsureReader()
    {
        // detectEncodingFromByteOrderMarks = true（默认），保持原行为
        _reader ??= new StreamReader(_stream, Encoding);
    }
    #endregion

    #region 写入
    /// <summary>写入全部</summary>
    /// <param name="data">数据集合</param>
    public void WriteAll(IEnumerable<IEnumerable<Object?>> data)
    {
        foreach (var line in data)
        {
            WriteLine(line);
        }
    }

    /// <summary>写入一行</summary>
    /// <param name="line">字段集合</param>
    public void WriteLine(IEnumerable<Object?> line)
    {
        EnsureWriter();

        if (_writer == null) throw new ArgumentNullException(nameof(_writer));

        var str = BuildLine(line);

        _writer.WriteLine(str);
    }

    /// <summary>写入一行</summary>
    /// <param name="values">字段列表</param>
    public void WriteLine(params Object[] values) => WriteLine(line: values);

    /// <summary>异步写入一行</summary>
    /// <param name="line">字段集合</param>
    public async Task WriteLineAsync(IEnumerable<Object> line)
    {
        EnsureWriter();

        if (_writer == null) throw new ArgumentNullException(nameof(_writer));

        var str = BuildLine(line);

        await _writer.WriteLineAsync(str).ConfigureAwait(false);
    }

    /// <summary>构建一行</summary>
    /// <param name="line">字段集合</param>
    /// <returns>CSV 格式化文本（不含行尾换行）</returns>
    protected virtual String BuildLine(IEnumerable<Object?> line)
    {
        var sb = Pool.StringBuilder.Get();

        foreach (var item in line)
        {
            if (sb.Length > 0) sb.Append(Separator);

            if (item is DateTime dt)
            {
                sb.Append(dt.ToFullString(""));
            }
            else if (item is Boolean b)
            {
                sb.Append(b ? "1" : "0");
            }
            else
            {
                if (item is not String str) str = item + "";

                // 避免出现科学计数问题 数据前增加制表符"\t"
                // 不同软件显示不太一样 wps超过9位就自动转为科学计数，有的软件是超过11位，所以采用最小范围9
                if (str.Length > 9 && Int64.TryParse(str, out _))
                {
                    sb.Append('\t');
                    sb.Append(str);
                }
                else
                {
                    // RFC4180：含 分隔符 / CR / LF / 双引号 时需要整体加双引号，内部双引号以两个双引号转义
                    var needQuote = str.IndexOfAny(new[] { Separator, '\r', '\n', '"' }) >= 0;
                    if (needQuote)
                    {
                        sb.Append('"');
                        if (str.Contains('"')) str = str.Replace("\"", "\"\"");
                        sb.Append(str);
                        sb.Append('"');
                    }
                    else
                    {
                        sb.Append(str);
                    }
                }
            }
        }

        return sb.Return(true);
    }

    private StreamWriter? _writer;
    private void EnsureWriter()
    {
        _writer ??= new StreamWriter(_stream, Encoding, 1024, _leaveOpen);
    }
    #endregion
}