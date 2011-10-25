using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity.Web;
using NewLife.YWS.Entities;

public partial class EntityForm2Test : System.Web.UI.Page
{
    EntityForm2 EntityForm;
    protected override void OnPreInit(EventArgs e)
    {
        EntityForm = new EntityForm2(this, typeof(Admin));
        EntityForm.CanSave = true;
        base.OnPreInit(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}