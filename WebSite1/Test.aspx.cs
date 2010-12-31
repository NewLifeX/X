using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using XCode.DataAccessLayer;
using System.Data.OleDb;
using System.Data;

public partial class Test : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Administrator admin = Administrator.FindByName("admin");
        DataSet ds = Administrator.Meta.Query("select * from Statistics");
    }
}