using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net
{
    /// <summary>安全沙箱</summary>
    public class SandBoxServer : NetServer
    {
        #region 属性
        private string _Policy = "<cross-domain-policy><allow-access-from domain=\"*\" to-ports=\"*\" /></cross-domain-policy>\0";
        /// <summary>安全策略文件内容</summary>
        public string Policy { get { return _Policy; } set { _Policy = value; } }
        #endregion

        /// <summary>实例化一个安全沙箱服务器</summary>
        public SandBoxServer()
        {
            Port = 843;
            ProtocolType = ProtocolType.Tcp;
        }
        /// <summary>数据返回</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            var session = e.Session;
            string sss = e.GetString();
            if (sss == "<policy-file-request/>\0")
            {
                
                session.Send(System.Text.Encoding.UTF8.GetBytes(_Policy.ToCharArray()));
            }
            base.OnReceived(sender, e);
            session.Dispose();
        }
    }
}
