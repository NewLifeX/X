namespace NewLife.Mvc
{
    /// <summary>控制器接口</summary>
    public interface IController
    {
        /// <summary>通过实现 <see cref="T:NewLife.Mvc.IController" /> 接口的自定义 Controller 启用 HTTP Web 请求的处理。</summary>
        /// <param name="context"><see cref="T:NewLife.Mvc.IRouteContext" /> 对象，它提供对用于为 HTTP 请求提供服务的内部服务器对象的引用。</param>
        void ProcessRequest(IRouteContext context);

        /// <summary>获取一个值，该值指示其他请求是否可以使用 <see cref="T:NewLife.Mvc.IController" /> 实例。</summary>
        /// <returns>如果 <see cref="T:NewLife.Mvc.IController" /> 实例可再次使用，则为 true；否则为 false。</returns>
        bool IsReusable { get; }
    }
}