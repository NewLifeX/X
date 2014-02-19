using System;
using NewLife.CommonEntity;
using NewLife.Log;

public partial class Test : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Write(Request.Url.ToString());
        Response.Write(Request.RawUrl);
    }
}