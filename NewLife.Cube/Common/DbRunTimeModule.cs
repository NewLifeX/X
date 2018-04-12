using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NewLife.Common;
using NewLife.Log;
using XCode.DataAccessLayer;

namespace NewLife.Cube
{
    /// <summary>页面查询执行时间模块</summary>
    public class DbRunTimeModule : IHttpModule
    {
        #region IHttpModule Members
        void IHttpModule.Dispose() { }

        /// <summary>初始化模块，准备拦截请求。</summary>
        /// <param name="context"></param>
        void IHttpModule.Init(HttpApplication context)
        {
            if (!Enable) return;

            context.BeginRequest += (s, e) => OnInit();
            //context.PostReleaseRequestState += (s, e) => OnEnd();
        }
        #endregion

        ///// <summary>上下文</summary>
        //public static HttpContext Context { get { return HttpContext.Current; } }

        /// <summary>执行时间字符串</summary>
        public static String DbRunTimeFormat { get; set; } = "查询{0}次，执行{1}次，耗时{2:n0}毫秒";

        const String _QueryTimes = "DAL.QueryTimes";
        const String _ExecuteTimes = "DAL.ExecuteTimes";

        /// <summary>初始化模块，准备拦截请求。</summary>
        void OnInit()
        {
            var ctx = HttpContext.Current;
            ctx.Items[_QueryTimes] = DAL.QueryTimes;
            ctx.Items[_ExecuteTimes] = DAL.ExecuteTimes;

            // 设计时收集执行的SQL语句
            if (SysConfig.Current.Develop) ctx.Items["XCode_SQLList"] = new List<String>();
        }

        private static Boolean _tip;
        /// <summary>获取执行时间和查询次数等信息</summary>
        /// <returns></returns>
        public static String GetInfo()
        {
            var ctx = HttpContext.Current;
            var ts = DateTime.Now - ctx.Timestamp;

            if (!ctx.Items.Contains(_QueryTimes) || !ctx.Items.Contains(_ExecuteTimes))
            {
                //throw new XException("设计错误！需要在web.config中配置{0}", typeof(DbRunTimeModule).FullName);
                if (!_tip)
                {
                    _tip = true;
                    XTrace.WriteLine("设计错误！需要在web.config中配置{0}", typeof(DbRunTimeModule).FullName);
                }
                return null;
            }

            var StartQueryTimes = (Int32)ctx.Items[_QueryTimes];
            var StartExecuteTimes = (Int32)ctx.Items[_ExecuteTimes];

            var inf = String.Format(DbRunTimeFormat, DAL.QueryTimes - StartQueryTimes, DAL.ExecuteTimes - StartExecuteTimes, ts.TotalMilliseconds);

            // 设计时收集执行的SQL语句
            if (SysConfig.Current.Develop)
            {
                var list = ctx.Items["XCode_SQLList"] as List<String>;
                if (list != null && list.Count > 0) inf += "<br />" + list.Select(e => HttpUtility.HtmlEncode(e)).Join("<br />" + Environment.NewLine);
            }

            return inf;
        }

        private static Boolean? _Enable;
        /// <summary>是否启用显示运行时间</summary>
        public static Boolean Enable
        {
            get
            {
                if (_Enable == null) _Enable = Setting.Current.ShowRunTime;
                return _Enable.Value;
            }
        }
    }
}