using System;
using NewLife.YWS.Entities;
using NewLife.Web;

public partial class Login : System.Web.UI.Page
{
    protected override void OnPreLoad(EventArgs e)
    {
        if (String.Equals("logout", Request["action"]))
        {
            Admin.Current.Logout();
        }
        base.OnPreLoad(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            if (Admin.Current != null)
            {
                if (String.Equals("logout", Request["action"]))
                {
                    Admin.Current.Logout();
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
            Admin.Login(UserName.Text, Password.Text);
            if (Admin.Current != null)
            {
                Response.Redirect("Default.aspx");
            }
        }
        catch (Exception ex)
        {
            WebHelper.Alert("登录失败," + ex.Message);
        }
    }
}