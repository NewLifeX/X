using System;
using System.Web.UI;
using NewLife;
using NewLife.CommonEntity;
using NewLife.Security;
using XCode;

public partial class Pages_AdminInfo : MyEntityForm
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return CommonManageProvider.Provider.AdminstratorType; } set { base.EntityType = value; } }

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        EntityForm.OnSetForm += new EventHandler<EventArgs<IEntity>>(EntityForm_OnSetForm);
        EntityForm.OnGetForm += new EventHandler<EventArgs<IEntity>>(EntityForm_OnGetForm);
    }

    void EntityForm_OnSetForm(object sender, EventArgs<IEntity> e)
    {
        frmPassword.Text = null;
    }

    void EntityForm_OnGetForm(object sender, EventArgs<IEntity> e)
    {
        if (!String.IsNullOrEmpty(frmPassword.Text)) EntityForm.Entity.SetItem("Password", DataHelper.Hash(frmPassword.Text));
    }

    //protected override void OnInitComplete(EventArgs e)
    //{
    //    base.OnInitComplete(e);

    //    EntityForm.Accessor.OnReadItem += new EventHandler<EntityAccessorEventArgs>(Accessor_OnRead);
    //    EntityForm.Accessor.OnWriteItem += new EventHandler<EntityAccessorEventArgs>(Accessor_OnWrite);
    //}

    //void Accessor_OnRead(object sender, EntityAccessorEventArgs e)
    //{
    //    if (e.Field.Name == "Password" && !String.IsNullOrEmpty(frmPassword.Text)) EntityForm.Entity.SetItem("Password", DataHelper.Hash(frmPassword.Text));
    //}

    //void Accessor_OnWrite(object sender, EntityAccessorEventArgs e)
    //{
    //    if (e.Field.Name == "Password") frmPassword.Text = null;
    //}

    protected void Page_Load(object sender, EventArgs e)
    {
        if ("" + EntityForm.EntityID != "" + Manager.Current.ID)
        {
            Response.Redirect("AdminInfo.aspx?ID=" + Manager.Current.ID);
        }

        if (!Page.IsPostBack)
        {
            btnSave.Visible = true;
            btnSave.Text = "保存";
        }
    }
}