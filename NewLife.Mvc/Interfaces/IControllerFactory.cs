namespace NewLife.Mvc
{
    /// <summary>控制器工厂接口，用于创建控制器</summary>
    /// <remarks>因为需要针对每一次请求创建实例，而控制器工厂只需要一个即可，避免每次创建控制器都需要反射</remarks>
    public interface IControllerFactory
    {
        /// <summary>返回实现 <see cref="T:NewLife.Mvc.IController" /> 接口的类的实例。</summary>
        /// <returns>处理请求的新的 <see cref="T:NewLife.Mvc.IController" /> 对象。</returns>
        /// <param name="context"><see cref="T:NewLife.Mvc.IRouteContext" /> 类的实例，它提供对用于为 HTTP 请求提供服务的内部服务器对象的引用。</param>
        IController GetController(IRouteContext context);

        /// <summary>使工厂可以重用现有的控制器实例。</summary>
        /// <param name="handler">要重用的 <see cref="T:NewLife.Mvc.IController" /> 对象。</param>
        void ReleaseController(IController handler);
    }
}