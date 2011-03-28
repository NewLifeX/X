using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using XCode;

public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            GridView1.DataSource = EntityFactory.CreateOperate("Administrator").FindAll();
            GridView1.DataBind();
        }
    }
    protected void Button1_Click(object sender, EventArgs e)
    {
        DropDownList1.SelectedValue = "333";

        //IList<Area> list = Area.FindAll(null, null, 0, 10);
        //DropDownList1.DataSource = list;
        //DropDownList1.DataTextField = "Name";
        //DropDownList1.DataValueField = "ID";
        //DropDownList1.DataBind();

        //DropDownList1.SelectedValue = "0";
    }
}