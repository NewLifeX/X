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
        /// <param name="delaySecond">延迟指定秒数以后自动关闭，默认0表示不关闭</param>
        /// <param name="kind">种类，info/success/error等</param>
        /// <returns></returns>
        protected override void OnAlert(String message, String title, Int32 delaySecond, String kind)
        {
            var script = Encode(string.Format("(parent[\"infoDialog\"] || window[\"infoDialog\"] || (function(title,msg){{alert(msg);}}))(\"{0}\",\"{1}\",true);", title, message));
            WriteScript(script, true);
        }
    }
}