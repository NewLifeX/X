using System;
using NewLife.YWS.Entities;
using NewLife.Web;
using Menu = NewLife.CommonEntity.Menu;
using XCode;

public partial class Center_Default : System.Web.UI.Page
{
    protected override void OnPreLoad(EventArgs e)
    {
        PageBase.CheckStarting();

        base.OnPreLoad(e);

        if (Admin.Current == null) Response.Redirect("Login.aspx");

        if (Request["act"] == "logout")
        {
            Admin.Current.Logout();
            // 再跳一次，除去Url中的尾巴
            if (!String.IsNullOrEmpty(Request.Url.Query)) Response.Redirect("Default.aspx");
        }
        //Label_IP.Text = WebHelper.UserHost;
        //Label_FN.Text = Admin.Current.DisplayName;
        //Label_Admin.Text = Admin.Current.RoleName;

        // 一些初始化工作
        Menu entity = Menu.FindByName("MacDoc");
        if (entity != null)
        {
            entity.Name = "机器档案子系统";
            entity.Permission = entity.Name;
            entity.Save();
        }

        // 把Select开头的菜单项都删除了吧
        EntityList<Menu> list = Menu.Meta.Cache.Entities.FindAll(delegate(Menu item)
        {
            if (!String.IsNullOrEmpty(item.Name) && item.Name.StartsWith("Select", StringComparison.OrdinalIgnoreCase)) return true;
            if (!String.IsNullOrEmpty(item.Remark) && item.Remark.StartsWith("Select", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        });
        if (list != null && list.Count > 0) list.Delete();

        if (Admin.Current.Role != null)
        {
            list = Admin.Current.Role.GetMySubMenus(Menu.Root.ID);
            menuItem.DataSource = list;
            menuItem.DataBind();

            if (list != null && list.Count > 0)
            {
                String js = "document.getElementById('leftiframe').src='Frame/Left.aspx?ID={0}';";
                js += "document.getElementById('main').src='{1}';";
                js = String.Format(js, list[0].ID, list[0].Url);
                ClientScript.RegisterStartupScript(this.GetType(), "location", js, true);
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        //ClientScript.RegisterClientScriptResource(this.GetType(), "XControl.Box.Box.js");
    }
}