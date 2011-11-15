using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;

public partial class Admin_System_WebConfig : MyEntityList
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            String file = Server.MapPath("~/web.config");
            txtLog.Text = File.ReadAllText(file);
        }
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        String file = Server.MapPath("~/web.config");
        String str = File.ReadAllText(file);
        if (txtLog.Text == str) return;

        File.WriteAllText(file, txtLog.Text);
    }
}