using System;
using NewLife.CommonEntity;

public partial class MenuForm : MyEntityForm<NewLife.CommonEntity.Menu>
{
    protected void Page_Load(object sender, EventArgs e)
    {
        MasterPage.SetToolBar(false);

        EntityForm.OnSaveSuccess += new EventHandler<EntityFormEventArgs>(EntityForm_OnSaveSuccess);
    }

    void EntityForm_OnSaveSuccess(object sender, EntityFormEventArgs e)
    {
        e.Cancel = true;

        Response.Redirect("Menu.aspx"); 
    }
}