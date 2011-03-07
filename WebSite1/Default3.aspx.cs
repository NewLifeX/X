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
        DataTable dt = DAL.Create("Firebird").Session.GetSchema("ReservedWords", null);
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
}