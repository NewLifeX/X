using System.Buffers;
using System.Net;
using System.Web;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>Http会话</summary>
/// <remarks>
/// 负责处理单个Http连接的请求和响应，支持：
/// 1. 请求解析和主体分片接收；
/// 2. WebSocket 握手和消息处理；
/// 3. 链路追踪和日志记录。
/// </remarks>
public class HttpSession : INetHandler, IDisposable
{
    #region 属性
    /// <summary>当前请求</summary>
    public HttpRequest? Request { get; set; }

    /// <summary>Http服务主机。不一定是HttpServer</summary>
    public IHttpHost? Host { get; set; }

    /// <summary>最大请求长度。单位字节，默认1G</summary>
    public Int32 MaxRequestLength { get; set; } = 1 * 1024 * 1024 * 1024;

    /// <summary>在途请求的停滞上限。单位毫秒，默认30000；0或负数不启用</summary>
    /// <remarks>
    /// <para>请求已开始（头部或实体未收齐）却超过此时长没有新字节到达时，回 408 并关闭连接，兜住慢速攻击（slowloris，滴字节占用连接）。</para>
    /// <para>只判<b>停滞</b>不判<b>总时长</b>：持续推进的慢速上传（大文件、弱网）不受影响，完全静默的连接由会话超时回收。</para>
    /// <para>仅在请求头/实体未收齐时生效，已交付业务处理的请求不会再被本判定打断。</para>
    /// </remarks>
    public Int32 MaxRequestIdle { get; set; } = 30_000;

    /// <summary>忽略的头部。在链路追踪中不记录这些头部</summary>
    public static String[] ExcludeHeaders { get; set; } = [
        "traceparent", "Authorization", "Cookie"
    ];

    /// <summary>支持作为标签数据的内容类型</summary>
    public static String[] TagTypes { get; set; } = [
        "text/plain", "text/xml", "application/json", "application/xml", "application/x-www-form-urlencoded"
    ];

    /// <summary>请求头跨轮分片缓存上限。超出丢弃，防止无效数据持续占用内存</summary>
    private const Int32 MaxHeadLength = 64 * 1024;

    private static readonly Byte[] NewLine2 = [(Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n'];

    private INetSession _session = null!;
    private WebSocket? _websocket;
    private MemoryStream? _cache;
    private MemoryStream? _headCache;

    /// <summary>在途请求的实体是否为分块传输（无 Content-Length，必须解码终止块后才能交付）</summary>
    private Boolean _chunkedBody;

    /// <summary>上次收到字节的时间。用于判定在途请求是否停滞（慢速攻击兜底）</summary>
    private DateTime _lastReceive;
    #endregion

    #region 辅助
    /// <summary>判断本轮数据是否像 HTTP 头部开头且尚不完整（无空行）：需要缓存等待后续分片</summary>
    /// <param name="pk">本轮数据包</param>
    /// <returns>是否应缓存等待后续分片</returns>
    private static Boolean IsIncompleteHead(IPacket pk)
    {
        // 首行断言跨段前缀拼读；总长与空行查找均链式感知
        Span<Byte> buf = stackalloc Byte[10];
        return HttpBase.FastValidHeader(pk.GetPrefix(buf, 10)) && pk.Total <= MaxHeadLength && pk.IndexOf(NewLine2) < 0;
    }
    #endregion

    #region 收发数据
    /// <summary>建立连接时初始化会话</summary>
    /// <param name="session">网络会话</param>
    public void Init(INetSession session)
    {
        _session = session;
        Host ??= session.Host as IHttpHost;
    }

    /// <summary>处理客户端发来的数据</summary>
    /// <param name="data">数据帧</param>
    public void Process(IData data)
    {
        var pk = data.Packet;
        // Length 是“本节点长度”，链式包的首节点可能为空：按 Total 判断整包是否为空，
        // 否则首节点为空的链式包会被整包丢弃
        if (pk == null || pk.Total == 0) return;

        // WebSocket 通道已建立，直接交给 WebSocket 处理
        if (_websocket != null)
        {
            _websocket.Process(pk);
            return;
        }

        // 慢速攻击兜底：在途请求（头部或实体未收齐）长期没有新字节到达时，连接会被无限期占用。
        // 只判“距上次收到字节的间隔”而不判请求总时长，故持续推进的慢速上传不受影响；
        // 完全静默的连接不会进入本方法，由会话超时回收。
        var now = DateTime.Now;
        if ((_headCache != null || _cache != null) && MaxRequestIdle > 0 && (now - _lastReceive).TotalMilliseconds > MaxRequestIdle)
        {
            _headCache = null;
            _cache = null;
            _chunkedBody = false;

            Reject(HttpStatusCode.RequestTimeout);

            return;
        }
        _lastReceive = now;

        // 在途请求的实体尚未收完：本轮字节一律属于该实体，绝不能再尝试解析新的请求头。
        // 否则实体内恰好出现一段合法请求头（代理转发、恶意构造）时，会在此覆盖 Request 并丢弃实体缓存，
        // 原请求永不回响应，其后的字节被当成下一个请求处理，连接边界失步（与前置代理共存即为请求走私面）
        if (_cache != null)
        {
            if (Request is { } pending)
            {
                // 实体收满后，超出声明长度的字节属于后续请求（流水线），以共享视图返回继续解析
                pk = _chunkedBody
                    ? ReceiveChunkedBody(pending, _cache, pk, data)
                    : ReceiveBody(pending, _cache, pk, data);
                if (pk == null || pk.Total == 0) return;

                // 交付后连接可能已被关闭（Connection: close）：剩余字节不再处理
                if (_session.Disposed) return;
            }
            else
            {
                // Request 被外部清空，与实体缓存状态不一致：丢弃缓存，回到无在途请求的常态
                _cache = null;
                _chunkedBody = false;
            }
        }

        // 上次接收遗留的字节（流水线请求的整段或头部片段）先与本轮数据拼接，再一起解析
        if (_headCache != null)
        {
            pk.CopyTo(_headCache);
            pk = new ArrayPacket(_headCache.GetBuffer(), 0, (Int32)_headCache.Length);
        }

        // 一次接收可能包含多个请求（HTTP/1.1 流水线）：逐个解析并交付，直到字节不足或出现错误。
        // 旧实现丢弃请求体之后的字节，客户端一次发出两个请求只能收到一个响应，第二个请求要等到连接超时
        IPacket? left = null;
        try
        {
            while (pk.Total > 0)
            {
                var request = new HttpRequest();
                if (!request.Parse(pk))
                {
                    // 请求头未解析成功：缓存不完整头部等待后续分片（头部跨轮），或按无效请求拒绝
                    if (_headCache != null)
                    {
                        // 缓存的头部片加本轮数据仍不能成头：超限，或含完整空行仍解析失败（无效请求头）——都按 400 拒绝并关闭
                        if (_headCache.Length > MaxHeadLength || pk.IndexOf(NewLine2) >= 0)
                        {
                            _headCache = null;

                            Reject(HttpStatusCode.BadRequest);
                        }
                    }
                    else if (IsIncompleteHead(pk))
                    {
                        // 无活动请求，本轮数据像 HTTP 开头但头部不含空行（不完整）：缓存等待后续分片（请求头跨轮）
                        _headCache = new MemoryStream();
                        pk.CopyTo(_headCache);
                    }
                    else if (pk.IndexOf(NewLine2) >= 0)
                    {
                        // 头部已完整（含空行）却解析失败：聘形请求行或无效头部。
                        // 按 400 拒绝并关闭；此前既不回响应也不关连接，客户端只能等到超时
                        Reject(HttpStatusCode.BadRequest);
                    }

                    break;
                }

                _headCache = null;
                Request = request;

                (_session as NetSession)?.WriteLog("{0} {1}", request.Method, request.RequestUri);

                // Transfer-Encoding：只支持 chunked，且必须是唯一编码（RFC 9112 §6.1 要求 chunked 最后出现，
                // 本库不实现其它传输编码，故出现 gzip 等一律按不支持回 400）。分块体长度未知，必须逐块解码，
                // 否则 ContentLength 取到 -1、内容被当作“已完整”，chunk 帧会被当成业务参数/JSON 解析（静默错误）
                var te = request.Headers["Transfer-Encoding"];
                var chunked = false;
                if (!te.IsNullOrEmpty())
                {
                    if (!te.EqualIgnoreCase("chunked"))
                    {
                        Reject(HttpStatusCode.BadRequest);

                        break;
                    }

                    chunked = true;
                }

                // Content-Length 非法（无法解析/为负/溢出/重复）：按协议错误处理。
                // 若按“无体”继续，声明的那段主体会被当成后续请求字节，连接边界失步（与前置代理共存时还有走私面）
                if (request.InvalidContentLength)
                {
                    Reject(HttpStatusCode.BadRequest);

                    break;
                }

                // 限制最大请求体
                if (request.ContentLength > MaxRequestLength)
                {
                    Reject(HttpStatusCode.RequestEntityTooLarge);

                    break;
                }

                // Expect：本端只处理 100-continue，其余期望一律忽略，不回 417。
                // 100-continue 值得处理——curl 对超过 1KB 的请求体默认就发它，客户端发完请求头后等待临时响应才发实体，
                // 不应答会让每个此类请求白等一次客户端超时；实体已随头部一并到达时无需应答。
                // 其它期望在现实中不会出现（RFC 9110 §10.1.1 的 417 是 MAY 不是 MUST），回 417 只会把本可成功的请求变成失败。
                // HTTP/1.0 不支持临时响应（RFC 7231 §5.1.1 要求忽略其 Expect），按其协议版本原样处理。
                // 分块体的“是否已完整”不能用 IsCompleted 判定（ContentLength 为 -1 时为 true）
                var expect = request.Headers["Expect"];
                if ((chunked || !request.IsCompleted) && !expect.IsNullOrEmpty() && !request.Version.EqualIgnoreCase("1.0") &&
                    expect.IndexOf("100-continue", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // 临时响应不含实体，也不得声明 Content-Length（RFC 7230 §3.3.2），故直接发送响应行
                    var version = request.Version.IsNullOrEmpty() ? "1.1" : request.Version;
                    _session.Send($"HTTP/{version} 100 Continue\r\n\r\n");
                }

                _websocket = null; // 新请求到来，清空 websocket 握手状态
                OnNewRequest(request, data);

                // 分块传输的请求体：长度未知，必须先解码出终止块才能交付业务，不能按“已完整”放行
                if (chunked)
                {
                    _chunkedBody = true;
                    _cache = new MemoryStream();

                    // 本轮已到达的实体字节所有权转交解码器；解码器只借阅，用完由这里释放
                    var raw = request.Body;
                    request.Body = null;

                    var done = TryDeliverChunked(request, _cache, raw, data, out var chunkLeft);
                    raw.TryDispose();

                    if (!done) break;

                    // 终止块之后可能还有流水线请求：以剩余字节继续循环
                    if (_session.Disposed || _websocket != null) break;
                    if (chunkLeft == null || chunkLeft.Total == 0) break;

                    left.TryDispose();
                    left = chunkLeft;
                    pk = chunkLeft;
                    continue;
                }

                // 后面还有数据包，克隆缓冲区
                if (request.IsCompleted)
                {
                    // 头部 + 空主体 或 已一次性接收完整主体
                    _cache = null;
                }
                else
                {
                    // 缓存流按需增长：按 ContentLength 预分配会让“声明巨大长度但不发送”的请求直接占住等量内存
                    // （MaxRequestLength 默认 1GB，一条声明 1GB 的连接即可触发大对象分配）
                    _cache = new MemoryStream();

                    if (request.Body != null && request.Body.Length > 0)
                    {
                        // 解析阶段已经截取到的主体部分先写入缓存
                        request.Body.CopyTo(_cache);
                        request.Body.TryDispose();
                        request.Body = null;
                    }
                }

                Deliver(request, data);

                // 连接已关闭（拒绝、Connection: close、流式发送失败），或已升级为 WebSocket（剩余字节是帧，等下一轮处理）：
                // 剩余字节不再按请求解析
                if (_session.Disposed || _websocket != null) break;

                // 剩余字节属于后续流水线请求，切入共享句柄继续解析；本方法切出的句柄需自行释放，
                // 入参句柄由框架在轮末按引用计数裁决，不得在此释放
                var count = pk.Total - request.ConsumedLength;
                if (count <= 0) break;

                var next = pk.Slice(request.ConsumedLength, -1);
                left.TryDispose();  // 上一轮切出的句柄已用完
                left = next;
                pk = next;
            }
        }
        finally
        {
            left.TryDispose();
        }
    }

    /// <summary>接收分块传输的请求体：累积原始字节，收齐终止块后解码交付</summary>
    /// <param name="req">在途请求</param>
    /// <param name="cache">原始字节缓存（由 <see cref="Process"/> 保证非空）</param>
    /// <param name="pk">本轮数据（只借阅，不释放）</param>
    /// <param name="data">数据帧</param>
    /// <returns>分块体之后的剩余字节（缓存流缓冲区的视图，无需释放）；尚未收齐时返回 null</returns>
    private IPacket? ReceiveChunkedBody(HttpRequest req, MemoryStream cache, IPacket pk, IData data)
        => TryDeliverChunked(req, cache, pk, data, out var remain) ? remain : null;

    /// <summary>尝试解码并交付分块传输的请求体</summary>
    /// <param name="req">在途请求</param>
    /// <param name="cache">原始字节缓存</param>
    /// <param name="raw">本轮新到的原始字节；只借阅，不释放</param>
    /// <param name="data">数据帧</param>
    /// <param name="remain">分块体之后的剩余字节（同一缓存缓冲区的视图），无则为 null</param>
    /// <returns>是否已收齐并交付；false 表示还需后续分片，或已因超限/非法而拒绝并关闭</returns>
    private Boolean TryDeliverChunked(HttpRequest req, MemoryStream cache, IPacket? raw, IData data, out IPacket? remain)
    {
        remain = null;

        if (raw != null && raw.Total > 0) raw.CopyTo(cache);

        // 原始字节上限即内存上限（解码结果只会更小）
        if (cache.Length > MaxRequestLength)
        {
            _cache = null;
            _chunkedBody = false;

            Reject(HttpStatusCode.RequestEntityTooLarge);

            return false;
        }

        var status = TryDecodeChunked(cache.GetBuffer(), (Int32)cache.Length, out var body, out var consumed);
        if (status == ChunkedStatus.Incomplete) return false;
        if (status == ChunkedStatus.Invalid)
        {
            _cache = null;
            _chunkedBody = false;

            Reject(HttpStatusCode.BadRequest);

            return false;
        }

        _cache = null;
        _chunkedBody = false;

        // 解码后按已知长度交付：业务、链路追踪、multipart 解析看到的主体与 Content-Length 请求完全一致。
        // 头部也要一并对齐（RFC 9112 §7.1.3 的解码算法即“Content-Length := length，再从 Transfer-Encoding 移除 chunked”）：
        // 留着 Transfer-Encoding 会让下游按该头判断“拿到的实体是不是原始分块”而再解码一次，
        // 但这里交付的已经是解码结果，重复解码必然失败（NewLife.AI 的 HttpMcpServer 就是按该头判断的）
        req.Body.TryDispose();
        req.Body = body;
        req.ContentLength = body.Total;
        req.Headers.Remove("Transfer-Encoding");
        req.Headers["Content-Length"] = body.Total + "";

        // 终止块之后的字节属于后续流水线请求，切出视图交给调用方继续解析
        var extra = (Int32)cache.Length - consumed;
        if (extra > 0) remain = new ArrayPacket(cache.GetBuffer(), consumed, extra);

        Deliver(req, data);

        return true;
    }

    /// <summary>分块传输解码结果</summary>
    private enum ChunkedStatus
    {
        /// <summary>数据不足，需继续接收</summary>
        Incomplete,

        /// <summary>已收齐终止块</summary>
        Complete,

        /// <summary>分块格式非法（坏块长度行）</summary>
        Invalid,
    }

    /// <summary>解码分块传输体（RFC 9112 §7.1）。块长度十六进制、忽略“;”扩展，终止块之后的 trailer 段一并消费</summary>
    /// <param name="buffer">原始字节</param>
    /// <param name="length">原始字节有效长度</param>
    /// <param name="body">解码后的主体；未收齐或非法时为 null</param>
    /// <param name="consumed">消耗的原始字节数（收齐时有效）</param>
    /// <returns>解码结果</returns>
    private static ChunkedStatus TryDecodeChunked(Byte[] buffer, Int32 length, out IPacket body, out Int32 consumed)
    {
        body = null!;
        consumed = 0;

        var decoded = new MemoryStream();
        var pos = 0;
        while (true)
        {
            var eol = IndexOfCrlf(buffer, pos, length);
            if (eol < 0) return ChunkedStatus.Incomplete;

            // 块长度行：十六进制长度，可带 ";" 扩展
            var line = buffer.AsSpan(pos, eol - pos);
            var sc = line.IndexOf((Byte)';');
            if (sc >= 0) line = line[..sc];
            line = line.Trim((Byte)' ');

            if (line.Length == 0 || line.Length > 8) return ChunkedStatus.Invalid;

            var size = 0L;
            foreach (var b in line)
            {
                var d = HexValue(b);
                if (d < 0) return ChunkedStatus.Invalid;

                size = size * 16 + d;
            }

            pos = eol + 2;

            // 终止块：其后可能有 trailer 段，以空行结束
            if (size == 0)
            {
                if (pos + 1 < length && buffer[pos] == (Byte)'\r' && buffer[pos + 1] == (Byte)'\n')
                    consumed = pos + 2;
                else
                {
                    var end = IndexOfCrlf2(buffer, pos, length);
                    if (end < 0) return ChunkedStatus.Incomplete;

                    consumed = end + 4;
                }

                body = new ArrayPacket(decoded.GetBuffer(), 0, (Int32)decoded.Length);

                return ChunkedStatus.Complete;
            }

            // 数据不足：块体与末尾 CRLF 必须都在缓冲区内
            if (size > length - pos - 2) return ChunkedStatus.Incomplete;

            decoded.Write(buffer, pos, (Int32)size);
            pos += (Int32)size;

            // 块数据后的 CRLF
            if (pos + 2 > length || buffer[pos] != (Byte)'\r' || buffer[pos + 1] != (Byte)'\n') return ChunkedStatus.Incomplete;

            pos += 2;
        }
    }

    /// <summary>在指定范围内查找 CRLF，返回起始下标；未找到返回 -1</summary>
    private static Int32 IndexOfCrlf(Byte[] buffer, Int32 start, Int32 length)
    {
        for (var i = start; i + 1 < length; i++)
        {
            if (buffer[i] == (Byte)'\r' && buffer[i + 1] == (Byte)'\n') return i;
        }

        return -1;
    }

    /// <summary>在指定范围内查找空行（CRLFCRLF），返回其起始下标；未找到返回 -1</summary>
    private static Int32 IndexOfCrlf2(Byte[] buffer, Int32 start, Int32 length)
    {
        for (var i = start; i + 3 < length; i++)
        {
            if (buffer[i] == (Byte)'\r' && buffer[i + 1] == (Byte)'\n' && buffer[i + 2] == (Byte)'\r' && buffer[i + 3] == (Byte)'\n') return i;
        }

        return -1;
    }

    /// <summary>十六进制字符转数值；非法字符返回 -1</summary>
    private static Int32 HexValue(Byte b) => b switch
    {
        >= (Byte)'0' and <= (Byte)'9' => b - (Byte)'0',
        >= (Byte)'a' and <= (Byte)'f' => b - (Byte)'a' + 10,
        >= (Byte)'A' and <= (Byte)'F' => b - (Byte)'A' + 10,
        _ => -1,
    };

    /// <summary>接收在途请求的实体分片，收满声明长度后交付业务处理</summary>
    /// <param name="req">在途请求</param>
    /// <param name="cache">实体缓存流（由 <see cref="Process"/> 保证非空）</param>
    /// <param name="pk">本轮数据</param>
    /// <param name="data">数据帧</param>
    /// <returns>本轮未消费的剩余字节（缓存流缓冲区的视图，无需释放）；实体尚未收满时返回 null</returns>
    private IPacket? ReceiveBody(HttpRequest req, MemoryStream cache, IPacket pk, IData data)
    {
        pk.CopyTo(cache);

        // 防御：主体量达到声明长度视为完成。本分支恒有 ContentLength >= 0，否则头部解析后 IsCompleted 已为真
        if (cache.Length < req.ContentLength) return null;

        cache.Position = 0;
        req.Body = new ArrayPacket(cache.GetBuffer(), 0, req.ContentLength);
        _cache = null;

        // 超出声明长度的字节（同连接的下一个请求）不属于本请求，不能一并当主体交给业务，
        // 也不能丢弃：切出视图交给调用方继续解析并响应，否则流水线上第二个请求只能等到连接超时
        var extra = (Int32)cache.Length - req.ContentLength;
        IPacket? remain = null;
        if (extra > 0) remain = new ArrayPacket(cache.GetBuffer(), req.ContentLength, extra);

        Deliver(req, data);

        return remain;
    }

    /// <summary>交付请求给业务处理并发送响应，随后释放请求主体</summary>
    /// <param name="req">请求；为 null 表示本轮数据尚未构成完整请求头，无需交付</param>
    /// <param name="data">数据帧</param>
    private void Deliver(HttpRequest? req, IData data)
    {
        if (req != null)
        {
            // 改变数据
            data.Message = req;
            data.Packet = req.Body; // 仅在收到完整主体后非空
        }

        // 主体接收完成，触发业务处理
        if (req != null && req.IsCompleted)
        {
            var rs = ProcessRequest(req, data);

            // 请求已交付处理：清除当前请求引用，防止后续轮次（新请求头分片/残片数据）再次处理旧请求（重放）
            if (ReferenceEquals(Request, req)) Request = null;

            if (rs != null)
            {
                var server = _session.Host as HttpServer;
                if (server != null && !server.ServerName.IsNullOrEmpty() && !rs.Headers.ContainsKey("Server"))
                    rs.Headers["Server"] = server.ServerName;

                // 响应版本跟随请求：HTTP/1.0 客户端若收到 HTTP/1.1 响应，会按 1.1 语义（默认 keep-alive）解析，导致连接失步
                if (req != null && !req.Version.IsNullOrEmpty()) rs.Version = req.Version;

                // 请求为空（理论上不会发生）按关闭处理，避免响应无主连接悬留
                var closing = req == null || (!req.KeepAlive && _websocket == null);
                if (closing && !rs.Headers.ContainsKey("Connection")) rs.Headers["Connection"] = "close";

                // HEAD 请求：不得返回实体，但仍需声明实体长度，否则 keep-alive 连接上客户端会把后续响应当成本次实体（连接失步）。
                // 必须覆盖 BodyStream 形态（静态文件/嵌入资源均走流式）：旧判断只看内存体，HEAD 时会把实体一起发出去
                if (req != null && req.Method.EqualIgnoreCase("HEAD"))
                {
                    var length = -1L;
                    if (rs.BodyStream is { } headStream)
                    {
                        // 长度可知（可寻址流）则声明 Content-Length，否则不声明（HEAD 允许省略）
                        if (headStream.CanSeek)
                        {
                            try { length = headStream.Length - headStream.Position; } catch { length = -1; }
                        }

                        // 实体所有权随响应：HEAD 不发送实体，就地释放，避免残留文件/内存句柄
                        rs.BodyStream = null;
                        headStream.TryDispose();
                    }
                    else
                    {
                        length = rs.Body?.Total ?? 0;
                    }

                    using var res = rs.BuildHeaderPacket(length);
                    _session.Send(res);
                }
                // 流式响应体：先发头部，再流式发送主体（已知长度走 Content-Length，未知走分块传输）
                else if (rs.BodyStream is { } stream)
                    SendStreamBody(rs, stream);
                else
                {
                    // 发送响应。用完后释放数据包，还给缓冲池
                    using var res = rs.Build();
                    _session.Send(res);
                }

                // 响应体所有权随响应：Build 已把体写入发送缓冲，此处归还其（可能池化的）引用
                rs.Body.TryDispose();
                rs.Body = null;

                if (closing) _session.Dispose();
            }
        }

        // 请求结束后释放主体（响应发送后即可释放）
        if (req != null)
        {
            // data.Packet 引用同一句柄：先置空，避免交付完成后仍有环节通过 IData.Packet 读到已释放句柄
            if (ReferenceEquals(data.Packet, req.Body)) data.Packet = null;

            req.Body.TryDispose();
            req.Body = null;

            // multipart 文件数据是请求包上的共享切片（独立持有池缓冲引用），必须一并释放，
            // 否则每上传一次就泄漏一块池缓冲（服务端会话可能存活到超时，缓冲长期不回池）
            if (req.Files != null)
            {
                foreach (var file in req.Files) file.Data.TryDispose();
                req.Files = null;
            }
        }
    }

    /// <summary>回指定状态码并关闭会话。用于请求进入业务处理前的拒绝（聘形请求、超限、实体无法定界）</summary>
    /// <param name="code">HTTP 状态码</param>
    private void Reject(HttpStatusCode code)
    {
        var rs = new HttpResponse { StatusCode = code };

        using var res = rs.Build();
        _session.Send(res);
        _session.Dispose();
    }

    /// <summary>发送流式响应体。先发头部，再分块读取并发送主体</summary>
    /// <param name="rs">响应</param>
    /// <param name="stream">主体数据流（所有权随响应，发送完成后释放）</param>
    /// <remarks>
    /// <para>背压：读一块写一块，慢速客户端下每连接内存有界——待发数据由内核发送缓冲限定（水位即内核缓冲），不随响应体大小增长。</para>
    /// <para>长度可知时整段交给会话的发送出口，其中“文件流从头整发”交给内核零拷贝推送；
    /// 长度未知时按分块传输逐块发送。</para>
    /// </remarks>
    private void SendStreamBody(HttpResponse rs, Stream stream)
    {
        try
        {
            // 长度可知（可寻址流）：声明 Content-Length；否则分块传输
            var length = -1L;
            if (stream.CanSeek)
            {
                try { length = stream.Length - stream.Position; } catch { length = -1; }
            }

            var chunked = length < 0;
            if (chunked) rs.Headers["Transfer-Encoding"] = "chunked";

            // 分块响应体（长度未知）：头部与各分块同走发送队列出口——一条响应内严格有序，
            // 且发送泵会把读窗口内累积的多块合并为一次散列写提交，减少系统调用与 TCP 段
            TcpSession? queued = null;
            if (chunked && _session.Session is TcpSession { Active: true } host) queued = host;

            var head = rs.BuildHeaderPacket(length);
            if (queued != null)
                SendBlock(queued, head);
            else
            {
                _session.Send(head);
                head.TryDispose();
            }

            // 长度可知：整段交给会话的发送出口（读一块写一块，内核缓冲形成背压）。
            // 本同步链上等待完成：HTTP 处理链由 void 的 Process(IData) 驱动、无法 await，
            // 慢客户端下大文件响应会占住该会话的处理线程（分块响应体已改走发送队列，不再阻塞在套接字写上）
            if (!chunked && _session.Session is TcpSession { Active: true } tcp)
            {
                // 文件流且从头整发：交给内核零拷贝推文件（sendfile/TransmitFile），应用层不再经读块搬运；
                // 其余情形（Range 片段、内存流等）走分块读写的流式发送
                if (stream is FileStream fs && fs.Position == 0 && fs.Length == length && !fs.Name.IsNullOrEmpty())
                {
                    // 零拷贝按路径重开文件：流打开后文件被删除/改名（临时文件、DeleteOnClose）或相对路径解析失败时，
                    // 此路径会在未发出任何数据前抛 FileNotFoundException，此时降级为按原流分块发送。
                    // 没有这层兜底，优化会把「读已打开的流一定成功」变成「路径失效即整条响应失败」
                    try
                    {
                        if (tcp.SendFileAsync(fs.Name).GetAwaiter().GetResult() < 0) throw new IOException($"Send file failed: {fs.Name}");

                        return;
                    }
                    catch (FileNotFoundException)
                    {
                        if (fs.Position != 0) fs.Seek(0, SeekOrigin.Begin);
                    }
                }

                tcp.SendAsync(stream, length).GetAwaiter().GetResult();

                return;
            }

            using var buffer = Pool.Rent(64 * 1024);

            while (true)
            {
                var count = stream.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;

                if (chunked)
                {
                    // 分块传输：十六进制长度 CRLF + 数据 + CRLF 组装为单包
                    var pk = BuildChunk(buffer, count);
                    if (queued != null)
                        SendBlock(queued, pk);
                    else
                    {
                        // 直发为借用语义（不接管句柄），发完由本层归还池缓冲
                        SendBlock(pk);
                        pk.TryDispose();
                    }
                }
                else
                {
                    _session.Send(buffer, 0, count);
                }
            }

            // 终止块
            if (chunked)
            {
                if (queued != null)
                    SendBlock(queued, new ArrayPacket("0\r\n\r\n".GetBytes()));
                else
                    _session.Send("0\r\n\r\n");
            }
        }
        catch (Exception ex)
        {
            // 发送失败（含流已被释放）：记录并关闭连接，客户端可感知响应不完整
            (_session as NetSession)?.WriteLog("流式发送失败 {0}", ex.Message);
            _session.Dispose();
        }
        finally
        {
            stream.Dispose();
        }
    }

    /// <summary>发送一块流式数据（直发）。与其它发送入口共用会话写锁，写完（或内核缓冲满时挂起）才返回</summary>
    /// <param name="pk">数据包。句柄借用：与 <see cref="INetSession.Send(IPacket)"/> 同义，返回后调用方自行释放</param>
    private void SendBlock(IPacket pk) => _session.Send(pk);

    /// <summary>发送一块流式数据（经发送队列，所有权转移）。队列积压时本同步链上等待水位恢复</summary>
    /// <remarks>
    /// <para>同步等待：<see cref="SendStreamBody"/> 由同步接收链调用（<c>Process(IData)</c> 返回 void），本层无法 await。</para>
    /// <para>发送泵运行在专用线程（LongRunning）上，故此处阻塞不会与泵互抢线程池线程而死锁。</para>
    /// </remarks>
    /// <param name="session">目标会话</param>
    /// <param name="pk">数据包。所有权转移：入队后由发送泵负责释放，调用方不得再释放</param>
    private static void SendBlock(TcpSession session, IPacket pk)
    {
        Boolean ok;
        try
        {
            ok = session.SendQueuedAsync(pk).GetAwaiter().GetResult();
        }
        catch
        {
            // 未入队即失败（会话已释放/未打开）：句柄所有权未转移，由本层归还
            pk.TryDispose();

            throw;
        }

        // 返回 false 表示队列已结束：数据已由管道释放，这里不得再释放（否则重复归还池缓冲）
        if (!ok) throw new IOException("Send queue is closed.");
    }

    /// <summary>组装分块传输数据块（十六进制长度 CRLF + 数据 CRLF）</summary>
    /// <param name="data">数据缓冲</param>
    /// <param name="count">有效字节数</param>
    /// <returns>分块数据包，调用方负责 Dispose</returns>
    private static IOwnerPacket BuildChunk(Byte[] data, Int32 count)
    {
        var hex = count.ToString("X");
        var size = hex.Length + 2 + count + 2;

        var pk = new OwnerPacket(size);
        var span = pk.GetSpan();

        for (var i = 0; i < hex.Length; i++) span[i] = (Byte)hex[i];
        var p = hex.Length;
        span[p++] = 13;
        span[p++] = 10;
        data.AsSpan(0, count).CopyTo(span[p..]);
        p += count;
        span[p++] = 13;
        span[p] = 10;

        return pk;
    }

    /// <summary>收到新的Http请求（仅请求头解析完成时触发）</summary>
    /// <param name="request">请求</param>
    /// <param name="data">原始数据帧</param>
    protected virtual void OnNewRequest(HttpRequest request, IData data) { }

    /// <summary>处理Http请求</summary>
    /// <param name="request">请求</param>
    /// <param name="data">数据帧</param>
    /// <returns>响应</returns>
    protected virtual HttpResponse ProcessRequest(HttpRequest request, IData data)
    {
        if (request?.RequestUri == null) return new HttpResponse { StatusCode = HttpStatusCode.NotFound };

        //// 提取路径（不含查询）。使用 AbsolutePath 而非手动截取，避免奇异 ? 位置问题
        //var rawUri = request.RequestUri;
        //var path = rawUri.AbsolutePath; // AbsolutePath 已经处理百分号编码解码语义由下游决定
        // 匹配路由处理器。rawUri 没有Host部分，导致取 AbsolutePath 时报错
        var path = request.RequestUri.OriginalString;
        var p = path.IndexOf('?');
        if (p > 0) path = path[..p];

        // 路径安全检查
        if (!IsPathSafe(path)) return new HttpResponse { StatusCode = HttpStatusCode.Forbidden };

        // 埋点
        using var span = _session.Host.Tracer?.NewSpan(path);
        if (span != null)
        {
            span.Tag = $"{_session.Remote.EndPoint} {request.Method} {request.RequestUri}";
            span.Detach(request.Headers);
            span.Value = request.ContentLength;

            if (span is DefaultSpan ds && ds.TraceFlag > 0)
            {
                AppendSpanTag(span, request);
            }
        }

        // 匹配路由处理器。优先使用增强匹配以支持参数化路由
        var parameters = new Dictionary<String, Object?>();
        IHttpHandler? handler;

        if (Host is HttpServer server)
            handler = server.MatchHandler(path, request, parameters);
        else
            handler = Host?.MatchHandler(path, request);

        var context = new DefaultHttpContext(_session, request, path, handler)
        {
            ServiceProvider = _session as IServiceProvider
        };

        // 路由参数在 PrepareRequest 之后再合并（见下方）：路径是本请求的身份，不应被 ?id= 或表单字段覆盖

        // 创建请求级作用域，注册 IHttpContext 使构造函数 DI 可获取；请求结束（finally）释放作用域
        IServiceScope? scope = null;
        var sp = context.ServiceProvider;
        if (sp != null)
        {
            scope = sp.CreateScope();
            if (scope is IServiceRegistry registry)
            {
                registry.TryAdd(typeof(IHttpContext), context);
                context.ServiceProvider = scope.ServiceProvider;
            }
        }

        // 设置为当前上下文，便于静态访问
        DefaultHttpContext.Current = context;

        try
        {
            PrepareRequest(context);

            // 路由参数最后合并：路径参数优先于查询串/请求体参数。旧顺序下 /api/users/5?id=999 会让 id 变成 999，
            // 既是语义错误（路径应优先），也是参数篡改面（下游按 id 取数/鉴权会被绕过）
            foreach (var kv in parameters)
            {
                context.Parameters[kv.Key] = kv.Value;
            }

            // 处理 WebSocket 握手（只在第一次调用时尝试）
            _websocket ??= WebSocket.Handshake(context);

            // 声明了 Sec-WebSocket-Key 却握手不通过：明确回 400，不要落入普通路由（会被当成 404 掩盖真实原因）
            if (_websocket == null && !context.Request.Headers["Sec-WebSocket-Key"].IsNullOrEmpty())
                return new HttpResponse { StatusCode = HttpStatusCode.BadRequest };

            if (handler != null)
            {
                // 通过 HttpServer 的中间件管道执行处理器
                if (Host is HttpServer svr)
                    svr.ExecutePipeline(context, handler).GetAwaiter().GetResult();
                else
                    handler.ProcessRequest(context);
            }
            else if (_websocket == null)
                return new HttpResponse { StatusCode = HttpStatusCode.NotFound };

            // 根据状态码识别异常
            if (span != null)
            {
                var res = context.Response;
                span.Value += res.ContentLength;
                var code = res.StatusCode;
                if (code == HttpStatusCode.BadRequest || code > HttpStatusCode.NotFound)
                    span.SetError(new HttpRequestException($"Http Error {(Int32)code} {code}"), null);
            }
        }
        catch (HttpException hex)
        {
            span?.SetError(hex, null);
            context.Response.StatusCode = hex.StatusCode;
            context.Response.StatusDescription = hex.Message;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            context.Response.SetResult(ex);
        }
        finally
        {
            // 清理当前上下文，避免泄漏到后续请求
            DefaultHttpContext.Current = null;

            // 释放请求级作用域（scoped 服务的释放语义随 ServiceProvider 实现）
            scope?.Dispose();
        }

        return context.Response;
    }

    /// <summary>向链路追踪Span追加标签信息</summary>
    /// <param name="span">追踪Span</param>
    /// <param name="request">Http请求</param>
    private void AppendSpanTag(ISpan span, HttpRequest request)
    {
        var includeBody = false;
        var bodyLength = request.Body?.Total ?? 0;
        if (request.BodyLength > 0 && request.Body != null && bodyLength > 0 && bodyLength < 8 * 1024 && request.ContentType.EqualIgnoreCase(TagTypes))
        {
            // 主体可能为链式：链感知截断读取（最多 1024 字节）
            span.AppendTag("\r\n<=\r\n" + request.Body.ToStr(null, 0, 1024));
            includeBody = true;
        }

        if (span.Tag == null || span.Tag.Length < 500)
        {
            if (!includeBody) span.AppendTag("\r\n<=");
            var vs = request.Headers.Where(e => !e.Key.EqualIgnoreCase(ExcludeHeaders)).ToDictionary(e => e.Key, e => e.Value + "");
            span.AppendTag("\r\n" + vs.Join(Environment.NewLine, e => $"{e.Key}:{e.Value}"));
        }
        else if (!includeBody)
        {
            span.AppendTag("\r\n<=\r\n");
            span.AppendTag($"ContentLength: {request.ContentLength}\r\n");
            span.AppendTag($"ContentType: {request.ContentType}");
        }
    }

    /// <summary>简单路径安全检查，防止目录穿越和空字节注入</summary>
    /// <param name="path">请求路径</param>
    /// <returns>路径是否安全</returns>
    private static Boolean IsPathSafe(String path) => path.IndexOf("..", StringComparison.Ordinal) < 0 && path.IndexOf('\0') < 0;

    /// <summary>准备请求参数</summary>
    /// <param name="context">Http上下文</param>
    protected virtual void PrepareRequest(IHttpContext context)
    {
        var req = context.Request;
        var ps = context.Parameters;

        // 注入 IHttpContext 和 IServiceProvider 到参数列表（隐藏参数，供下游按名称匹配）
        ps["__context"] = context;
        ps["__serviceProvider"] = context.ServiceProvider;

        // 解析地址参数
        var uri = req.RequestUri;
        if (uri == null) return;

        var url = uri.OriginalString;
        var p = url.IndexOf('?');
        if (p > 0)
        {
            var qs = url[(p + 1)..].SplitAsDictionary("=", "&")
                .ToDictionary(e => HttpUtility.UrlDecode(e.Key), e => HttpUtility.UrlDecode(e.Value));
            ps.Merge(qs);
        }

        // POST 提交参数：Url编码、表单、Json
        if (req.Method == "POST" && req.BodyLength > 0 && req.Body != null)
        {
            ParsePostBody(req, ps);
        }
    }

    /// <summary>解析POST请求体参数</summary>
    /// <param name="req">Http请求</param>
    /// <param name="ps">参数字典</param>
    private void ParsePostBody(HttpRequest req, IDictionary<String, Object?> ps)
    {
        // 主体可能为链式（跨接收段）：统一用链感知读取
        var body = req.Body!;
        if (req.ContentType.StartsWithIgnoreCase("application/x-www-form-urlencoded", "application/x-www-urlencoded"))
        {
            var qs = body.ToStr().SplitAsDictionary("=", "&")
                .ToDictionary(e => HttpUtility.UrlDecode(e.Key), e => HttpUtility.UrlDecode(e.Value));
            ps.Merge(qs);
        }
        else if (req.ContentType.StartsWithIgnoreCase("multipart/form-data;"))
        {
            var dic = req.ParseFormData();
            var fs = dic.Values.Where(e => e is FormFile).Cast<FormFile>().ToArray();
            if (fs.Length > 0) req.Files = fs;
            ps.Merge(dic);
        }
        else if (body.Total >= 2 && body[0] == (Byte)'{' && body[body.Total - 1] == (Byte)'}')
        {
            var js = body.ToStr().DecodeJson();
            if (js != null) ps.Merge(js);
        }
    }
    #endregion

    #region 销毁
    /// <summary>销毁。释放请求头/体缓存与 WebSocket 粘包编码器（归还段链池缓冲）</summary>
    public void Dispose()
    {
        _cache?.Dispose();
        _cache = null;

        _headCache?.Dispose();
        _headCache = null;

        _websocket?.Dispose();
        _websocket = null;
    }
    #endregion
}
