using System;
using System.Collections.Generic;
using NewLife.CommonEntity;
using NewLife.Reflection;
using XCode;

public partial class Center_Default : System.Web.UI.Page
{
    protected override void OnPreLoad(EventArgs e)
    {
        //PageBase.CheckStarting();

        base.OnPreLoad(e);

        IManageUser user = ManageProvider.Provider.Current;
        if (user == null) Response.Redirect("Login.aspx");

        ICommonManageProvider provider = CommonManageProvider.Provider;
        IMenu root = null;
        if (provider != null) root = provider.MenuRoot;

        IAdministrator admin = user as IAdministrator;
        if (admin == null)
        {
            if (root != null)
            {
                menuItem.DataSource = root.Childs;
                menuItem.DataBind();
            }
            return;
        }

        if (Request["act"] == "logout")
        {
            admin.Logout();
            // 再跳一次，除去Url中的尾巴
            if (!String.IsNullOrEmpty(Request.Url.Query)) Response.Redirect("Default.aspx");
        }

        if (admin.Role != null)
        {
            List<IMenu> list2 = admin.Role.GetMySubMenus(root.ID);
            menuItem.DataSource = list2;
            menuItem.DataBind();

            if (list2 != null && list2.Count > 0)
            {
                String js = "document.getElementById('leftiframe').src='Frame/Left.aspx?ID={0}';";
                js += "document.getElementById('main').src='{1}';";
                js = String.Format(js, list2[0].ID, list2[0].Url);
                ClientScript.RegisterStartupScript(this.GetType(), "location", js, true);
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            IMenu root = CommonManageProvider.Provider.MenuRoot;
            if (root != null)
            {
                root.CheckMenuName("Admin", "管理平台")
                    .CheckMenuName(@"Admin\Sys", "系统管理")
                    .CheckMenuName(@"Admin\Advance", "高级设置");
            }
        }
    }
}