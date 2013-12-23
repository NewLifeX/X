using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Log;
using NewLife.CommonEntity;
using NewLife.Web;

public partial class _Default : System.Web.UI.Page 
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Redirect("Admin/Default.aspx");
        //Js.Alert("Hi, BigStone!").Redirect("Admin/Default.aspx");
    }
}