using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Telnet
{
    /// <summary>Telnet服务器</summary>
    public class TelnetServer : NetServer<TelnetSession>
    {
        //TODO 未实现Telnet服务端

        #region 属性
        //private Dictionary<Int32, TelnetSession> _Sessions;
        ///// <summary>会话集合</summary>
        //public IDictionary<Int32, TelnetSession> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<int, TelnetSession>()); } }
        #endregion

        /// <summary>实例化一个Telnet服务器</summary>
        public TelnetServer()
        {
            Port = 23;
            ProtocolType = ProtocolType.Tcp;
        }
    }
}