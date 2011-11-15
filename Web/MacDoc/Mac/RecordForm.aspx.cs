using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Web;
using NewLife.YWS.Entities;
using NewLife.CommonEntity;


public partial class Admin_Center_RecordForm : PageBase
{
    /// <summary>编号</summary>
    public Int32 EntityID { get { return WebHelper.RequestInt("ID"); } set { EntityID = value; } }

    private Record _Entity;
    /// <summary>机器零件规格</summary>
    public Record Entity
    {
        get { return _Entity ?? (_Entity = Record.FindByKeyForEdit(EntityID)); }
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
        Int32 mid = 0;
        if (!Int32.TryParse(ChooseButton1.Value, out mid) || mid <= 0)
        {
            WebHelper.Alert("请选择机器！");
            return;
        }

        if (frmLeaveTime.Value == DateTime.MinValue)
        {
            WebHelper.Alert("请出厂日期！");
            return;
        }

        if (!WebHelper.CheckEmptyAndFocus(frmTransactor, null)) return;
        if (!WebHelper.CheckEmptyAndFocus(frmLeaveTime, null)) return;

        Entity.CustomerID = cid;
        Entity.Attachment = frmAttachment.Text.Trim();
        Entity.AddTime = DateTime.Now;
        Entity.MachineID = mid;
        Entity.LeaveTime = frmLeaveTime.Value;
        Entity.Transactor = frmTransactor.Text.Trim();
        Entity.Remark = frmRemark.Text.Trim();

        Machine m = Machine.FindByID(mid);

        if (m == null) throw new Exception("所选机器不存在！");

        Entity.DischargeSpec = m.DischargeSpec;
        //Entity.Groupings = frmGroupings.Text.Trim();
        Entity.Kind = m.Kind;
        Entity.MeteringpumpSpec = m.MeteringpumpSpec;
        Entity.Model = m.Model;
        Entity.Name = m.Name;
        Entity.OutlineSize = m.OutlineSize;
        Entity.PresSize = m.PresSize;
        Entity.Size = m.Size;
        Entity.SupplypipeSpec = m.SupplypipeSpec;

        Entity.MeteringpumpSpecB = m.MeteringpumpSpecB;
        Entity.PresSizeB = m.PresSizeB;
        Entity.SizeB = m.SizeB;
        Entity.SupplypipeSpecB = m.SupplypipeSpecB;
        Entity.DischargeSpecB = m.DischargeSpecB;

        Entity.Type = m.Type;        
        Entity.VacuumpumpSpec = m.VacuumpumpSpec;


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