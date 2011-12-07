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

        EntityForm.OnSetForm += EntityForm_OnSetForm;
        EntityForm.OnGetForm += EntityForm_OnGetForm;
    }

    void EntityForm_OnSetForm(object sender, EntityFormEventArgs e)
    {
        frmPassword.Text = null;
    }

    void EntityForm_OnGetForm(object sender, EntityFormEventArgs e)
    {
        if (!String.IsNullOrEmpty(frmPassword.Text)) EntityForm.Entity.SetItem("Password", DataHelper.Hash(frmPassword.Text));
    }

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