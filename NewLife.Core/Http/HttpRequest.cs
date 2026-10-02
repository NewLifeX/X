using System;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Http;

/// <summary>Http请求</summary>
public class HttpRequest : HttpBase
{
    #region 属性
    /// <summary>Http方法</summary>
    public String? Method { get; set; }

    /// <summary>资源路径</summary>
    public Uri? RequestUri { get; set; }

    /// <summary>目标主机</summary>
    public String? Host { get; set; }

    /// <summary>保持连接。HTTP/1.1 默认保持，除非显式 Connection: close；HTTP/1.0 仅在 keep-alive 时保持</summary>
    public Boolean KeepAlive { get; set; }

    /// <summary>文件集合</summary>
    public FormFile[]? Files { get; set; }
    #endregion

    /// <summary>是否严格校验头部行。请求侧强制：字段名含空白、或续行折叠一律拒绝（RFC 7230 §3.2.4）</summary>
    protected override Boolean StrictHeader => true;

    /// <summary>分析第一行</summary>
    /// <param name="firstLine"></param>
    protected override Boolean OnParse(String firstLine)
    {
        if (firstLine.IsNullOrEmpty()) return false;

        var ss = firstLine.Split(' ');
        if (ss.Length < 3) return false;

        // 分析请求方法 GET / HTTP/1.1。第三段必须以 HTTP/ 开头：
        // 畸形请求行（如 GET /x FOO/1.1）此前会被当成解析成功，Method 与 Version 留空后仍进入业务处理
        if (!ss[2].StartsWithIgnoreCase("HTTP/")) return false;

        Method = ss[0];
        RequestUri = new Uri(ss[1], UriKind.RelativeOrAbsolute);
        Version = ss[2].TrimPrefix("HTTP/");

        Host = Headers["Host"];

        var conn = Headers["Connection"];
        if (Version == "1.1")
            KeepAlive = !conn.EqualIgnoreCase("close");
        else
            KeepAlive = conn.EqualIgnoreCase("keep-alive");

        return true;
    }

    private static readonly Byte[] NewLine = [(Byte)'\r', (Byte)'\n'];
    private static readonly Byte[] NewLine2 = [(Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n'];
    /// <summary>快速分析请求头，只分析第一行</summary>
    /// <remarks>主体为入参数据包的共享切片（引用计数独立持有）；本方法不释放 <paramref name="pk"/>，由调用方负责释放。首行可跨节点（链式帧）：跨段查找换行定位首行末端。</remarks>
    /// <param name="pk">数据包</param>
    /// <returns>是否解析成功</returns>
    public Boolean FastParse(IPacket pk)
    {
        // 快速验证：请求行以谓语开头（最多10字节内出现空格；跨段前缀拼读）
        Span<Byte> fastBuf = stackalloc Byte[10];
        if (!FastValidHeader(pk.GetPrefix(fastBuf, 10))) return false;

        // 首行可能跨节点：跨段查找换行定位首行末端
        var p = pk.IndexOf(NewLine);
        if (p < 0) return false;

        var line = pk.Slice(0, p).ToStr();

        // 主体：共享切片独立持有引用；入参句柄由调用方释放
        Body = pk.Slice(p + 2, -1);

        // 分析第一行
        if (!OnParse(line)) return false;

        return true;
    }

    /// <summary>创建头部</summary>
    /// <param name="length"></param>
    /// <returns></returns>
    protected override String BuildHeader(Int32 length)
    {
        if (Method.IsNullOrEmpty()) Method = length > 0 ? "POST" : "GET";

        // 分解主机和资源
        var uri = RequestUri ?? new Uri("/");

        if (Host.IsNullOrEmpty())
        {
            var host = "";
            if (uri.Host.IsNullOrEmpty())
            {
                // 相对路径情况下由外部附加 Host 头；此处保持空字符串
            }
            else if (uri.Scheme.EqualIgnoreCase("http", "ws"))
            {
                host = uri.Port == 80 ? uri.Host : $"{uri.Host}:{uri.Port}";
            }
            else if (uri.Scheme.EqualIgnoreCase("https", "wss"))
            {
                host = uri.Port == 443 ? uri.Host : $"{uri.Host}:{uri.Port}";
            }
            Host = host;
        }

        // 构建头部
        var sb = Pool.StringBuilder.Get();
        sb.AppendFormat("{0} {1} HTTP/{2}\r\n", Method, uri.PathAndQuery, Version);
        if (!Host.IsNullOrEmpty()) sb.AppendFormat("Host: {0}\r\n", Host);

        // 内容长度
        if (length > 0) Headers["Content-Length"] = length + "";
        if (!ContentType.IsNullOrEmpty()) Headers["Content-Type"] = ContentType;

        if (KeepAlive) Headers["Connection"] = "keep-alive";

        foreach (var item in Headers)
        {
            if (!item.Key.EqualIgnoreCase("Host"))
                sb.AppendFormat("{0}: {1}\r\n", item.Key, item.Value);
        }

        sb.Append("\r\n");

        return sb.Return(true);
    }

    /// <summary>分析表单数据</summary>
    /// <remarks>主体支持链式（跨接收段）：按链扫描定位段边界并共享切片，不做整体物化；文件数据为源包的共享切片（引用计数），随表单对象存活，应由使用方显式释放。</remarks>
    public virtual IDictionary<String, Object> ParseFormData()
    {
        var dic = new Dictionary<String, Object>();
        if (ContentType.IsNullOrEmpty()) return dic;

        var boundary = ContentType.Substring("boundary=", null);
        if (boundary.IsNullOrEmpty()) return dic;

        // 兼容带引号或带后续参数的 boundary
        var p = boundary.IndexOf(';');
        if (p > 0) boundary = boundary[..p];
        boundary = boundary.Trim().Trim('"');
        if (boundary.IsNullOrEmpty()) return dic;

        var body = Body;
        if (body == null || body.Total == 0) return dic;

        // 段分隔标记与段结束标记（上一段数据尾部 + 分隔标记）
        var bd = ("--" + boundary).GetBytes();
        var bd2 = ("\r\n--" + boundary).GetBytes();

        var total = body.Total;
        // 头部跨段拼读缓冲，循环外分配一次
        Span<Byte> hbuf = stackalloc Byte[256];

        // 首个分隔标记之前可含前导内容，按链扫描定位；其后各段位置由段结束标记推导
        var pos = body.IndexOf(bd);

        /*
         * ------WebKitFormBoundary3ZXeqQWNjAzojVR7
         * Content-Disposition: form-data; name="name"
         * 
         * 大石头
         * ------WebKitFormBoundary3ZXeqQWNjAzojVR7
         * Content-Disposition: form-data; name="password"
         * 
         * 565656
         * ------WebKitFormBoundary3ZXeqQWNjAzojVR7
         * Content-Disposition: form-data; name="avatar"; filename="logo.png"
         * Content-Type: image/jpeg
         * 
         */

        // 前面加两个横杠，作为段分隔标记。末段分隔标记的末尾也有两个横杠
        while (pos >= 0)
        {
            // 分隔行以 CRLF 结尾才有后续数据；末段为“--boundary--”，到此结束
            var p1 = pos + bd.Length;
            if (p1 + 2 > total || body[p1] != (Byte)'\r' || body[p1 + 1] != (Byte)'\n') break;

            var start = p1 + 2;
            var next = -1;

            // 段内容（头部+数据）为共享切片，结尾的 CRLF 与分隔标记不属于数据
            var part = body.Slice(start, -1);
            try
            {
                // 段结束标记界定数据终点
                var q = part.IndexOf(bd2);
                if (q < 0) break;

                // 空行分隔头部与数据
                var ph = part.IndexOf(NewLine2);
                if (ph < 0 || ph + NewLine2.Length > q) break;

                // 头部区：首节点足够时零拷贝直读，跨段时拼入缓冲
                ReadOnlySpan<Byte> head = ph <= hbuf.Length
                    ? part.GetPrefix(hbuf, ph)
                    : part.GetPrefix(new Byte[ph], ph);
                var lines = head[..ph].ToStr().SplitAsDictionary(":", "\r\n");
                if (lines.TryGetValue("Content-Disposition", out var str))
                {
                    var ss = str.SplitAsDictionary("=", ";", true);
                    var name = ss["name"];
                    var fileName = ss["filename"];
                    if (!name.IsNullOrEmpty())
                    {
                        var offset = ph + NewLine2.Length;
                        var count = q - offset;
                        if (fileName.IsNullOrEmpty())
                        {
                            // 文本字段：直接解码为字符串；链式时跨段整体读取，避免多字节字符被段边界截断
                            dic[name] = part.Next == null
                                ? part.GetSpan().Slice(offset, count).ToStr()
                                : part.ReadBytes(offset, count).AsSpan().ToStr();
                        }
                        else
                        {
                            var file = new FormFile
                            {
                                Name = name,
                                FileName = fileName,
                                ContentDisposition = ss["[0]"],
                                ContentType = lines.TryGetValue("Content-Type", out var ct) ? ct : null,
                            };
                            // 文件数据为源包的共享切片（引用计数）：随表单对象存活，应由使用方显式释放；开发期（DEBUG）未释放时由析构兜底归还池化缓冲
                            file.Data = part.Slice(offset, count);
                            dic[name] = file;
                        }
                    }
                }

                // 下一段从段结束标记之后继续，+2 跳过 CRLF
                next = start + q + 2;
            }
            finally
            {
                part.TryDispose();
            }

            pos = next;
        }

        return dic;
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{Method} {RequestUri}";
}