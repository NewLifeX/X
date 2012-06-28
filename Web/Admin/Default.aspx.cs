using System;
using System.Collections.Generic;
using NewLife.CommonEntity;
using NewLife.Reflection;
using XCode;

public partial class Center_Default : System.Web.UI.Page
{
    private String _DefaultLeft = "Frame/Left.aspx";
    /// <summary>默认左菜单</summary>
    public String DefaultLeft { get { return _DefaultLeft; } set { _DefaultLeft = value; } }

    private String _DefaultMain = "Main.aspx";
    /// <summary>默认内容页</summary>
    public String DefaultMain { get { return _DefaultMain; } set { _DefaultMain = value; } }

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
            List<IMenu> list = admin.Role.GetMySubMenus(root.ID);
            menuItem.DataSource = list;
            menuItem.DataBind();

            if (list != null && list.Count > 0)
            {
                IMenu first = list[0];
                DefaultLeft = String.Format("Frame/Left.aspx?ID={0}", first.ID);
                DefaultMain = first.Url;
                //String js = "document.getElementById('leftiframe').src='Frame/Left.aspx?ID={0}';";
                //js += "document.getElementById('main').src='{1}';";
                //js = String.Format(js, list[0].ID, list[0].Url);
                //ClientScript.RegisterStartupScript(this.GetType(), "location", js, true);
            }
        }

        //IMenu root = CommonManageProvider.Provider.MenuRoot;
        if (root != null)
        {
            root.CheckMenuName("Admin", "管理平台")
                .CheckMenuName(@"Admin\Sys", "系统管理")
                .CheckMenuName(@"Admin\Advance", "高级设置")
                .CheckMenuName(@"Admin\Help", "帮助手册");
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Title = SysSetting.DisplayName;

        if (!IsPostBack)
        {
        }
    }
}