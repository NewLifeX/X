using System;
using System.IO;
using NewLife.Messaging;
using NewLife.PeerToPeer.Messages;
using System.Net;
using System.Configuration;

namespace NewLife.PeerToPeer.Client
{
    /// <summary>
    /// 命令客户端
    /// </summary>
    public class CommandClient //: MessageServer
    {
        #region 属性
        private String _ServerAddress;
        /// <summary>服务器地址</summary>
        public String ServerAddress
        {
            get { return _ServerAddress; }
            set { _ServerAddress = value; }
        }
        #endregion

        #region 实例
        private static CommandClient _Instance;
        /// <summary>实例</summary>
        public static CommandClient Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new CommandClient();
                    _Instance.ServerAddress = ConfigurationManager.AppSettings["NewLife.PeerToPeer.CommandServer"];
                }
                return _Instance;
            }
        }
        #endregion

        #region 处理
        ///// <summary>
        ///// 已重载。
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //protected override void OnReceived(object sender, EventArgs<Message, Stream> e)
        //{
        //    P2PMessage msg = e.Arg1 as P2PMessage;
        //    if (msg == null) return;

        //    switch (msg.MessageType)
        //    {
        //        case MessageTypes.TestResponse:
        //            if (Test != null) Test(sender, e);
        //            break;
        //        case MessageTypes.PingResponse:
        //            if (Ping != null) Ping(sender, e);
        //            break;
        //        case MessageTypes.FindTorrentResponse:
        //            if (FindTorrent != null) FindTorrent(sender, e);
        //            break;
        //        case MessageTypes.TextResponse:
        //            if (Text != null) Text(sender, e);
        //            break;
        //        default:
        //            break;
        //    }
        //}
        #endregion

        #region 事件
        ///// <summary>
        ///// 
        ///// </summary>
        //public event EventHandler<EventArgs<Message, Stream>> Ping;

        ///// <summary>
        ///// 
        ///// </summary>
        //public event EventHandler<EventArgs<Message, Stream>> FindTorrent;

        ///// <summary>
        ///// 
        ///// </summary>
        //public event EventHandler<EventArgs<Message, Stream>> Test;

        ///// <summary>
        ///// 
        ///// </summary>
        //public event EventHandler<EventArgs<Message, Stream>> Text;
        #endregion

        #region 发送
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Byte[] Send(Byte[] buffer)
        {
            if (String.IsNullOrEmpty(ServerAddress)) throw new Exception("未指定命令服务器地址！");

            WebClient client = new WebClient();
            //return client.UploadData(ServerAddress, buffer);
            return client.DownloadData(String.Format("{0}?{1}", ServerAddress, ToHex(buffer)));
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Message Send(Message message)
        {
            Byte[] buffer = Send(message.ToArray());
            if (buffer == null || buffer.Length < 1) return null;

            MemoryStream ms = new MemoryStream(buffer);
            return Message.Deserialize(ms);
        }
        #endregion

        #region 编码
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static String ToHex(Byte[] data)
        {
            if (data == null || data.Length < 1) return null;

            return BitConverter.ToString(data).Replace("-", null);
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] FromHex(String data)
        {
            if (String.IsNullOrEmpty(data)) return null;

            Byte[] bts = new Byte[data.Length / 2];
            for (int i = 0; i < data.Length / 2; i++)
            {
                bts[i] = (Byte)Convert.ToInt32(data.Substring(2 * i, 2), 16);
            }
            return bts;
        }
        #endregion
    }
}