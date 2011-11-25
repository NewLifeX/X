using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Log;
using NewLife.Web;
using <#=Config.NameSpace#>;

public partial class <#=Config.EntityConnName+"_"+Table.Alias#>Form : MyEntityForm<<#=Table.Alias#>>
{
    protected void Page_Load(object sender, EventArgs e)
    {
    }
}