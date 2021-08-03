using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.Http
{
    /// <summary>控制器处理器</summary>
    public class ControllerHandler : IHttpHandler
    {
        #region 属性
        /// <summary>控制器类型</summary>
        public Type ControllerType { get; set; }
        #endregion

        /// <summary>处理请求</summary>
        /// <param name="context"></param>
        public void ProcessRequest(IHttpContext context) => throw new NotImplementedException();
    }
}
