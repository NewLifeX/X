using System;
using System.IO;
using System.Web;
using System.Web.SessionState;
using NewLife.CommonEntity;
using NewLife.Exceptions;
using NewLife.IO;
using NewLife.Messaging;
using NewLife.Security;

namespace NewLife.YWS.Entities
{
    /// <summary>消息处理器，用于接收通过Http通道传输的消息</summary>
    public class MessageHandler : IHttpHandler, IRequiresSessionState
    {
        /// <summary>处理消息</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual Message Process(Message message)
        {
            // 特殊处理登录请求
            if (message.Kind == YWS + 1)
            {
                var msg = message as LoginRequest;
                var admin = Administrator.Login(msg.UserName, msg.Password);
                var rs = new LoginResponse();
                rs.Admin = admin;
                return rs;
            }
            else
            {
                if (Admin.Current == null) throw new XException("未登录！");

                // 反射调用目标方法
                if (message.Kind == MessageKind.Method)
                {
                    //return (message as MethodMessage).Invoke();
                    // 不能直接调用，为了安全，增加验证，这里只允许调用Admin类的
                    var msg = message as MethodMessage;
                    if (msg.Type != typeof(Admin)) throw new XException("不允许调用{0}类的方法！", msg.Type);

                    return msg.Invoke();
                }
                else
                    throw new XException("不支持的消息类型Kind={0}！", message.Kind);
            }
        }

        #region IHttpHandler 成员
        bool IHttpHandler.IsReusable { get { return false; } }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            Message rs;
            try
            {
                var s = context.Request.InputStream;
                if (s == null || s.Position >= s.Length)
                {
                    var d = context.Request.Url.Query;
                    if (!String.IsNullOrEmpty(d))
                    {
                        if (d.StartsWith("?")) d = d.Substring(1);
                        s = new MemoryStream(DataHelper.FromHex(d));
                    }
                }
                var msg = Message.Read(s);
                rs = Process(msg);
            }
            catch (Exception ex)
            {
                rs = new ExceptionMessage() { Value = ex };
            }
            if (rs != null)
            {
                var data = rs.GetStream().ReadBytes();
                context.Response.BinaryWrite(data);
            }
        }
        #endregion

        #region 消息
        static readonly MessageKind YWS = MessageKind.UserDefine + 50;

        class LoginRequest : Message
        {
            public override MessageKind Kind { get { return YWS + 1; } }

            private String _UserName;
            /// <summary>用户名</summary>
            public String UserName { get { return _UserName; } set { _UserName = value; } }

            private String _Password;
            /// <summary>密码</summary>
            public String Password { get { return _Password; } set { _Password = value; } }
        }

        class LoginResponse : Message
        {
            public override MessageKind Kind { get { return YWS + 2; } }

            private IAdministrator _Admin;
            /// <summary>已登录的管理员对象</summary>
            public IAdministrator Admin { get { return _Admin; } set { _Admin = value; } }
        }

        //class TestMessage : Message
        //{
        //    public override MessageKind Kind { get { return YWS + 2; } }

        //    private String _Text;
        //    /// <summary>文本</summary>
        //    public String Text { get { return _Text; } set { _Text = value; } }
        //}
        #endregion
    }
}