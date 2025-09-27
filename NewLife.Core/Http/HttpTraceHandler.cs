using System.Net.Http;
using NewLife.Log;

namespace NewLife.Http;

/// <summary>支持APM跟踪的HttpClient处理器</summary>
/// <remarks>实例化一个支持APM的HttpClient处理器</remarks>
/// <param name="innerHandler">内部处理器</param>
public class HttpTraceHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    #region 属性
    /// <summary>APM跟踪器</summary>
    public ITracer? Tracer { get; set; }

    /// <summary>异常过滤器。仅记录满足条件的异常，默认空记录所有异常</summary>
    public Predicate<Exception>? ExceptionFilter { get; set; }
    #endregion

    /// <summary>发送请求</summary>
    /// <param name="request">请求消息</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>响应消息</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var tracer = Tracer;
        var uri = request.RequestUri;
        // 没有跟踪器或无Uri时，直接透传
        if (tracer == null || uri == null) return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // 如果父级已经做了ApiHelper.Invoke埋点，这里不需要再做一次
        var parent = DefaultSpan.Current;
        if (parent != null && parent.Tag == uri + "" || request.Headers.Contains("traceparent"))
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        using var span = tracer.NewSpan(request); // NewSpan 扩展内部会 Attach 头部并设置初始 Tag / Value
        try
        {
            // 任何层级，只要是通用库代码，await 时都应该调用 ConfigureAwait(false)
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (tracer.Resolver is DefaultTracerResolver resolver && resolver.RequestContentAsTag)
                span?.AppendTag(response);

            return response;
        }
        catch (Exception ex)
        {
            if (ExceptionFilter == null || ExceptionFilter(ex)) span?.SetError(ex, null);
            throw;
        }
    }
}