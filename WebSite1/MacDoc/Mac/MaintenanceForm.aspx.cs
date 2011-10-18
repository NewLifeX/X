using System;
using NewLife.CommonEntity;
using NewLife.Web;
using NewLife.YWS.Entities;


public partial class Pages_MaintenanceForm : PageBase
{
    /// <summary>编号</summary>
    public Int32 EntityID { get { return WebHelper.RequestInt("ID"); } }

    private Maintenance _Entity;
    /// <summary>维修保养记录</summary>
    public Maintenance Entity
    {
        get { return _Entity ?? (_Entity = Maintenance.FindByKeyForEdit(EntityID)); }
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
        Int32 cid = 0;
        if (!Int32.TryParse(customerList.Value, out cid) || cid <= 0)
        {
            WebHelper.Alert("请选择客户");
            return;
        }
        Int32 mid = 0;
        if (!Int32.TryParse(ChooseButton1.Value, out mid) || mid <= 0)
        {
            WebHelper.Alert("请选择机器！");
            return;
        }

        Entity.CustomerID = cid;
        Entity.MachineID = mid;
        Entity.Propose = frmPropose.Text.Trim();
        Entity.AddTime = DateTime.Now;
        Entity.Reason = frmReason.Text;
        Entity.Technician = frmTechnician.Text.Trim();
        Entity.Remark = frmRemark.Text.Trim();
        

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