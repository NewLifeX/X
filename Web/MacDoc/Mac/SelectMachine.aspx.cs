using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


public partial class Admin_Center_SelectMachine : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack) lblmsg.Text = Request["Name"];
    }

    public override bool CheckPermission()
    {
        return true;
    }
}