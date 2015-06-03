using System;
using NewLife.Web;

namespace NewLife.Bootstrap
{
    /// <summary>Bootstrap脚本提供者</summary>
    public class BootstrapJs : Js
    {
        /// <summary>重载Alert实现</summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns></returns>
        protected override void OnAlert(String message, String title)
        {
            message = "Bootstrap " + message;
            base.OnAlert(message, title);
        }
    }
}