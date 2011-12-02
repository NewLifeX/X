using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class foo : System.Web.UI.Page
{
    protected string specUrl;

    protected void Page_Load(object sender, EventArgs e)
    {
        Uri u = Request.Url;
        specUrl = string.Format("http://test.localhost{0}/{1}",
            u.IsDefaultPort ? "" : ":" + u.Port,
            Request.ApplicationPath.Trim('/')
        );
    }
}