using System;
using NewLife.CommonEntity;
using NewLife.CommonEntity.Web;

public partial class EntityForm2Test : System.Web.UI.Page
{
    EntityForm2 EntityForm;
    protected override void OnPreInit(EventArgs e)
    {
        EntityForm = new EntityForm2(this, typeof(Administrator));
        EntityForm.CanSave = true;
        base.OnPreInit(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}