using System.Net;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Remoting;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>Http响应</summary>
public class HttpResponse : HttpBase
{
    #region 属性
    /// <summary>状态码</summary>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    /// <summary>状态描述</summary>
    public String? StatusDescription { get; set; }

    /// <summary>流式响应体。设置后发送时先发头部、再流式发送该流，不物化到内存；流所有权移交响应，发送完成后释放</summary>
    /// <remarks>
    /// <para>长度可知（可寻址流）时声明 Content-Length，未知长度时使用分块传输（Transfer-Encoding: chunked）。</para>
    /// <para>与 <see cref="HttpBase.Body"/> 二选一，本属性优先；<see cref="Build"/> 整包构建时会把流物化到 <see cref="HttpBase.Body"/> 并释放。</para>
    /// </remarks>
    public Stream? BodyStream { get; set; }
    #endregion

    /// <summary>分析第一行</summary>
    /// <param name="firstLine"></param>
    protected override Boolean OnParse(String firstLine)
    {
        if (firstLine.IsNullOrEmpty()) return false;

        // HTTP/1.1 502 Bad Gateway
        if (!firstLine.StartsWith("HTTP/")) return false;

        var ss = firstLine.Split(' ');
        //if (ss.Length < 3) throw new Exception("非法响应头 {0}".F(firstLine));
        if (ss.Length < 3) return false;

        Version = ss[0].TrimPrefix("HTTP/");

        // 分析响应码
        var code = ss[1].ToInt();
        if (code > 0) StatusCode = (HttpStatusCode)code;

        StatusDescription = ss.Skip(2).Join(" ");

        return true;
    }

    /// <summary>创建请求响应包。非成功状态且无主体时使用状态描述作为主体</summary>
    /// <returns></returns>
    public override IOwnerPacket Build()
    {
        // 流式响应体在整包构建时物化并释放（流式发送路径不会走到这里）
        var stream = BodyStream;
        if (stream != null)
        {
            BodyStream = null;
            if (Body == null) Body = (ArrayPacket)stream.ReadBytes(-1);
            stream.Dispose();
        }

        // 如果响应异常，则使用响应描述作为内容。无实体响应（204/304）除外：这类响应不得携带实体
        if (!HasNoEntity && StatusCode > HttpStatusCode.OK && Body == null && !StatusDescription.IsNullOrEmpty())
        {
            Body = (ArrayPacket)StatusDescription.GetBytes();
        }

        return base.Build();
    }

    /// <summary>是否为无实体响应。204/304 不得携带实体，也不应声明 Content-Length（RFC 7230 §3.3.2）</summary>
    protected override Boolean HasNoEntity => StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotModified;

    /// <summary>创建头部</summary>
    /// <param name="length"></param>
    /// <returns></returns>
    protected override String BuildHeader(Int32 length)
    {
        // 构建头部
        var sb = Pool.StringBuilder.Get();
        sb.AppendFormat("HTTP/{2} {0} {1}\r\n", (Int32)StatusCode, StatusDescription ?? StatusCode.ToString(), Version);

        //// cors
        //sb.AppendFormat("Access-Control-Allow-Origin:{0}\r\n", "*");
        //sb.AppendFormat("Access-Control-Allow-Methods:{0}\r\n", "POST, GET");
        //sb.AppendFormat("Access-Control-Allow-Headers:{0}\r\n", "Content-Type, Access-Control-Allow-Headers, Authorization, X-Requested-With");

        // 内容长度：存在主体明确长度；否则除非 Transfer-Encoding/Upgrade 才可省略，默认发送 0。
        // 204/304 不得携带实体，也不应声明 Content-Length（RFC 7230 §3.3.2）：
        // 这里显式删除而非仅不写入，避免调用方自行设置的 Content-Length 漏出
        if (HasNoEntity)
            Headers.Remove("Content-Length");
        else if (length > 0)
            Headers["Content-Length"] = length + "";
        else if (!Headers.ContainsKey("Content-Length") && !Headers.ContainsKey("Transfer-Encoding") && !Headers.ContainsKey("Upgrade"))
            Headers["Content-Length"] = "0";

        if (!ContentType.IsNullOrEmpty()) Headers["Content-Type"] = ContentType;

        foreach (var item in Headers)
        {
            sb.AppendFormat("{0}: {1}\r\n", item.Key, item.Value);
        }

        sb.Append("\r\n");

        return sb.Return(true);
    }

    /// <summary>验证，如果失败则抛出异常</summary>
    public void Valid()
    {
        if (StatusCode != HttpStatusCode.OK) throw new Exception(StatusDescription ?? (StatusCode + ""));
    }

    /// <summary>设置结果，影响Body和ContentType</summary>
    /// <param name="result">业务结果或异常</param>
    /// <param name="contentType">指定内容类型，缺省时按类型推断</param>
    public void SetResult(Object result, String? contentType = null)
    {
        if (result == null) return;

        switch (result)
        {
            case Exception ex:
                SetExceptionResult(ex);
                return;
            case ISpanSerializable span:
                contentType ??= "application/octet-stream";
                Body = span.ToPacket();
                break;
            case IAccessor accessor:
                contentType ??= "application/octet-stream";

                var ms = new MemoryStream();
                accessor.Write(ms, null);
                ms.Position = 0;
                Body = new ArrayPacket(ms);
                break;
            case IPacket pk:
                contentType ??= "application/octet-stream";
                Body = pk;
                break;
            case Byte[] buffer:
                contentType ??= "application/octet-stream";
                Body = (ArrayPacket)buffer;
                break;
            case Stream stream:
                contentType ??= "application/octet-stream";
                // 物化到内存；大文件流式发送请改用 BodyStream（长度可知时 Content-Length，否则分块传输）
                Body = (ArrayPacket)stream.ReadBytes(-1);
                break;
            case String str:
                contentType ??= "text/html";
                Body = (ArrayPacket)str.GetBytes();
                break;
            default:
                contentType ??= "application/json";
                Body = (ArrayPacket)result.ToJson().GetBytes();
                break;
        }

        if (ContentType.IsNullOrEmpty()) ContentType = contentType;
    }

    /// <summary>设置异常结果，自动映射ApiException到对应状态码</summary>
    /// <param name="ex">异常</param>
    private void SetExceptionResult(Exception ex)
    {
        if (ex is ApiException aex)
            StatusCode = (HttpStatusCode)aex.Code;
        else
            StatusCode = HttpStatusCode.InternalServerError;

        StatusDescription = ex.Message;
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"HTTP/{Version} {(Int32)StatusCode} {StatusDescription ?? (StatusCode + "")}";
}