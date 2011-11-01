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
        /// 创建控制器
        /// </summary>
        /// <returns></returns>
        public IController Create() { return new GenericController(); }
        #endregion
    }
}