using System;
using NewLife.Reflection;
using System.Web.UI.HtmlControls;
using NewLife.CommonEntity;

public partial class ManagerPage : System.Web.UI.MasterPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        FieldInfoX fix = FieldInfoX.Create(Page.GetType(), "Manager");
        if (fix != null)
        {
            IManagePage manager = fix.GetValue(Page) as IManagePage;
            if (manager != null) Navigation.Text = manager.Navigation;
        }

        Page.ClientScript.RegisterClientScriptInclude("jquery", ResolveUrl("~/Scripts/jquery-1.4.1.min.js"));
        Page.ClientScript.RegisterClientScriptInclude("adminstyle", ResolveUrl("~/Scripts/adminstyle.js"));

        HtmlLink link = new HtmlLink();
        link.Href = ResolveUrl("~/Admin/images/css.css");
        link.Attributes["rel"] = "stylesheet";
        link.Attributes["type"] = "text/css";
        Page.Header.Controls.Add(link);
    }
}