#if !__CORE__
using System;
using System.Web;
using NewLife.Web;
using XCode.DataAccessLayer;

namespace XCode.Web
{
    /// <summary>页面查询执行时间模块</summary>
    public class DbRunTimeModule : RunTimeModule
    {
        /// <summary>执行时间字符串</summary>
        public static String DbRunTimeFormat { get; set; } = "查询{0}次，执行{1}次，耗时{2:n0}毫秒！";

        /// <summary>初始化模块，准备拦截请求。</summary>
        /// <param name="context"></param>
        protected override void OnInit(HttpContext context)
        {
            context.Items["DAL.QueryTimes"] = DAL.QueryTimes;
            context.Items["DAL.ExecuteTimes"] = DAL.ExecuteTimes;
        }

        /// <summary>输出</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override String Render(HttpContext context)
        {
            var ts = DateTime.Now - context.Timestamp;

            var StartQueryTimes = (Int32)context.Items["DAL.QueryTimes"];
            var StartExecuteTimes = (Int32)context.Items["DAL.ExecuteTimes"];

            return String.Format(DbRunTimeFormat, DAL.QueryTimes - StartQueryTimes, DAL.ExecuteTimes - StartExecuteTimes, ts.TotalMilliseconds);
        }
    }
}
#endif