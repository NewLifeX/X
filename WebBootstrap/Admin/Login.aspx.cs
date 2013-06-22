using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Threading;
using XCode;
using NewLife.Web;
using System.Threading;
using NewLife.CommonEntity.Exceptions;
using NewLife.Log;

public partial class Admin_Login : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // 引发反向工程
            ThreadPoolX.QueueUserWorkItem(delegate() { EntityFactory.CreateOperate(ManageProvider.Provider.ManageUserType).FindCount(); });

            IManageUser user = ManageProvider.Provider.Current;
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
        }
    }

    protected void LoginBt_Click(object sender, EventArgs e)
    {
        try
        {
            String account = AcccountName.Text;
            ManageProvider.Provider.Login(account, Password.Text);

            //if (ManageProvider.Provider.Current != null)
            //{
            //    //检查当前登录的帐号是否绑定了指定的帐号，如果没有绑定指定的帐号即退出
            //    if (!ManageProvider.Provider.Current.IsAdmin)
            //    {
            //        Administrator a = ManageProvider.Provider.Current as Administrator;
            //        //if (!a.CheckAccount()) WebHelper.AlertAndRedirect("该帐号没有绑定用户信息！", "Default.aspx");
            //    }
            //    Response.Redirect("Default.aspx");
            //}

            Response.Redirect("Default.aspx");
        }
        catch (ThreadAbortException) { }
        catch (Exception ex)
        {
            String msg = "帐号或密码错误";
            if (ex is EntityException)
                msg += "," + ex.Message;
            else
                XTrace.WriteException(ex);
            errorMessage.Text = msg;
        }
    }
}