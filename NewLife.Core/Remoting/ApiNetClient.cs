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
        public ApiNetClient(NetUri uri)
        {
            Client = uri.CreateRemote();
        }
        #endregion

        #region 方法
        public void Open()
        {
        }

        public void Close()
        {
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}