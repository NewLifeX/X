using NewLife.Common;
using NewLife.CommonEntity;
using System;

public partial class Admin_Default : System.Web.UI.Page
{
    /// <summary>系统配置。如果重载，修改这里即可。</summary>
    public static SysConfig Config { get { return SysConfig.Current; } }

    protected override void OnPreLoad(EventArgs e)
    {
        base.OnPreLoad(e);

        IManageUser user = ManageProvider.Provider.Current;
        if (user == null) Response.Redirect("../Login.aspx");

        ICommonManageProvider provider = CommonManageProvider.Provider;
        IMenu root = null;
        if (provider != null) root = provider.MenuRoot;

        IAdministrator admin = user as IAdministrator;

        if (Request["act"] == "logout")
        {
            admin.Logout();
            if (string.IsNullOrEmpty(Request["tohome"]))
            {
                // 再跳一次，除去Url中的尾巴
                if (!String.IsNullOrEmpty(Request.Url.Query)) Response.Redirect("Default.aspx");
                return;
            }
            else
            {
                Response.Redirect("~/");
                return;
            }
        }

        if (root != null)
        {
            root.CheckMenuName("Admin", "管理平台")
                .CheckMenuName(@"Admin\Sys", "系统管理")
                .CheckMenuName(@"Admin\Advance", "高级设置");

            IMenu menu = root.FindByPath(@"Admin");
            if (menu != null && String.Equals(menu.Url, "../Admin/Default.aspx", StringComparison.OrdinalIgnoreCase))
            {
                menu.Url = "../Admin/Main.aspx";
                menu.Save();
            }
        }
    }
    protected void Page_Load(object sender, EventArgs e)
    {
    }
}