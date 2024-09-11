using System.Text;
using NewLife.Buffers;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Http;

/// <summary>Http请求响应基类</summary>
public abstract class HttpBase : IDisposable
{
    #region 属性
    /// <summary>协议版本</summary>
    public String Version { get; set; } = "1.1";

    /// <summary>内容长度</summary>
    public Int32 ContentLength { get; set; } = -1;

    /// <summary>内容类型</summary>
    public String? ContentType { get; set; }

    /// <summary>请求或响应的主体部分</summary>
    public IPacket? Body { get; set; }

    /// <summary>主体长度</summary>
    public Int32 BodyLength => Body == null ? 0 : Body.Total;

    /// <summary>是否已完整。头部未指定长度，或指定长度后内容已满足</summary>
    public Boolean IsCompleted => ContentLength < 0 || ContentLength <= BodyLength;

    /// <summary>头部集合</summary>
    public IDictionary<String, String> Headers { get; set; } = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);

    /// <summary>获取/设置 头部</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public String this[String key] { get => Headers[key] + ""; set => Headers[key] = value; }
    #endregion

    #region 构造
    /// <summary>释放</summary>
    public void Dispose() => Body.TryDispose();
    #endregion

    #region 解析
    /// <summary>快速验证协议头，剔除非HTTP协议。仅排除，验证通过不一定就是HTTP协议</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Boolean FastValidHeader(ReadOnlySpan<Byte> data)
    {
        // 性能优化，Http头部第一行以请求谓语或响应版本开头，然后是一个空格。最长谓语Options/Connect，版本HTTP/1.1，不超过10个字符
        if (data.Length > 10) data = data[..10];
        var p = data.IndexOf((Byte)' ');
        return p >= 0;
    }

    private static readonly Byte[] NewLine = [(Byte)'\r', (Byte)'\n'];
    private static readonly Byte[] NewLine2 = [(Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n'];
    /// <summary>分析请求头。截取Body时获取缓冲区所有权</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public Boolean Parse(IPacket pk)
    {
        var data = pk.GetSpan();
        if (!FastValidHeader(data)) return false;

        // 识别整个请求头
        var p = data.IndexOf(NewLine2);
        if (p < 0) return false;

        // 分析头部
        var header = data[..(p + 2)];
        var firstLine = "";
        while (true)
        {
            var p2 = header.IndexOf(NewLine);
            if (p2 < 0) break;

            var line = header[..p2];
            if (firstLine.IsNullOrEmpty())
                firstLine = line.ToStr();
            else
            {
                var p3 = line.IndexOf((Byte)':');
                if (p3 > 0)
                {
                    var name = line[..p3].Trim((Byte)' ').ToStr();
                    var value = line[(p3 + 1)..].Trim((Byte)' ').ToStr();
                    Headers[name] = value;
                }
            }

            if (p2 + 2 == header.Length) break;
            header = header[(p2 + 2)..];
        }

        //var str = pk.ReadBytes(0, p).ToStr();

        // 截取主体，获取所有权
        //var lines = str.Split("\r\n");
        Body = pk.Slice(p + 4);

        //// 分析头部
        //for (var i = 1; i < lines.Length; i++)
        //{
        //    var line = lines[i];
        //    p = line.IndexOf(':');
        //    if (p > 0) Headers[line[..p]] = line[(p + 1)..].Trim();
        //}

        ContentLength = Headers["Content-Length"].ToInt(-1);
        ContentType = Headers["Content-Type"];

        // 分析第一行
        if (!OnParse(firstLine)) return false;

        return true;
    }

    /// <summary>分析第一行</summary>
    /// <param name="firstLine"></param>
    protected abstract Boolean OnParse(String firstLine);
    #endregion

    #region 读写
    /// <summary>创建请求响应包</summary>
    /// <remarks>数据来自缓冲池，使用者用完返回数据包后应该释放，以便把缓冲区放回池里</remarks>
    /// <returns></returns>
    public virtual IOwnerPacket Build()
    {
        var body = Body;
        var len = body != null ? body.Total : 0;

        var header = BuildHeader(len);

        // 从内存池申请缓冲区，Slice后管理权转移，外部使用完以后释放
        using var pk = new ArrayPacket(Encoding.UTF8.GetByteCount(header) + len);
        var writer = new SpanWriter(pk.GetSpan());

        //BuildHeader(writer, len);
        writer.Write(header);

        if (body != null) writer.Write(body.GetSpan());

        return pk.Slice(0, writer.Position);

        //var header = BuildHeader(len);
        //var rs = new Packet(header.GetBytes())
        //{
        //    Next = data
        //};

        //return rs;
    }

    /// <summary>创建头部</summary>
    /// <param name="length"></param>
    /// <returns></returns>
    protected abstract String BuildHeader(Int32 length);
    #endregion
}