//using System.Web;
//using NewLife.Web;
//using NewLife.Messaging;
//using NewLife.PeerToPeer.Messages;
//using System;
//using NewLife.Net.Sockets;
//using System.IO;
//using System.Net;

//namespace NewLife.PeerToPeer.Server
//{
//    /// <summary>
//    /// Http命令处理器
//    /// </summary>
//    public class CommandHttpHandler : IHttpHandler
//    {
//        /// <summary>
//        /// 处理请求
//        /// </summary>
//        /// <param name="context"></param>
//        public void ProcessRequest(HttpContext context)
//        {
//            // 可以写一个HttpMessageHandler，然后Web.Config中配置映射即可
//            HttpStream stream = new HttpStream(context);
//            //Message.RegisterFactory((Int32)MessageTypes.Ping, Activator.CreateInstance(typeof(PingMessage)) as PingMessage);
//            CommandServer CS = new CommandServer();

//            CS.Ping += delegate(Object sender, EventArgs<Message, Stream> e)
//            {
//                PingMessage pm = e.Arg1 as PingMessage;
//                CS.OnlineUser.Save(pm.Token, pm.Private, context.Request.UserHostAddress);
//                P2PMessage.ReceivedMessageProcess(sender, e, context.Response.OutputStream);
//            };

//            P2PMessage.Process(stream);
//        }

//        /// <summary>
//        /// 是否可以重用
//        /// </summary>
//        public bool IsReusable
//        {
//            get
//            {
//                return true;
//            }
//        }
//    }
//}