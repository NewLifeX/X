using System;
using System.Threading;
using NewLife.Common;
using NewLife.CommonEntity;
using NewLife.CommonEntity.Exceptions;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XControl;

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
            if (String.Equals("logout", Request["action"], StringComparison.OrdinalIgnoreCase))
            {
                IAdministrator admin = user as IAdministrator;
                if (admin == null) admin.Logout();
            }
            else
                Response.Redirect("Default.aspx");
        }

        if (WebHelper.RequestBool("login")) Login();
    }

    void SetPass(String pass)
    {
        String js = String.Format("$('user').value='{0}';", pass.Replace("'", "\\'"));
        ClientScript.RegisterStartupScript(this.GetType(), "SetPass", js, true);
    }

    void Login()
    {
        try
        {
            String user = Request["user"];
            String pass = Request["pass"];

            Provider.Login(user, pass);
            if (Provider.Current != null)
                Response.Redirect("Default.aspx");
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