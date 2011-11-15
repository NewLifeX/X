using System;
using NewLife.CommonEntity;
using NewLife.Web;
using NewLife.YWS.Entities;


public partial class Pages_FeedliquorForm : PageBase
{
    /// <summary>编号</summary>
    public Int32 EntityID { get { return WebHelper.RequestInt("ID"); } }

    private Feedliquor _Entity;
    /// <summary>液料规格</summary>
    public Feedliquor Entity
    {
        get { return _Entity ?? (_Entity = Feedliquor.FindByKeyForEdit(EntityID)); }
        set { _Entity = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            DataBind();

            // 添加/编辑 按钮需要添加/编辑权限
            if (EntityID > 0)
                UpdateButton.Visible = Acquire(PermissionFlags.Update);
            else
                UpdateButton.Visible = Acquire(PermissionFlags.Insert);
        }       
    }

    protected void UpdateButton_Click(object sender, EventArgs e)
    {
        //if (!WebHelper.CheckEmptyAndFocus(frmSort, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmRemark, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmPermission, null)) return;

        //if (String.IsNullOrEmpty(customerList.SelectedValue))
        //{
        //    WebHelper.Alert("请选择客户");
        //    return;
        //}
        if (!WebHelper.CheckEmptyAndFocus(frmManufacturer, null)) return;
        if (!WebHelper.CheckEmptyAndFocus(frmCementGroup, null)) return;
        if (!WebHelper.CheckEmptyAndFocus(frmProductNo, null)) return;

        //Entity.CustomerID = Convert.ToInt32(customerList.SelectedItem.Value);
        Entity.Address = frmAddress.Text.Trim();
        Entity.AddTime = DateTime.Now;
        Entity.CementGroup = frmCementGroup.Text.Trim();
        Entity.CementGroupB = frmCementGroupB.Text.Trim();
        //Entity.FillersAmount = frmFillersAmount.Text.Trim();
        //Entity.FillersType = frmFillersType.Text.Trim();
        Entity.Hardening = frmHardening.Text.Trim();
        Entity.IsAbradability = frmIsAbradability.Checked;
        Entity.IsAgitation = frmIsAgitation.Checked;
        Entity.IsCorrosivity = frmIsCorrosivity.Checked;
        Entity.IsExcept = frmIsExcept.Checked;
        Entity.IsFillers = frmIsFillers.Checked;
        Entity.IsSensitivity = frmIsSensitivity.Checked;
        Entity.IsSolventName = frmIsSolventName.Checked;
        Entity.Manufacturer = frmManufacturer.Text.Trim();
        Entity.MixViscosity = frmMixViscosity.Text.Trim();
        Entity.ProductNo = frmProductNo.Text.Trim();

        if (frmIsFillers.Checked)
        {
            Entity.FillersAmount = frmFillersAmount.Value;
            Entity.FillersType = frmFillersType.Text.Trim();
        }
        if (frmIsFillersB.Checked)
        {
            Entity.FillersAmountB = frmFillersAmountB.Value;
            Entity.FillersTypeB = frmFillersTypeB.Text.Trim();
        }


        //Entity.HardeningB = frmHardeningB.Text.Trim();
        Entity.IsAbradabilityB = frmIsAbradabilityB.Checked;
        Entity.IsAgitationB = frmIsAgitationB.Checked;
        Entity.IsCorrosivityB = frmIsCorrosivityB.Checked;
        Entity.IsExceptB = frmIsExceptB.Checked;
        Entity.IsFillersB = frmIsFillersB.Checked;
        Entity.IsSensitivityB = frmIsSensitivityB.Checked;
        Entity.IsSolventNameB = frmIsSolventNameB.Checked;
        //Entity.MixViscosityB = frmMixViscosityB.Text.Trim();
        Entity.ProductNoB = frmProductNoB.Text.Trim();
        Entity.SpecificGravityB = frmSpecificGravityB.Text.Trim();
        Entity.TemperatureB = frmTemperatureB.Text.Trim();
        Entity.ViscosityB = frmViscosityB.Text.Trim();
        Entity.VolumeRatioB = frmVolumeRatioB.Value;
        Entity.WeightRatioB = frmWeightRatioB.Value;
        //Entity.WorkingHoursB = Convert.ToInt32(frmWorkingHoursB.Text);
        Entity.WViscosityB = frmWViscosityB.Text;



        Entity.Remark = frmRemark.Text.Trim();
        Entity.SpecificGravity = frmSpecificGravity.Text.Trim();
        Entity.Tel = frmTel.Text.Trim();
        Entity.Temperature = frmTemperature.Text.Trim();
        Entity.Viscosity = frmViscosity.Text.Trim();
        Entity.VolumeRatio = frmVolumeRatio.Value;
        Entity.WeightRatio = frmWeightRatio.Value;
        Entity.WorkingHours = frmWorkingHours.Value;
        Entity.WViscosity = frmWViscosity.Text;

        try
        {
            Entity.Save();
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('成功！');parent.Dialog.CloseAndRefresh(frameElement);", true);
        }
        catch (Exception ex)
        {
            WebHelper.Alert("失败！" + ex.Message);
        }
    }
}