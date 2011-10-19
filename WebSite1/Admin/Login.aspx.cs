using System;
using NewLife.CommonEntity;
using NewLife.CommonEntity.Exceptions;
using NewLife.Log;
using NewLife.Web;

public partial class Login : System.Web.UI.Page
{
    IAdministrator Current { get { return CommonManageProvider.Provider.Current; } }

    protected override void OnPreLoad(EventArgs e)
    {
        if (String.Equals("logout", Request["action"]))
        {
            Current.Logout();
        }
        base.OnPreLoad(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            if (Current != null)
            {
                if (String.Equals("logout", Request["action"]))
                {
                    Current.Logout();
                }
                else
                {
                    Response.Redirect("Default.aspx");
                }
            }

            //UserName.Text = "admin";
            //Password.Text = "admin";
        }
    }

    protected void LoginButton_Click(object sender, EventArgs e)
    {
        //bool flag = login.ValidateUser(UserName.Text.ToString().Trim(), Password.Text.ToString().Trim());
        try
        {
            CommonManageProvider.Provider.Login(UserName.Text, Password.Text);
            if (Current != null)
            {
                Response.Redirect("Default.aspx");
            }
        }
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