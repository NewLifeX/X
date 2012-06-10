using System;
using NewLife.CommonEntity;
using NewLife.CommonEntity.Exceptions;
using NewLife.Log;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using NewLife.Security;

public partial class Login : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        this.Title = SysSetting.DisplayName;

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
            else
            {
                // 单一用户自动填写密码
                IEntityOperate eop = EntityFactory.CreateOperate(ManageProvider.Provider.ManageUserType);
                if (eop.Count == 1)
                {
                    user = eop.FindAll(null, null, null, 0, 1)[0] as IManageUser;
                    if (user != null)
                    {
                        // 使用admin或者用户名做密码，认为是默认密码，默认填写
                        if (user.Password == DataHelper.Hash("admin"))
                        {
                            UserName.Text = user.Account;
                            //Password.Text = "admin";
                            SetPass("admin");
                        }
                        else if (user.Password == DataHelper.Hash(user.Account))
                        {
                            UserName.Text = user.Account;
                            //Password.Text = user.Account;
                            SetPass(user.Account);
                        }
                    }
                }
            }
        }
    }

    void SetPass(String pass)
    {
        String js = String.Format("document.getElementById('{0}').value='{1}';", Password.ClientID, pass.Replace("'", "\\'"));
        ClientScript.RegisterStartupScript(this.GetType(), "SetPass", js, true);
    }

    protected void LoginButton_Click(object sender, EventArgs e)
    {
        try
        {
            ManageProvider.Provider.Login(UserName.Text, Password.Text);
            if (ManageProvider.Provider.Current != null)
                Response.Redirect("Default.aspx");
            else
            {
                XTrace.WriteLine("{0}登录失败，但是没有异常，很是奇怪！", UserName.Text);

                String msg = "登录失败";
                WebHelper.Alert(msg);
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