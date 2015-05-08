using System;
using NewLife.CommonEntity;
using NewLife.Security;
using XCode.Membership;

public partial class Pages_AdminForm : MyEntityForm
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return ManageProvider.Provider.UserType; } set { base.EntityType = value; } }

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        EntityForm.OnSetForm += EntityForm_OnSetForm;
        EntityForm.OnGetForm += EntityForm_OnGetForm;
    }

    void EntityForm_OnSetForm(object sender, EntityFormEventArgs e)
    {
        frmPassword_.Text = null;
    }

    void EntityForm_OnGetForm(object sender, EntityFormEventArgs e)
    {
        if (!String.IsNullOrEmpty(frmPassword_.Text)) EntityForm.Entity.SetItem("Password", DataHelper.Hash(frmPassword_.Text));
    }

    protected override void OnInitComplete(EventArgs e)
    {
        base.OnInitComplete(e);
        ods.DataObjectTypeName = ods.TypeName = ManageProvider.Provider.GetService<IRole>().GetType().FullName;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}