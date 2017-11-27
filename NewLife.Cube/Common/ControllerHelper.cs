using System;
using System.Web.Mvc;

namespace NewLife.Cube
{
    /// <summary>控制器帮助类</summary>
    public static class ControllerHelper
    {
        #region Json响应
        /// <summary>返回结果并跳转</summary>
        /// <param name="data">结果。可以是错误文本、成功文本、其它结构化数据</param>
        /// <param name="url">提示信息后跳转的目标地址，[refresh]表示刷新当前页</param>
        /// <returns></returns>
        public static ActionResult JsonTips(Object data, String url = null)
        {
            var vr = new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
            //vr.Data = data;
            //vr.ContentType = contentType;
            //vr.ContentEncoding = contentEncoding;

            if (data is Exception ex)
                vr.Data = new { result = false, data = ex.GetTrue()?.Message, url };
            else
                vr.Data = new { result = true, data = data, url };

            return vr;
        }

        /// <summary>返回结果并刷新</summary>
        /// <param name="data">消息</param>
        /// <returns></returns>
        public static ActionResult JsonRefresh(Object data)
        {
            return JsonTips(data, "[refresh]");
        }
        #endregion
    }
}