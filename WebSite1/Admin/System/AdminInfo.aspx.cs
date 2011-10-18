using System;
using System.Web.UI;
using NewLife.CommonEntity;
using NewLife.Security;
using NewLife.Web;
using NewLife.YWS.Entities;
using NewLife.CommonEntity.Web;

public partial class Pages_AdminInfo : EntityForm<Int32, Admin>
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (EntityID != Current.ID)
        {
            Response.Redirect("AdminInfo?ID=" + Current.ID);
        }

        if (!Page.IsPostBack)
        {
            btnSave.Visible = true;
            btnSave.Text = "保存";
        }
    }

    public override bool CheckPermission()
    {
        return true;
    }

    protected override void SetForm()
    {
        base.SetForm();

        btnSave.Visible = true;
        btnSave.Text = "保存";

        frmPassword.Text = null;
    }

    protected override void SetFormItem(XCode.Configuration.FieldItem field, Control control, bool canSave)
    {
        base.SetFormItem(field, control, true);
    }

    protected override void GetForm()
    {
        base.GetForm();

        if (!String.IsNullOrEmpty(frmPassword.Text))
        {
            Entity.Password = DataHelper.Hash(frmPassword.Text);
        }
    }
}