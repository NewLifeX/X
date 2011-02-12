using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using NewLife.Security;

namespace NewLife.IO
{
    /// <summary>
    /// 基于Http协议的数据流客户端
    /// </summary>
    public class HttpStreamClient : StreamClient
    {
        #region 属性
        private WebClient _Client;
        /// <summary>客户端</summary>
        public WebClient Client
        {
            get
            {
                if (_Client == null)
                {
                    WebClient client = new WebClient();
                    client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(client_DownloadDataCompleted);
                    client.UploadDataCompleted += new UploadDataCompletedEventHandler(client_UploadDataCompleted);
                    _Client = client;
                }
                return _Client;
            }
            set { _Client = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        public HttpStreamClient() { }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="uri"></param>
        public HttpStreamClient(Uri uri) : base(uri) { }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="url"></param>
        public HttpStreamClient(String url) : base(url) { }
        #endregion

        #region 发送数据
        /// <summary>
        /// 同步发送数据
        /// </summary>
        /// <param name="data">待发送数据</param>
        /// <returns>服务端响应数据</returns>
        protected override byte[] Send(byte[] data)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException("data");

            // 小数据使用GET方式传输，大数据使用POST方式传输
            if (data.Length < 100)
            {
                String url = Uri.ToString();
                if (!url.Contains("?")) url += "?";
                url += DataHelper.ToHex(data);
                return Client.DownloadData(url);
            }
            else
                return Client.UploadData(Uri, data);
        }

        /// <summary>
        /// 异步发送数据，服务端响应数据将由数据流总线处理
        /// </summary>
        /// <param name="data">待发送数据</param>
        protected override void SendAsync(byte[] data)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException("data");

            // 小数据使用GET方式传输，大数据使用POST方式传输
            if (data.Length < 100)
            {
                String url = Uri.ToString();
                if (!url.Contains("?")) url += "?";
                url += DataHelper.ToHex(data);
                Client.DownloadDataAsync(new Uri(url), null);
            }
            else
                Client.UploadDataAsync(Uri, data);
        }
        #endregion

        #region 数据流处理
        void client_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            Process(new MemoryStream(e.Result));
        }

        void client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            Process(new MemoryStream(e.Result));
        }
        #endregion
    }
}