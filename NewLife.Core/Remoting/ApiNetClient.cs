using System;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Net;

namespace NewLife.Remoting
{
    class ApiNetClient : IApiClient
    {
        #region 属性
        public NetUri Remote { get; set; }

        public ISocketClient Client { get; set; }
        #endregion

        #region 构造
        #endregion

        #region 方法
        public virtual Boolean Init(Object config)
        {
            var uri = config as NetUri;
            if (uri == null) return false;

            Client = uri.CreateRemote();
            Remote = uri;

            return true;
        }

        public void Open()
        {
            Client.Log = Log;
#if DEBUG
            //Client.LogSend = true;
            //Client.LogReceive = true;
#endif
            Client.Open();
        }

        public void Close()
        {
            Client.Close();
        }

        public Task<Byte[]> SendAsync(Byte[] data)
        {
            return Client.SendAsync(data);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}