using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Pages_<#=ClassName#>Form : System.Web.UI.Page
{
    /// <summary>编号</summary>
    public Int32 EntityID { get { return WebHelper.RequestInt("ID"); } }

    private <#=ClassName#> _Entity;
    /// <summary><#=ClassDescription#></summary>
    public <#=ClassName#> Entity
    {
        get { return _Entity ?? (_Entity = <#=ClassName#>.FindByKeyForEdit(EntityID)); }
        set { _Entity = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}
