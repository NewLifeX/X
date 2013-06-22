using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using <#=Config.NameSpace#>;

public partial class <#=Config.EntityConnName+"_"+Table.Name#> : MyEntityList<<#=Table.Name#>>
{
    protected void Page_Load(object sender, EventArgs e)
    {
    }
}