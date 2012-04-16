using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Default5 : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }
    protected string RandomColor()
    {
        Random r = new Random();
        return string.Format("#{0:x2}{1:x2}{2:x2}", r.Next(0x80 + 1) + 100, r.Next(0x80 + 1) + 100, r.Next(0x80 + 1) + 100);
    }
}