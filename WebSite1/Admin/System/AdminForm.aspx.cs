using System;
using System.Web.UI;
using NewLife.Security;
using NewLife.Web;

using NewLife.YWS.Entities;
using NewLife.CommonEntity;

public partial class Pages_AdminForm : PageBase
{
    /// <summary>编号</summary>
    public Int32 EntityID { get { return WebHelper.RequestInt("ID"); } }

    private Admin _Entity;
    /// <summary>系统管理员</summary>
    public Admin Entity
    {
        get { return _Entity ?? (_Entity = Admin.FindByKeyForEdit(EntityID)); }
        set { _Entity = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            DataBind();
            frmRoleID.SelectedValue = Entity.RoleID.ToString();

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

        if (!WebHelper.CheckEmptyAndFocus(frmName, "必须填写登录名！")) return;

        if (EntityID > 0)
        {
            int NameCount = Admin.FindCount(Admin._.Name, frmName.Text);
            if (NameCount > 1)
            {
                WebHelper.AlertAndRefresh("登录名已存在！");
                return;
            }
            if (NameCount == 1)
            {
                int keyid = Admin.Find(Admin._.Name, frmName.Text).ID;
                if (keyid != EntityID)
                {
                    WebHelper.AlertAndRefresh("登录名已存在！");
                    return;
                }
            }
        }

        if (EntityID <= 0 && !WebHelper.CheckEmptyAndFocus(frmPassword, "必须填写密码！")) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmDisplayName, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmRoleID, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmLogins, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmLastLogin, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmLastLoginIP, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmIsEnable, null)) return;

        Entity.Name = frmName.Text;
        Entity.DisplayName = frmDisplayName.Text;
        Entity.RoleID = Convert.ToInt32(frmRoleID.SelectedValue);
        //Entity.RoleID = frmRoleID.Value;
        //Entity.Logins = frmLogins.Value;
        //Entity.LastLogin = frmLastLogin.Value;
        //Entity.LastLoginIP = frmLastLoginIP.Text;
        Entity.IsEnable = frmIsEnable.Checked;
        Entity.QQ = frmQQ.Text;
        Entity.MSN = frmMSN.Text;
        Entity.Email=frmEmail.Text;
        Entity.Phone = frmPhone.Text;

        if (!String.IsNullOrEmpty(frmPassword.Text))
            Entity.Password = DataHelper.Hash(frmPassword.Text);

        try
        {
            Entity.Save();
            //WebHelper.AlertAndRedirect("成功！", "Admin.aspx");
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('成功！');parent.Dialog.CloseAndRefresh(frameElement);", true);
        }
        catch (Exception ex)
        {
            WebHelper.Alert("失败！" + ex.Message);
        }
    }
}