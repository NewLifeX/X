using System;
using System.IO;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.PeerToPeer.Messages;

namespace NewLife.PeerToPeer.Server
{
    /// <summary>
    /// 命令服务器
    /// </summary>
    public class CommandServer : MessageServer
    {
        //#region 构造
        //static CommandServer()
        //{
        //    P2PMessage.Init();
        //}

        //CommandServer()
        //{
        //    //MessageHandler.Instance.MessageReceived += new EventHandler<EventArgs<Message, Stream>>(Instance_MessageReceived);
        //    Message.Received += new EventHandler<EventArgs<Message, Stream>>(Message_Received);
        //}

        ///// <summary>
        ///// 析构，取消事件注册
        ///// </summary>
        //~CommandServer()
        //{
        //    //MessageHandler.Instance.MessageReceived -= new EventHandler<EventArgs<Message, Stream>>(Instance_MessageReceived);
        //    Message.Received += new EventHandler<EventArgs<Message, Stream>>(Message_Received);
        //}
        //#endregion

        //#region 实例
        //private static CommandServer _Instance;
        ///// <summary>实例</summary>
        //public static CommandServer Instance
        //{
        //    get { return _Instance ?? (_Instance = new CommandServer()); }
        //}
        //#endregion

        #region 处理
        ///// <summary>
        ///// 处理数据流
        ///// </summary>
        ///// <param name="stream"></param>
        //public void Process(Stream stream)
        //{
        //    try
        //    {
        //        Message.Process(stream, MessageExceptionOption.Throw);
        //    }
        //    catch (Exception ex)
        //    {
        //        XTrace.WriteLine(ex.ToString());
        //    }
        //}

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, EventArgs<Message, Stream> e)
        {
            P2PMessage msg = e.Arg1 as P2PMessage;
            if (msg == null) return;

            switch (msg.MessageType)
            {
                case MessageTypes.Test:
                    if (Test != null) Test(sender, e);
                    break;
                case MessageTypes.Ping:
                    if (Ping != null) Ping(sender, e);
                    break;
                case MessageTypes.FindTorrent:
                    if (FindTorrent != null) FindTorrent(sender, e);
                    break;
                case MessageTypes.Text:
                    if (Text != null) Text(sender, e);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region 事件
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventArgs<Message, Stream>> Ping;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventArgs<Message, Stream>> FindTorrent;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventArgs<Message, Stream>> Test;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventArgs<Message, Stream>> Text;
        #endregion
    }
}