using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.CommonEntity.Web;
using NewLife.Web;
using XControl;
using System.Xml.Serialization;
using <#=Config.NameSpace#>;

public partial class Pages_<#=Table.Alias#>Form : System.Web.UI.Page
{
    private EntityForm2 EntityForm;

    protected override void OnPreInit(EventArgs e)
    {
        EntityForm = new EntityForm2(this,typeof(Admin));
        //EntityForm.CanSave = true;
        base.OnPreInit(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        
    }
}