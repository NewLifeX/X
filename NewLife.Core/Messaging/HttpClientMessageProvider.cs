using System;
using System.IO;
using System.Net;
using NewLife.IO;

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
                    var client = new WebClient();
                    client.UploadDataCompleted += new UploadDataCompletedEventHandler(client_UploadDataCompleted);

                    _Client = client;
                }
                return _Client;
            }
            set
            {
                if (_Client != value)
                {
                    _Client = value;
                    if (value != null) value.UploadDataCompleted += new UploadDataCompletedEventHandler(client_UploadDataCompleted);
                }
            }
        }
        #endregion

        #region 异步接收
        void client_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                var msg = new ExceptionMessage() { Value = e.Error };
                Process(msg);
            }
            else if (e.Result == null || e.Result.Length <= 0)
            {
                Process(new NullMessage());
            }
            else
            {
                try
                {
                    var ms = new MemoryStream(e.Result);
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

        /// <summary>发送消息。如果有响应，可在消息到达事件中获得。</summary>
        /// <param name="message"></param>
        public override void Send(Message message)
        {
            Client.UploadDataAsync(Uri, message.GetStream().ReadBytes());
        }
    }
}