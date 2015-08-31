using System;
using NewLife.Web;

namespace NewLife.Bootstrap
{
    /// <summary>Bootstrap脚本提供者</summary>
    public class BootstrapJs : Js
    {
        /// <summary>重载Alert实现</summary>
        /// <param name="msg"></param>
        protected override void OnAlert(String message, String title, Int32 msDelay, String kind)
        {
            message = "Bootstrap " + message;
            base.OnAlert(message, title, msDelay, kind);
        }
    }
}