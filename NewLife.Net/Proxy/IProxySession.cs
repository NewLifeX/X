using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;
using NewLife.Net.Common;

namespace NewLife.Net.Proxy
{
    /// <summary>代理会话接口。客户端的一次转发请求（或者Tcp连接），就是一个会话。转发的全部操作都在会话中完成。</summary>
    /// <remarks>
    /// 一个会话应该包含两端，两个Socket，服务端和客户端。
    /// 客户端<see cref="INetSession.Session"/>发来的数据，在这里经过一系列过滤器后，转发给服务端<see cref="Remote"/>；
    /// 服务端<see cref="Remote"/>返回的数据，在这里经过过滤器后，转发给客户端<see cref="INetSession.Session"/>。
    /// 会话进行业务处理的过程中，可以通过多个SendRemote方法向远程服务端发送数据。
    /// </remarks>
    public interface IProxySession : INetSession
    {
        #region 属性
        /// <summary>代理对象</summary>
        IProxy Proxy { get; set; }

        /// <summary>远程客户端。跟目标服务端通讯的那个Socket，其实是客户端TcpClientX/UdpClientX</summary>
        ISocketSession Remote { get; set; }

        ///// <summary>远程客户端IP终结点</summary>
        //IPEndPoint RemoteEndPoint { get; set; }

        ///// <summary>服务端协议。默认与客户端协议相同</summary>
        //ProtocolType RemoteProtocolType { get; set; }

        /// <summary>服务端地址</summary>
        NetUri RemoteUri { get; }
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        IProxySession SendRemote(byte[] buffer, int offset = 0, int size = 0);

        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        IProxySession SendRemote(Stream stream);

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        IProxySession SendRemote(string msg, Encoding encoding = null);
        #endregion
    }
}