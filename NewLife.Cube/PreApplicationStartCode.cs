using System;
using System.Web;
using NewLife.Web;
using XCode.Web;

namespace NewLife.Cube
{
    /// <summary>应用启动代码</summary>
    public static class PreApplicationStartCode
    {
        private static Boolean _startWasCalled;

        /// <summary>Registers pre-application start code for web pages.</summary>
        public static void Start()
        {
            if (_startWasCalled) return;
            _startWasCalled = true;

#if !NET4
            HttpApplication.RegisterModule(typeof(ErrorModule));
            HttpApplication.RegisterModule(typeof(DbRunTimeModule));

            var set = Setting.Current;
            if (set.WebOnline || set.WebBehavior || set.WebStatistics)
            {
                UserBehaviorModule.WebOnline = set.WebOnline;
                UserBehaviorModule.WebBehavior = set.WebBehavior;
                UserBehaviorModule.WebStatistics = set.WebStatistics;
                HttpApplication.RegisterModule(typeof(UserBehaviorModule));
            }

            if (set.ForceSSL) HttpApplication.RegisterModule(typeof(CubeModule));
#endif
        }
    }
}