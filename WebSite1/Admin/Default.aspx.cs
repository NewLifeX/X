using System;
using System.Collections.Generic;
using NewLife.CommonEntity;
using NewLife.Reflection;
using XCode;

public partial class Center_Default : System.Web.UI.Page
{
    protected override void OnPreLoad(EventArgs e)
    {
        PageBase.CheckStarting();

        base.OnPreLoad(e);

        IManageUser user = CommonManageProvider.Provider.Current;
        if (user == null) Response.Redirect("Login.aspx");

        IAdministrator admin = user as IAdministrator;
        if (admin == null)
        {
            IMenu root = CommonManageProvider.Provider.MenuRoot;
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
            Int32 rootid = (PropertyInfoX.Create(CommonManageProvider.Provider.MenuType, "Root").GetValue() as IMenu).ID;
            List<IMenu> list2 = admin.Role.GetMySubMenus(rootid);
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
        //ClientScript.RegisterClientScriptResource(this.GetType(), "XControl.Box.Box.js");
    }
}