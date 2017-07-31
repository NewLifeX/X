using System;
using System.Web;
using NewLife.Web;
using XCode.Membership;

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

            HttpApplication.RegisterModule(typeof(ErrorModule));
            HttpApplication.RegisterModule(typeof(DbRunTimeModule));
            HttpApplication.RegisterModule(typeof(ManagerModule));
        }
    }
}