using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace NewLife.Web
{
    /// <summary>Http上下文</summary>
    public static class HttpContext
    {
        private static IHttpContextAccessor _accessor;

        /// <summary>当前Http上下文</summary>
        public static Microsoft.AspNetCore.Http.HttpContext Current => _accessor.HttpContext;

        internal static void Configure(IHttpContextAccessor accessor) => _accessor = accessor;
    }

    /// <summary>Http上下文扩展</summary>
    public static class StaticHttpContextExtensions
    {
        /// <summary>添加Http上下文访问器</summary>
        /// <param name="services"></param>
        public static void AddHttpContextAccessor(this IServiceCollection services) => services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        /// <summary>配置静态Http上下文访问器</summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStaticHttpContext(this IApplicationBuilder app)
        {
            var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            HttpContext.Configure(httpContextAccessor);
            return app;
        }
    }
}