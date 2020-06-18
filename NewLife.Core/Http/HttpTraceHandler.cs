using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Http
{
    /// <summary>支持APM跟踪的HttpClient处理器</summary>
    public class HttpTraceHandler : DelegatingHandler
    {
        #region 属性
        /// <summary>APM跟踪器</summary>
        public ITracer Tracer { get; set; }
        #endregion

        /// <summary>实例化一个支持APM的HttpClient处理器</summary>
        /// <param name="innerHandler"></param>
        public HttpTraceHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        /// <summary>发送请求</summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            var span = Tracer?.NewSpan(uri.PathAndQuery);
            span.Attach(request);
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                span?.SetError(ex, uri + "");

                throw;
            }
            finally
            {
                span?.Dispose();
            }
        }
    }
}