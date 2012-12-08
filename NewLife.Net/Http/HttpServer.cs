using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Http
{
    /// <summary>Http服务器</summary>
    public class HttpServer : NetServer<HttpSession>
    {
        //TODO 未实现Http服务端

        #region 属性
        //private Dictionary<Int32, HttpSession> _Sessions;
        ///// <summary>会话集合</summary>
        //public IDictionary<Int32, HttpSession> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<int, HttpSession>()); } }
        #endregion

        /// <summary>实例化一个Http服务器</summary>
        public HttpServer()
        {
            Port = 80;
            ProtocolType = ProtocolType.Tcp;
        }
    }
}