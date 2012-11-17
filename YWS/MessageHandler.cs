using System;
using System.Web.SessionState;
using NewLife.CommonEntity;
using NewLife.Exceptions;
using NewLife.Messaging;

namespace NewLife.YWS.Entities
{
    /// <summary>消息处理器，用于接收通过Http通道传输的消息</summary>
    public class MessageHandler : HttpMessageProviderHandler, IRequiresSessionState
    {
        private static IMessageConsumer _Consumer;
        /// <summary>消息消费者</summary>
        public static IMessageConsumer Consumer { get { return _Consumer; } set { _Consumer = value; } }

        static MessageHandler()
        {
            Consumer = HttpServerMessageProvider.Instance.Register(88);
            Consumer.OnReceived += new EventHandler<MessageEventArgs>(Consumer_OnReceived);
        }

        static void Consumer_OnReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.Kind != MessageKind.Method) throw new XException("只支持请求{0}类型消息！", MessageKind.Method);
            var msg = e.Message as MethodMessage;
            var provider = CommonManageProvider.Provider;

            //// 特殊处理登录请求
            //if (msg.Header.SessionID <= 0 && (msg.TypeName == "Admin" || msg.TypeName == "Administrator") && msg.Name == "Login")
            //{
            //    var user = provider.Login((String)msg.Parameters[0], (String)msg.Parameters[1]);
            //    user.Properties["Password"] = null;
            //    var rs = new EntityMessage();
            //    rs.Value = user;
            //    rs.Header.SessionID = (Int32)user.ID;

            //    Consumer.Send(rs);
            //}
            //else
            //{
            //    if (Admin.Current == null) throw new XException("未登录！");

            //    // 反射调用目标方法
            //    if (msg.Kind != MessageKind.Method) throw new XException("不支持的消息类型Kind={0}！", msg.Kind);

            //    //// 不能直接调用，为了安全，增加验证，这里只允许调用Admin类的
            //    //if (msg.Type != typeof(Admin)) throw new XException("不允许调用{0}类的方法！", msg.Type);

            //    Consumer.Send(msg.Invoke());
            //}
        }
    }
}