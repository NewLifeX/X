using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace NewLife.Update
{
    /// <summary>
    /// 更新处理器
    /// </summary>
    public class UpdateHandler : IHttpHandler
    {
        #region IHttpHandler 成员
        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            
        }
        #endregion

        #region 方法
        void Process(HttpRequest Request)
        {
        }

        protected virtual String GetVer()
        {
            return null;
        }
        #endregion
    }
}