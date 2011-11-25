using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Log;
using NewLife.Web;
using <#=Config.NameSpace#>;

public partial class <#=Config.EntityConnName+"_"+Table.Alias#>Form : MyEntityForm
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return typeof(<#=Table.Alias#>); } set { base.EntityType = value; } }

    /// <summary>实体</summary>
    public <#=Table.Alias#> Entity { get { return EntityForm.Entity as <#=Table.Alias#>; } }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}