using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Web;

using Menu = NewLife.CommonEntity.Menu;
using NewLife.CommonEntity;

public partial class Pages_MenuForm : PageBase
{
    /// <summary>编号</summary>
    public Int32 EntityID { get { return WebHelper.RequestInt("ID"); } }

    private Menu _Entity;
    /// <summary>菜单</summary>
    public Menu Entity
    {
        get { return _Entity ?? (_Entity = Menu.FindByKeyForEdit(EntityID)); }
        set { _Entity = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            DataBind();

            frmParentID.Items.Add(new ListItem("|-根菜单", "0"));
            foreach (Menu item in Menu.Root.AllChilds)
            {
                String spaces = new String('　', item.Deepth);
                frmParentID.Items.Add(new ListItem(spaces + "|- " + item.Name, item.ID.ToString()));
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

        if (!WebHelper.CheckEmptyAndFocus(frmName, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmUrl, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmParentID, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmSort, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmRemark, null)) return;
        //if (!WebHelper.CheckEmptyAndFocus(frmPermission, null)) return;

        Entity.Name = frmName.Text;
        Entity.Url = frmUrl.Text;
        Entity.ParentID = Convert.ToInt32(frmParentID.SelectedValue);
        Entity.Sort = frmSort.Value;
        Entity.IsShow = frmIsShow.Checked;
        Entity.Remark = frmRemark.Text;
        Entity.Permission = frmPermission.Text;
        Entity.IsShow = frmIsShow.Checked;

        try
        {
            Entity.Save();
            //WebHelper.AlertAndRedirect("成功！", "Menu.aspx");
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('成功！');parent.Dialog.CloseAndRefresh(frameElement);", true);
        }
        catch (Exception ex)
        {
            WebHelper.Alert("失败！" + ex.Message);
        }
    }
}