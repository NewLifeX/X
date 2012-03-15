using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Messaging;
using NewLife.CommonEntity;

public partial class MessageClientTest : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (mp == null)
        {
            HttpClientMessageProvider client = new HttpClientMessageProvider();
            client.Uri = new Uri("http://localhost:8/Web/MessageHandler.ashx");
            mp = client;
        }
    }

    static IMessageProvider mp;
    protected void Button1_Click(object sender, EventArgs e)
    {
        LoginRequest request = new LoginRequest();
        request.UserName = "admin";
        request.Password = "admin";

        Message msg = mp.SendAndReceive(request, 0);
        LoginResponse rs = msg as LoginResponse;
        Response.Write("返回：" + rs.Admin);
    }

    protected void Button2_Click(object sender, EventArgs e)
    {

    }

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
    #endregion
}