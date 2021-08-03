using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.Http
{
    /// <summary>静态文件处理器</summary>
    public class StaticFilesHandler : IHttpHandler
    {
        #region 属性
        /// <summary>内容目录</summary>
        public String ContentPath { get; set; }
        #endregion

        /// <summary>处理请求</summary>
        /// <param name="context"></param>
        public void ProcessRequest(IHttpContext context) => throw new NotImplementedException();
    }
}