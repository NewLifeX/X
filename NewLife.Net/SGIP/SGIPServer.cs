using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.SGIP
{
    /// <summary>SGIP服务器</summary>
    public class SGIPServer : NetServer
    {
        #region 构造
        /// <summary>实例化</summary>
        public SGIPServer()
        {
            ProtocolType = ProtocolType.Tcp;
            Port = 8801;
        }
        #endregion
    }
}