using System;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Web;
using NewLife.Reflection;
using System.Collections.Generic;
//using Menu = NewLife.CommonEntity.Menu;

public partial class Center_Frame_Left : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            IMenu m = null;

            Int32 id = WebHelper.RequestInt("ID");
            if (id > 0)
            {
                //m = Menu.FindByID(id);
                m = MethodInfoX.Create(CommonManageProvider.Provider.MenuType, "FindByID").Invoke(null, id) as IMenu;
            }

            if (m == null)
            {
                m = CommonManageProvider.Provider.MenuRoot;
                if (m == null || m.Childs == null || m.Childs.Count < 1) return;
                m = m.Childs[0];
                if (m == null) return;
            }

            Literal1.Text = m.Name;

            if (CommonManageProvider.Provider.Current != null)
            {

                IAdministrator admin = CommonManageProvider.Provider.Current as IAdministrator;
                if (admin != null)
                {
                    if (admin.Role != null)
                    {
                        menu.DataSource = admin.Role.GetMySubMenus(m.ID);
                        menu.DataBind();
                    }
                }
                else
                {
                    menu.DataSource = m.Childs;
                    menu.DataBind();
                }
            }
        }
    }

    protected void menu_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e.Item == null || e.Item.DataItem == null) return;
        IMenu m = e.Item.DataItem as IMenu;
        if (m == null) return;

        Repeater rp = e.Item.FindControl("menuItem") as Repeater;
        if (rp == null) return;

        IList<IMenu> ms = null;

        IAdministrator admin = CommonManageProvider.Provider.Current as IAdministrator;
        if (admin != null)
        {
            if (admin.Role != null) ms = admin.Role.GetMySubMenus(m.ID);
        }
        else
            ms = m.Childs;

        rp.DataSource = ms;
        rp.DataBind();
    }
}