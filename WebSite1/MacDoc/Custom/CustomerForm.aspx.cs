/*
 * XCoder v3.2.2010.1014
 * 作者：SUN/SUN-PC
 * 时间：2010-12-22 20:17:01
 * 版权：版权所有 (C) 新生命开发团队 2010
*/
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using XControl;
using NewLife.YWS.Entities;
using NewLife.Web;

using NewLife.CommonEntity;

public partial class CustomerForm : PageBase
{
    /// <summary>编号</summary>
    public Int32 EntityID { get { return WebHelper.RequestInt("ID"); } }

    private Customer _Entity;
    /// <summary>客户</summary>
    public Customer Entity
    {
        get { return _Entity ?? (_Entity = Customer.FindByKeyForEdit(EntityID)); }
        set { _Entity = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            DataBind();

            typeList.Items.Add(new ListItem("|-请选择", "0"));
            //List<CustomerType> list = CustomerType.FindAll(CustomerType._.ParentID,0);
            List<CustomerType> list = CustomerType.FindAll();
            if (list != null && list.Count > 0)
            {
                foreach (CustomerType item in list)
                {
                    String spaces = new String('　', item.Deepth);
                    typeList.Items.Add(new ListItem(spaces + "|- " + item.Name, item.ID.ToString()));
                }
            }
            if (Entity != null) typeList.SelectedValue = Entity.CustomerTypeID.ToString();

            // 添加/编辑 按钮需要添加/编辑权限
            if (EntityID > 0)
                UpdateButton.Visible = Acquire(PermissionFlags.Update);
            else
                UpdateButton.Visible = Acquire(PermissionFlags.Insert);

  
        }
    }

    protected void UpdateButton_Click(object sender, EventArgs e)
    {

        if (typeList.SelectedValue.Equals("0"))
        {
            WebHelper.Alert("请选择客户类型！");
            return;
        }
        #region MyRegion
        if (!WebHelper.CheckEmptyAndFocus(frmNo, null)) return;
        if (!WebHelper.CheckEmptyAndFocus(frmName, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmLinkman, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmDepartment, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmTel, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmFax, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmEmail, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmQQ, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmMSN, null)) return;
        if (!WebHelper.CheckEmptyAndFocus(frmAddress, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmAddTime, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmRemark, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmTypeID, null)) return;

        #endregion
        Entity.No = frmNo.Text;
        Entity.Name = frmName.Text;
        Entity.Linkman = frmLinkman.Text;
        Entity.Department = frmDepartment.Text;
        Entity.Tel = frmTel.Text;
        Entity.Fax = frmFax.Text;
        Entity.Email = frmEmail.Text;
        Entity.QQ = frmQQ.Text;
        Entity.MSN = frmMSN.Text;
        Entity.Address = frmAddress.Text;
        Entity.AddTime = DateTime.Now;
        Entity.Remark = frmRemark.Text;
        Entity.CustomerTypeID = Convert.ToInt32(typeList.SelectedValue);

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