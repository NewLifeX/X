using System;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Web;
using Menu = NewLife.CommonEntity.Menu;

public partial class Center_Frame_Left : System.Web.UI.Page
{
    IAdministrator Current { get { return CommonManageProvider.Provider.Current; } }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            if (Current != null && Current.Role != null)
            {
                //Menu m = Menu.FindByName("管理平台");
                Menu m = null;

                Int32 id = WebHelper.RequestInt("ID");
                if (id > 0) m = Menu.FindByID(id);

                if (m == null)
                {
                    m = Menu.Root;
                    if (m == null || m.Childs == null || m.Childs.Count < 1) return;
                    m = m.Childs[0];
                    if (m == null) return;
                }

                Literal1.Text = m.Name;

                menu.DataSource = Current.Role.GetMySubMenus(m.ID);
                menu.DataBind();
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

        rp.DataSource = Current.Role.GetMySubMenus(m.ID);
        rp.DataBind();
    }
}