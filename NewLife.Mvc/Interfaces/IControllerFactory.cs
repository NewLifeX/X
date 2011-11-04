namespace NewLife.Mvc
{
    /// <summary>控制器工厂接口，用于创建控制器</summary>
    /// <remarks>因为需要针对每一次请求创建实例，而控制器工厂只需要一个即可，避免每次创建控制器都需要反射</remarks>
    public interface IControllerFactory
    {
        /// <summary>
        /// 当前控制器工厂产生的控制器是否支持指定路径的请求
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool Support(string path);

        /// <summary>
        /// 创建控制器
        /// </summary>
        /// <returns></returns>
        IController Create();
    }
}