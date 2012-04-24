using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using NewLife.IO;
using NewLife.Security;
using NewLife.Web;

namespace NewLife.Messaging
{
    /// <summary>Http客户端消息提供者</summary>
    public class HttpClientMessageProvider : MessageProvider
    {
        #region 属性
        private Uri _Uri;
        /// <summary>地址</summary>
        public Uri Uri { get { return _Uri; } set { _Uri = value; } }

        private WebClient _Client;
        /// <summary>客户端</summary>
        public WebClient Client
        {
            get
            {
                if (_Client == null)
                {
                    var client = new WebClientX();
                    //client.UploadDataCompleted += new UploadDataCompletedEventHandler(client_UploadDataCompleted);

                    _Client = client;
                }
                return _Client;
            }
            set
            {
                if (_Client != value)
                {
                    _Client = value;
                    //if (value != null) value.UploadDataCompleted += new UploadDataCompletedEventHandler(client_UploadDataCompleted);
                    _hasSetAsync = false;
                }
            }
        }
        #endregion

        #region 同步收发
        /// <summary>发送并接收消息。主要用于应答式的请求和响应。</summary>
        /// <param name="message"></param>
        /// <param name="millisecondsTimeout">等待的毫秒数，或为 <see cref="F:System.Threading.Timeout.Infinite" /> (-1)，表示无限期等待。默认0表示不等待</param>
        /// <returns></returns>
        public override Message SendAndReceive(Message message, int millisecondsTimeout = 0)
        {
            lock (this)
            {
                Byte[] rs = null;
                var data = message.GetStream().ReadBytes();
                if (data.Length < 128)
                    rs = Client.DownloadData(new Uri(Uri.ToString() + "?" + DataHelper.ToHex(data)));
                else
                    rs = Client.UploadData(Uri, data);
                if (rs == null || rs.Length < 1) return null;

                return Message.Read(new MemoryStream(rs));
            }
        }
        #endregion

        #region 异步收发
        Boolean _hasSetAsync;

        /// <summary>发送数据流。</summary>
        /// <param name="stream"></param>
        protected override void OnSend(Stream stream)
        {
            lock (this)
            {
                var client = Client;
                if (!_hasSetAsync)
                {
                    client.UploadDataCompleted += new UploadDataCompletedEventHandler(client_UploadDataCompleted);
                    client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(client_DownloadDataCompleted);
                }
                var data = stream.ReadBytes();
                if (data.Length < 128)
                    client.DownloadDataAsync(new Uri(Uri.ToString() + "?" + DataHelper.ToHex(data)));
                else
                    client.UploadDataAsync(Uri, data);
            }
        }

        void client_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e) { ProcessResponse(e, e.Result); }

        void client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e) { ProcessResponse(e, e.Result); }

        void ProcessResponse(AsyncCompletedEventArgs e, Byte[] result)
        {
            if (e.Error != null)
            {
                var msg = new ExceptionMessage() { Value = e.Error };
                Process(msg);
            }
            else if (result == null || result.Length <= 0)
            {
                Process(new NullMessage());
            }
            else
            {
                try
                {
                    var ms = new MemoryStream(result);
                    var msg = Message.Read(ms);
                    Process(msg);
                }
                catch (Exception ex)
                {
                    var msg = new ExceptionMessage() { Value = ex };
                    Process(msg);
                }
            }
        }
        #endregion
    }
}