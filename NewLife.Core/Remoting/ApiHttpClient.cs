using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Web;

namespace NewLife.Remoting
{
    class ApiHttpClient : IApiClient
    {
        public WebClient Client { get; set; }

        public String Remote { get; set; }

        public Boolean Init(Object config)
        {
            var url = config as String;
            if (url.IsNullOrEmpty()) return false;

            Client = new WebClientX();
            Remote = url;

            return true;
        }

        public void Open()
        {
        }

        public void Close()
        {
        }

        /// <summary>发送数据</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<Byte[]> SendAsync(Byte[] data)
        {
            return Client.UploadDataTaskAsync(Remote, data);
        }


        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}