using System.Net;
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

    /// <summary>Content-Length 头存在但非法（无法解析、为负或超出 Int32 范围）</summary>
    /// <remarks>此类报文应按协议错误拒绝（请求回 400），不能按“无体”继续处理，否则连接上请求边界失步</remarks>
    public Boolean InvalidContentLength { get; protected set; }

    /// <summary>内容类型</summary>
    public String? ContentType { get; set; }

    /// <summary>请求或响应的主体部分</summary>
    public IPacket? Body { get; set; }

    /// <summary>主体长度</summary>
    public Int32 BodyLength => Body == null ? 0 : Body.Total;

    /// <summary>是否已完整。头部未指定长度，或指定长度后内容已满足</summary>
    public Boolean IsCompleted => ContentLength < 0 || ContentLength <= BodyLength;

    /// <summary>本次解析消耗的字节数。为头部（含分隔空行）加上实体的长度</summary>
    /// <remarks>
    /// <para>HTTP/1.1 允许流水线（一次发送多个请求），调用方据此切出同一轮数据中其后多出的字节继续解析，不能丢弃。</para>
    /// <para>未声明 Content-Length 时，头部之后的可用字节按实体处理（兼容直接发体的客户端），消耗量即整包长度。</para>
    /// </remarks>
    internal Int32 ConsumedLength { get; private set; }

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

    /// <summary>是否严格校验头部行：拒绝字段名含空白、以及以空白开头的续行折叠（obs-fold）</summary>
    /// <remarks>RFC 7230 §3.2.4 对「服务端收到的请求」是 MUST（拒绝或替换为空格），对响应则是客户端替换、不得拒绝，
    /// 故请求侧置为 true（见 <see cref="HttpRequest" />），响应侧保持宽容以兼容历史服务器。</remarks>
    protected virtual Boolean StrictHeader => false;

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

        // 整表重建：同实例二次解析（复用 HttpRequest/HttpResponse）时，上一条的头部必须随之消失。
        // 否则残留的 Transfer-Encoding/Content-Length 会让本条消息按错误的方式分帧（残留判据比缺省值更危险）
        Headers.Clear();

        var firstLine = "";
        var clCount = 0;
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

                // 严格模式（请求侧）：字段名与冒号之间的空白、以及以空白开头的续行折叠（obs-fold）一律拒绝。
                // 二者都会让本端与前置代理对同一段字节得出不同理解——攻击者用「Content-Length\t: 5」或折叠出的
                // 「 Content-Length: 5」藏出第二个长度：本端认不出而代理认（或反之），后续字节在本端算作下一个请求、
                // 在代理算作实体，请求走私与连接边界失步由此成立。此处选择拒绝而非替换（RFC 7230 §3.2.4 二者皆可）
                if (StrictHeader)
                {
                    if (line.Length > 0 && (line[0] == (Byte)' ' || line[0] == (Byte)'\t')) return false;

                    if (p3 > 0 && (line[..p3].IndexOf((Byte)' ') >= 0 || line[..p3].IndexOf((Byte)'\t') >= 0)) return false;
                }

                if (p3 > 0)
                {
                    var name = line[..p3].Trim((Byte)' ').ToStr();
                    var value = line[(p3 + 1)..].Trim((Byte)' ').ToStr();

                    // 同名头部就地覆盖，重复的 Content-Length 只剩最后一个值，与前置代理（通常取第一个）理解不一致，
                    // 构成 CL.CL 走私面。只统计本次解析的出现次数
                    if (name.EqualIgnoreCase("Content-Length")) clCount++;

                    Headers[name] = value;
                }
            }

            if (p2 < 0 || p2 + 2 >= header.Length) break;
            header = header[(p2 + 2)..];
        }

        // Content-Length 用 Int64 解析：无法解析、为负或超上限都视为非法，不能退化成“无体”。
        // 旧实现用 ToInt(-1)，非法值直接变成 -1（无体），声明的那段主体会被当成后续请求字节（边界失步）。
        // 出现两次及以上同样非法（RFC 9112 §6.3）：取值会被就地覆盖成其中一个，前后端看到不同长度
        var cl = Headers["Content-Length"];
        ContentLength = -1;
        InvalidContentLength = false;
        if (clCount > 1)
            InvalidContentLength = true;
        else if (!cl.IsNullOrEmpty())
        {
            if (Int64.TryParse(cl, out var len) && len >= 0 && len <= Int32.MaxValue)
                ContentLength = (Int32)len;
            else
                InvalidContentLength = true;
        }

        ContentType = Headers["Content-Type"];

        // 截取主体（跳过 CRLFCRLF 共4字节）：共享切片独立持有引用；入参句柄由调用方释放。
        // 声明了 Content-Length 时按它截断：同一轮数据里其后紧跟的字节（客户端多发、或同连接的下一请求）
        // 不属于本请求，否则会被当业务参数/JSON 解析，并写进链路追踪
        var bodyStart = p + 4;
        var available = pk.Total - bodyStart;
        // 声明长度小于本轮可用字节时只截取声明长度
        var declared = ContentLength >= 0 && ContentLength < available;
        Body = declared
            ? pk.Slice(bodyStart, ContentLength)
            : pk.Slice(bodyStart, -1);

        // 本请求占用的字节数，其后多出的字节属于后续请求（HTTP/1.1 流水线），调用方据此切分保留。
        // 未声明 Content-Length 时头部之后全部按实体处理，消耗量即整包长度
        ConsumedLength = bodyStart + (declared ? ContentLength : available);

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
        // 无实体响应（204/304）不得携带实体：既不写入主体字节，也不计入长度
        var body = HasNoEntity ? null : Body;
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

    /// <summary>是否为无实体消息（1xx/204/304 响应）。无实体时不得声明 Content-Length（RFC 7230 §3.3.2）</summary>
    protected virtual Boolean HasNoEntity => false;

    /// <summary>判断状态码是否属于无实体响应（1xx、204、304）</summary>
    /// <remarks>RFC 7230 §3.3.2：1xx 与 204 不得携带 Content-Length，1xx/204/304 均不得携带实体。响应构建与低级封包构造共用本判据，避免两处口径不一致。</remarks>
    /// <param name="code">响应状态码</param>
    /// <returns>是否为无实体响应</returns>
    public static Boolean IsNoEntityStatus(HttpStatusCode code) => (Int32)code is >= 100 and < 200 or 204 or 304;

    /// <summary>仅创建头部封包（不含主体），流式发送时先发头部</summary>
    /// <param name="contentLength">主体长度；负数表示未知（调用方应先行设置 Transfer-Encoding 等头部）</param>
    /// <returns>头部数据包，调用方负责 Dispose</returns>
    /// <remarks>已知长度时自动写入 Content-Length（<see cref="HasNoEntity"/> 为 true 时例外）；长度未知时由调用方设置 Transfer-Encoding: chunked 等头部。</remarks>
    public IOwnerPacket BuildHeaderPacket(Int64 contentLength = -1)
    {
        if (contentLength >= 0 && !HasNoEntity) Headers["Content-Length"] = contentLength.ToString();

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