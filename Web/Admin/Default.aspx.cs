using System;
using System.Collections.Generic;
using NewLife.Common;
using NewLife.CommonEntity;

public partial class Admin_Default : System.Web.UI.Page
{
    private String _DefaultLeft = "Frame/Left.aspx";
    /// <summary>默认左菜单</summary>
    public String DefaultLeft { get { return _DefaultLeft; } set { _DefaultLeft = value; } }

    private String _DefaultMain = "Main.aspx";
    /// <summary>默认内容页</summary>
    public String DefaultMain { get { return _DefaultMain; } set { _DefaultMain = value; } }

    /// <summary>系统配置。如果重载，修改这里即可。</summary>
    public static SysConfig Config { get { return SysConfig.Current; } }

    protected override void OnPreLoad(EventArgs e)
    {
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

        #region 自动修正菜单
        // 自动修正菜单中英文
        if (root != null)
        {
            root.CheckMenuName("Admin", "管理平台")
                .CheckMenuName(@"Admin\Sys", "系统管理")
                .CheckMenuName(@"Admin\Advance", "高级设置")
                .CheckMenuName(@"Admin\Help", "帮助手册");

            // 自动挂载Main.aspx
            IMenu menu = root.FindByPath("Admin");
            if (menu != null && menu.Url == "../Admin/Default.aspx")
            {
                menu.Url = "../Admin/Main.aspx";
                menu.Save();
            }
            if (menu != null)
            {
                #region 自动排序
                IMenu menu2 = menu.FindByPath("Sys");
                if (menu2 != null)
                {
                    menu2.Sort = 3;
                    menu2.Save();
                }
                menu2 = menu.FindByPath("Advance");
                if (menu2 != null)
                {
                    menu2.Sort = 2;
                    menu2.Save();
                }
                menu2 = menu.FindByPath("Help");
                if (menu2 != null)
                {
                    menu2.Sort = 1;
                    menu2.Save();
                }
                #endregion
            }
        }
        #endregion
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        this.Title = Config.DisplayName;
    }
}