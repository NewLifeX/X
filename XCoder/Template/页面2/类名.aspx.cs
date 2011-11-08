using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using <#=Config.NameSpace#>;

public partial class <#=Config.EntityConnName+"_"+Table.Alias#> : MyEntityList
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return typeof(<#=Table.Alias#>); } set { base.EntityType = value; } }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}