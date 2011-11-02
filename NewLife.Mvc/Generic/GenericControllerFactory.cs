using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Mvc
{
    /// <summary>一般控制器工厂接口</summary>
    public class GenericControllerFactory : IControllerFactory
    {
        #region IControllerFactory 成员
        /// <summary>
        /// 当前控制器工厂产生的控制器是否支持指定路径的请求
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Support(string path)
        {
            return true;
        }
        /// <summary>
        /// 创建控制器
        /// </summary>
        /// <returns></returns>
        public IController Create() { return new GenericController(); }
        #endregion

    }
}