using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Log;
using NewLife.Web;
using <#=Config.NameSpace#>;

public partial class <#=Config.EntityConnName+"_"+Table.Name#>Form : MyEntityForm<<#=Table.Name#>>
{
    protected void Page_Load(object sender, EventArgs e)
    {
    }
}