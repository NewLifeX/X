/*
 * XCoder v3.2.2010.1014
 * 作者：SUN/SUN-PC
 * 时间：2010-12-22 20:05:31
 * 版权：版权所有 (C) 新生命开发团队 2010
*/
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using XControl;
using NewLife.Web;
using NewLife.YWS.Entities;
using NewLife.CommonEntity;



public partial class CustomerTypeForm : PageBase
{
    /// <summary>编号</summary>
    public Int32 EntityID { get { return WebHelper.RequestInt("ID"); } }

    private CustomerType _Entity;
    /// <summary>客户类型</summary>
    public CustomerType Entity
    {
        get { return _Entity ?? (_Entity = CustomerType.FindByKeyForEdit(EntityID)); }
        set { _Entity = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            DataBind();

            frmParentID.Items.Add(new ListItem("|-根类别", "0"));
            if (CustomerType.Root.AllChilds != null && CustomerType.Root.AllChilds.Count > 0)
            {
                foreach (CustomerType item in CustomerType.Root.AllChilds)
                {
                    String spaces = new String('　', item.Deepth);
                    frmParentID.Items.Add(new ListItem(spaces + "|- " + item.Name, item.ID.ToString()));
                }
            }
            //frmParentID.SelectedValue = Entity.ParentID.ToString();
            if (Entity != null) frmParentID.SelectedValue = Entity.ParentID.ToString();

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
            WebHelper.Alert("没有编辑权限！");
            return;
        }
        if (EntityID <= 0 && !Acquire(PermissionFlags.Insert))
        {
            WebHelper.Alert("没有添加权限！");
            return;
        }

        //if (frmParentID.SelectedValue.Equals("0"))
        //{
        //    WebHelper.Alert("请选择客户类型！");
        //    return;
        //}

        //if (!WebHelper.CheckEmptyAndFocus(frmName, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmParentID, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmAddTime, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmOperator, null)) return;
        
        Entity.Name = frmName.Text;
        Entity.ParentID = Convert.ToInt32(frmParentID.SelectedValue);
        Entity.AddTime = DateTime.Now;
        Entity.Operator2 = Current.Name;

        try
        {
            Entity.Save();
            //WebHelper.AlertAndRedirect("成功！", "CustomerType.aspx");
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('成功！');parent.Dialog.CloseAndRefresh(frameElement);", true);
        }
        catch (Exception ex)
        {
            WebHelper.Alert("失败！" + ex.Message);
        }
    }
}