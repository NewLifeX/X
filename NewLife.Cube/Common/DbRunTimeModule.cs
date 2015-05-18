using System;
using System.Web;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Web;
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

        /// <summary>上下文</summary>
        public static HttpContext Context { get { return HttpContext.Current; } }

        private static String _RunTimeFormat = "查询{0}次，执行{1}次，耗时{2:n0}毫秒";
        /// <summary>执行时间字符串</summary>
        public static String DbRunTimeFormat { get { return _RunTimeFormat; } set { _RunTimeFormat = value; } }

        /// <summary>初始化模块，准备拦截请求。</summary>
        void OnInit()
        {
            Context.Items["DAL.QueryTimes"] = DAL.QueryTimes;
            Context.Items["DAL.ExecuteTimes"] = DAL.ExecuteTimes;
        }

        /// <summary>获取执行时间和查询次数等信息</summary>
        /// <returns></returns>
        public static String GetInfo()
        {
            TimeSpan ts = DateTime.Now - HttpContext.Current.Timestamp;

            Int32 StartQueryTimes = (Int32)Context.Items["DAL.QueryTimes"];
            Int32 StartExecuteTimes = (Int32)Context.Items["DAL.ExecuteTimes"];

            return String.Format(DbRunTimeFormat, DAL.QueryTimes - StartQueryTimes, DAL.ExecuteTimes - StartExecuteTimes, ts.TotalMilliseconds);
        }

        private static Boolean? _Enable;
        /// <summary>是否启用显示运行时间</summary>
        public static Boolean Enable
        {
            get
            {
                if (_Enable == null) _Enable = Config.GetConfig<Boolean>("NewLife.Cube.ShowRunTime", XTrace.Debug);
                return _Enable.Value;
            }
        }

        //public static void Init()
        //{
        //    if (Enable)
        //    {
        //        HttpApplication.RegisterModule();
        //    }
        //}
    }
}