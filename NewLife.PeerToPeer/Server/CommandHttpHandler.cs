//using System.Web;
//using NewLife.Web;

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
//            CommandServer.Instance.Process(stream);
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