using System;
using System.Collections.Generic;
using NewLife.CommonEntity;
using NewLife.Reflection;
using XCode;

public partial class Center_Default : System.Web.UI.Page
{
    IAdministrator Current { get { return CommonManageProvider.Provider.Current; } }

    protected override void OnPreLoad(EventArgs e)
    {
        PageBase.CheckStarting();

        base.OnPreLoad(e);

        if (Current == null) Response.Redirect("Login.aspx");

        if (Request["act"] == "logout")
        {
            Current.Logout();
            // 再跳一次，除去Url中的尾巴
            if (!String.IsNullOrEmpty(Request.Url.Query)) Response.Redirect("Default.aspx");
        }

        // 一些初始化工作
        IEntityOperate op = EntityFactory.CreateOperate(CommonManageProvider.Provider.MenuType);
        //Menu entity = Menu.FindByName("MacDoc");
        IMenu entity = op.Find("Name", "MacDoc") as IMenu;
        if (entity != null)
        {
            entity.Name = "机器档案子系统";
            entity.Permission = entity.Name;
            (entity as IEntity).Save();
        }

        // 把Select开头的菜单项都删除了吧
        IEnumerable<IEntity> list = op.Cache.Entities.FindAll(delegate(IEntity elm)
         {
             IMenu item = elm as IMenu;
             if (!String.IsNullOrEmpty(item.Name) && item.Name.StartsWith("Select", StringComparison.OrdinalIgnoreCase)) return true;
             if (!String.IsNullOrEmpty(item.Remark) && item.Remark.StartsWith("Select", StringComparison.OrdinalIgnoreCase)) return true;
             return false;
         });
        //EntityList<Menu> list = Menu.Meta.Cache.Entities.FindAll(delegate(Menu item)
        //{
        //    if (!String.IsNullOrEmpty(item.Name) && item.Name.StartsWith("Select", StringComparison.OrdinalIgnoreCase)) return true;
        //    if (!String.IsNullOrEmpty(item.Remark) && item.Remark.StartsWith("Select", StringComparison.OrdinalIgnoreCase)) return true;
        //    return false;
        //});
        //if (list != null && list.Count > 0) list.Delete();

        if (Current.Role != null)
        {
            //Int32 rootid = Menu.Root.ID;
            Int32 rootid = (PropertyInfoX.Create(CommonManageProvider.Provider.MenuType, "Root").GetValue() as IMenu).ID;
            List<IMenu> list2 = Current.Role.GetMySubMenus(rootid);
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