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
        /// <param name="msDelay">延迟指定毫秒数以后自动关闭，默认0表示不关闭</param>
        /// <param name="kind">种类，info/success/error等</param>
        /// <returns></returns>
        protected override void OnAlert(String message, String title, Int32 msDelay, String kind)
        {
            var js = "";
            if (msDelay > 0)
                js = String.Format("(parent[\"tips\"] || window[\"tips\"] || (function(msg){{alert(msg);}}))(\"{0}\",true,{1},\"close\");", message, msDelay);
            else
                js = String.Format("(parent[\"infoDialog\"] || window[\"infoDialog\"] || (function(title,msg){{alert(msg);}}))(\"{0}\",\"{1}\",true);", title, message);

            WriteScript(js, true);
        }
    }
}