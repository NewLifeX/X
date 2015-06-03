using NewLife.Web;

namespace NewLife.Bootstrap
{
    /// <summary>Bootstrap脚本提供者</summary>
    public class BootstrapJs : Js
    {
        /// <summary>重载Alert实现</summary>
        /// <param name="msg"></param>
        protected override void OnAlert(string msg)
        {
            msg = "Bootstrap " + msg;
            base.OnAlert(msg);
        }
    }
}