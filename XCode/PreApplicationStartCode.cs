using System;
using System.Web;
using NewLife.Web;
using XCode.Web;

namespace XCode
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

            //var set = Setting.Current;
            //if (set.WebOnline || set.WebBehavior) HttpApplication.RegisterModule(typeof(UserBehaviorModule));
        }
    }
}