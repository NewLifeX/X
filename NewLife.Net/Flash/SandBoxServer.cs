using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net
{
    /// <summary>安全沙箱</summary>
    public class SandBoxServer : NetServer
    {
        #region 属性
        //private Dictionary<Int32, FTPSession> _Sessions;
        ///// <summary>会话集合</summary>
        //public IDictionary<Int32, FTPSession> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<int, FTPSession>()); } }
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

            base.OnReceived(sender, e);
            string policy = "<cross-domain-policy><allow-access-from domain=\"*\" to-ports=\"7000\" /></cross-domain-policy>\0";
            session.Send(System.Text.Encoding.UTF8.GetBytes(policy.ToCharArray()));
        }
    }
}
