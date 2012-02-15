using System;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Web;

public partial class Center_Frame_Left : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            ICommonManageProvider provider = CommonManageProvider.Provider;

            Int32 id = WebHelper.RequestInt("ID");

            IMenu m = provider.FindByMenuID(id);
            if (m != null) Literal1.Text = m.Name;

            menu.DataSource = provider.GetMySubMenus(id);
            menu.DataBind();
        }
    }

    protected void menu_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e.Item == null || e.Item.DataItem == null) return;
        IMenu m = e.Item.DataItem as IMenu;
        if (m == null) return;

        Repeater rp = e.Item.FindControl("menuItem") as Repeater;
        if (rp == null) return;

        rp.DataSource = CommonManageProvider.Provider.GetMySubMenus(m.ID);
        rp.DataBind();
    }
}