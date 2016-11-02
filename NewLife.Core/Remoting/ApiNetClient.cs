using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public Boolean Init(Object config)
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
            Client.LogSend = true;
            Client.LogReceive = true;
#endif
            Client.Received += Client_Received;
            Client.Open();
        }

        public void Close()
        {
            Client.Close();
        }

        public Task<Byte[]> SendAsync(Byte[] data)
        {
            Client.SendAsync(data);

            _src = new TaskCompletionSource<Byte[]>();

            return _src.Task;
        }

        TaskCompletionSource<Byte[]> _src;

        private void Client_Received(Object sender, ReceivedEventArgs e)
        {
            if (_src != null) _src.SetResult(e.Data.ReadBytes(e.Length));
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}