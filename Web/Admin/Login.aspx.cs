using System;
using System.Threading;
using System.Web;
using NewLife.CommonEntity;
using NewLife.Log;
using NewLife.Threading;
using NewLife.Web;
using XCode;

public partial class Admin_Login : System.Web.UI.Page
{
    static Admin_Login()
    {
        // 引发反向工程
        ThreadPoolX.QueueUserWorkItem(delegate() { Int32 count = EntityFactory.CreateOperate(Provider.ManageUserType).Cache.Entities.Count; });
    }

    public static IManageProvider Provider { get { return ManageProvider.Provider; } }

    protected void Page_Load(object sender, EventArgs e)
    {
        IManageUser user = Provider.Current;
        if (user != null)
        {
            if (Request["act"] == "logout")
                Provider.Current = null;
            else
                Response.Redirect("Default.aspx");
        }

        if (Request["login"] == "true") Login();
    }

    void Login()
    {
        try
        {
            String user = Request["user"];
            String pass = Request["pass"];

            Provider.Login(user, pass);
            if (Provider.Current != null)
            {
                // 处理记住密码
                HttpCookie cookie = Response.Cookies["Admin"];
                if (WebHelper.RequestBool("remember"))
                    cookie.Expires = DateTime.Now.AddDays(30);
                else
                    cookie.Expires = DateTime.MinValue;

                Response.Redirect("Default.aspx");
            }
            else
            {
                XTrace.WriteLine("{0}登录失败，但是没有异常，很是奇怪！", user);

                String msg = "登录失败";
                WebHelper.Alert(msg);
            }
        }
        catch (ThreadAbortException) { }
        catch (Exception ex)
        {
            String msg = "登录失败";
            if (ex is EntityException)
                msg += "," + ex.Message;
            else
                XTrace.WriteException(ex);
            WebHelper.Alert(msg);
        }
    }
}