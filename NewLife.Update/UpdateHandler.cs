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
            get { throw new NotImplementedException(); }
        }

        public void ProcessRequest(HttpContext context)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}