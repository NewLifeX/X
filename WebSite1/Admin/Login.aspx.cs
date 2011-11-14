using System;
using NewLife.CommonEntity;
using NewLife.CommonEntity.Exceptions;
using NewLife.Log;
using NewLife.Threading;
using NewLife.Web;
using XCode;

public partial class Login : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // 引发反向工程
            ThreadPoolX.QueueUserWorkItem(delegate() { EntityFactory.CreateOperate(CommonManageProvider.Provider.AdminstratorType).FindCount(); });

            IManageUser user = CommonManageProvider.Provider.Current;
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

    protected void LoginButton_Click(object sender, EventArgs e)
    {
        try
        {
            CommonManageProvider.Provider.Login(UserName.Text, Password.Text);
            if (CommonManageProvider.Provider.Current != null) Response.Redirect("Default.aspx");
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