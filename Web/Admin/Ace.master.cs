using System;
using System.Web.UI.HtmlControls;
using NewLife.CommonEntity;
using NewLife.Reflection;

public partial class ManagerPage : System.Web.UI.MasterPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        //IManagePage manager = Reflect.GetValue(Page, "Manager", false) as IManagePage;
        //if (manager != null) Navigation.Text = manager.Navigation;
    }
}