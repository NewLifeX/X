using System;
using NewLife.CommonEntity;
using NewLife.Web;
//using NewLife.YWS.Entities;

using NewLife.YWS.Entities;
using System.Web.UI.WebControls;

public partial class Pages_MachineForm : PageBase
{
    /// <summary>编号</summary>
    public Int32 EntityID { get { return WebHelper.RequestInt("ID"); } }

    private Machine _Entity;
    /// <summary>机器零件规格</summary>
    public Machine Entity
    {
        get { return _Entity ?? (_Entity = Machine.FindByKeyForEdit(EntityID)); }
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
        // 添加/编辑 按钮需要添加/编辑权限
        if (EntityID > 0 && !Acquire(PermissionFlags.Update))
        {
            WebHelper.Alert("没有权限！");
            return;
        }
        if (EntityID <= 0 && !Acquire(PermissionFlags.Insert))
        {
            WebHelper.Alert("没有权限！");
            return;
        }

        Int32 cid = 0;
        if (!Int32.TryParse(customerList.Value, out cid) || cid <= 0)
        {
            WebHelper.Alert("请选择客户");
            return;
        }

        Int32 fid = 0;
        if (!Int32.TryParse(ChooseButton1.Value, out fid) || fid <= 0)
        {
            WebHelper.Alert("请选择液料规格！");
            return;
        }

        if (frmLeaveTime.Value == DateTime.MinValue)
        {
            WebHelper.Alert("请出厂日期！");
            return;
        }

        if (!WebHelper.CheckEmptyAndFocus(frmName, null)) return;
        if (!WebHelper.CheckEmptyAndFocus(frmTransactor, null)) return;
        if (!WebHelper.CheckEmptyAndFocus(frmLeaveTime, null)) return;

        Entity.CustomerID = cid;
        Entity.FeedliquorID = fid;
        Entity.Attachment = frmAttachment.Text.Trim();
        Entity.AddTime = DateTime.Now;
        Entity.DischargeSpec = frmDischargeSpec.Text;
        //Entity.Groupings = frmGroupings.Text.Trim();
        Entity.Kind = frmKind.Text.Trim();
        Entity.LeaveTime = frmLeaveTime.Value;
        Entity.MeteringpumpSpec = frmMeteringpumpSpec.Text;
        Entity.Model = frmModel.Text.Trim();
        Entity.Name = frmName.Text.Trim();
        Entity.OutlineSize = frmOutlineSize.Text;
        Entity.PresSize = frmPresSize.Value;
        Entity.Size = frmSize.Value;
        Entity.SupplypipeSpec = frmSupplypipeSpec.Text.Trim();
        Entity.Transactor = frmTransactor.Text.Trim();


        Entity.MeteringpumpSpecB = frmMeteringpumpSpecB.Text;
        Entity.PresSizeB = frmPresSizeB.Value;
        Entity.SizeB = frmSizeB.Value;
        Entity.SupplypipeSpecB = frmSupplypipeSpecB.Text.Trim();
        Entity.DischargeSpecB = frmDischargeSpecB.Text.Trim();

        Entity.Type = frmType.Text.Trim();
        Entity.Remark = frmRemark.Text.Trim();
        Entity.VacuumpumpSpec = frmVacuumpumpSpec.Text.Trim();

        Record record = null;
        if (EntityID > 0) record = Record.FindByMachineID(EntityID);
        if (record == null) record = new Record();

        record.CustomerID = cid;
        record.Attachment = frmAttachment.Text.Trim();
        record.AddTime = DateTime.Now;
        record.DischargeSpec = frmDischargeSpec.Text;
        //record.Groupings = frmGroupings.Text.Trim();
        record.Kind = frmKind.Text.Trim();
        record.LeaveTime = frmLeaveTime.Value;
        record.MeteringpumpSpec = frmMeteringpumpSpec.Text;
        record.Model = frmModel.Text.Trim();
        record.Name = frmName.Text.Trim();
        record.OutlineSize = frmOutlineSize.Text;
        record.PresSize = frmPresSize.Value;
        record.Size = frmSize.Value;
        record.SupplypipeSpec = frmSupplypipeSpec.Text.Trim();
        record.Transactor = frmTransactor.Text.Trim();


        record.MeteringpumpSpecB = frmMeteringpumpSpecB.Text;
        record.PresSizeB = frmPresSizeB.Value;
        record.SizeB = frmSizeB.Value;
        record.SupplypipeSpecB = frmSupplypipeSpecB.Text.Trim();
        record.DischargeSpecB = frmDischargeSpecB.Text.Trim();

        record.Type = frmType.Text.Trim();
        record.Remark = frmRemark.Text.Trim();
        record.VacuumpumpSpec = frmVacuumpumpSpec.Text.Trim();

        try
        {
            Int32 mid = Entity.Save();
            record.MachineID = EntityID == 0 ? mid : EntityID;
            record.Save();
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('成功！');parent.Dialog.CloseAndRefresh(frameElement);", true);
        }
        catch (Exception ex)
        {
            WebHelper.Alert("失败！" + ex.Message);
        }
    }
    //protected void GridView1_SelectedIndexChanged(object sender, EventArgs e)
    //{
    //    DataKey key = GridView1.SelectedDataKey;
    //    EntityID = (Int32)key.Value;
    //    DataBind();
    //}
}