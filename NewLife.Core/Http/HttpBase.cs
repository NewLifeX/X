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

    /// <summary>获取/设置 头部。获取时若键不存在返回空字符串（而非null）</summary>
    /// <param name="key">头部名称</param>
    /// <returns>头部值；键不存在时返回空字符串</returns>
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
    /// <summary>分析请求头。主体以共享切片截取，不释放入参</summary>
    /// <remarks>主体为入参数据包的共享切片（引用计数独立持有）；本方法不释放 <paramref name="pk"/>，由调用方负责释放。头部可跨节点（链式帧/跨接收段）：前缀与空行均跨段查找，跨段时物化头部区域后解析。</remarks>
    /// <param name="pk">数据包</param>
    /// <returns>是否解析成功</returns>
    public Boolean Parse(IPacket pk)
    {
        // 快速验证：第一行以请求谓语/响应版本开头（最多10字节内出现空格；跨段前缀拼读）
        Span<Byte> fastBuf = stackalloc Byte[10];
        if (!FastValidHeader(pk.GetPrefix(fastBuf, 10))) return false;

        // 查找首个空行（CRLFCRLF）。p 指向空行起始位置（跨段查找，返回全局偏移）
        var p = pk.IndexOf(NewLine2);
        if (p < 0) return false;

        // 头部区域：单段零拷贝直读；链式（头部跨节点）物化头部区，头部通常很小
        ReadOnlySpan<Byte> data;
        if (pk.Next == null)
            data = pk.GetSpan();
        else
            data = pk.ReadBytes(0, p);

        // 只取头部区域（不包含分隔空行），避免之前 (p+2) 的截取导致尾部半行进入解析产生潜在问题
        var header = data[..p];
        var firstLine = "";
        while (header.Length > 0)
        {
            var p2 = header.IndexOf(NewLine);
            //if (p2 < 0) break; // 不正常，但跳出以避免死循环

            // 最后一行没有末尾换行符
            var line = p2 < 0 ? header : header[..p2];
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

            if (p2 < 0 || p2 + 2 >= header.Length) break;
            header = header[(p2 + 2)..];
        }

        // 截取主体（跳过 CRLFCRLF 共4字节）：共享切片独立持有引用；入参句柄由调用方释放
        Body = pk.Slice(p + 4, -1);

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
    /// <remarks>数据来自缓冲池，使用者用完返回数据包后应该释放，以便把缓冲区放回池里。链式主体逐段写入，实际内容与声明长度一致。</remarks>
    /// <returns></returns>
    public virtual IOwnerPacket Build()
    {
        var body = Body;
        var len = body != null ? body.Total : 0;

        var header = BuildHeader(len);

        // 从内存池申请缓冲区，Slice后管理权转移，外部使用完以后释放
        //using var pk = new ArrayPacket(Encoding.UTF8.GetByteCount(header) + len);
        len += Encoding.UTF8.GetByteCount(header);
        var pk = new OwnerPacket(len);
        var writer = new SpanWriter(pk.GetSpan());

        writer.Write(header, -1);

        // 链式主体由 Write(IPacket) 逐段写入，无需聚合拷贝
        if (body != null) writer.Write(body);

        return pk.Resize(writer.Position);
    }

    /// <summary>仅创建头部封包（不含主体），流式发送时先发头部</summary>
    /// <param name="contentLength">主体长度；负数表示未知（调用方应先行设置 Transfer-Encoding 等头部）</param>
    /// <returns>头部数据包，调用方负责 Dispose</returns>
    /// <remarks>已知长度时自动写入 Content-Length；长度未知时由调用方设置 Transfer-Encoding: chunked 等头部。</remarks>
    public IOwnerPacket BuildHeaderPacket(Int64 contentLength = -1)
    {
        if (contentLength >= 0) Headers["Content-Length"] = contentLength.ToString();

        var header = BuildHeader(0);
        var bytes = Encoding.UTF8.GetBytes(header);
        var pk = new OwnerPacket(bytes.Length);
        bytes.CopyTo(pk.GetSpan());

        return pk;
    }

    /// <summary>创建头部</summary>
    /// <param name="length"></param>
    /// <returns></returns>
    protected abstract String BuildHeader(Int32 length);
    #endregion
}