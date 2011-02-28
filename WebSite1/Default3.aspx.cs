using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Data;
using XCode.DataAccessLayer;

public partial class Default3 : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        DataTable dt = DAL.Create("MySql_System").Session.GetSchema("ReservedWords", null);
        if (dt != null && dt.Rows != null && dt.Rows.Count > 0)
        {
            foreach (DataRow dr in dt.Rows)
            {
                //keyWords.Add(dr[0].ToString().ToUpper());
                Response.Write(",");
                Response.Write(dr[0].ToString());
            }
        }
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        Label1.Text = GridViewExtender1.SelectedIndexesString;
        Label2.Text = GridViewExtender1.SelectedValuesString;
    }
}