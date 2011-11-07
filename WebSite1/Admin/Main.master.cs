using System;
using NewLife.Reflection;
using System.Web.UI.HtmlControls;

public partial class Master_Main : System.Web.UI.MasterPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        PropertyInfoX pix = PropertyInfoX.Create(Page.GetType(), "Navigation");
        if (pix != null) Navigation.Text = (String)pix.GetValue(Page);

        Page.ClientScript.RegisterClientScriptInclude("jquery", ResolveUrl("~/Scripts/jquery-1.4.1.min.js"));
        Page.ClientScript.RegisterClientScriptInclude("adminstyle", ResolveUrl("~/Scripts/adminstyle.js"));

        HtmlLink link = new HtmlLink();
        link.Href = ResolveUrl("~/Admin/images/css.css");
        link.Attributes["rel"] = "stylesheet";
        link.Attributes["type"] = "text/css";
        Page.Header.Controls.Add(link);
    }
}