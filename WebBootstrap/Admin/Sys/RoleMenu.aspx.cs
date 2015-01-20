using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Web;
using XCode;

/*
 * 该页面极为复杂，如无特殊需求，不建议修改。
 * 如果想要八个自定义权限，可以设置IsFullPermission为true
 */

public partial class Common_RoleMenu : MyEntityList
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return CommonManageProvider.Provider.MenuType; } set { base.EntityType = value; } }

    IEntityOperate Factory { get { return EntityFactory.CreateOperate(EntityType); } }

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        ods.DataObjectTypeName = ods.TypeName = CommonManageProvider.Provider.MenuType.FullName;
        odsRole.DataObjectTypeName = odsRole.TypeName = CommonManageProvider.Provider.RoleType.FullName;

        //if (!IsPostBack)
        {
            Int32 roleID = WebHelper.RequestInt("RoleID");
            if (roleID > 0)
            {
                ddlRole.DataBind();

                ddlRole.SelectedValue = roleID.ToString();
            }

            IMenu root = CommonManageProvider.Provider.MenuRoot;
            if (root != null)
            {
                ddlCategory.DataSource = root.Childs;
                ddlCategory.DataBind();
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e) { }

    public Int32 RoleID { get { return String.IsNullOrEmpty(ddlRole.SelectedValue) ? 0 : Convert.ToInt32(ddlRole.SelectedValue); } }

    /// <summary>是否使用完整权限。完整权限包括8个自定义权限</summary>
    protected Boolean IsFullPermission { get { return false; } }

    protected void ddlRole_SelectedIndexChanged(object sender, EventArgs e) { gv.DataBind(); }

    protected void gv_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row == null) return;

        IMenu entity = e.Row.DataItem as IMenu;
        if (entity == null) return;

        CheckBox cb = e.Row.FindControl("CheckBox1") as CheckBox;
        CheckBoxList cblist = e.Row.FindControl("CheckBoxList1") as CheckBoxList;

        // 检查权限
        PermissionFlags pf = FindByRoleAndMenu(RoleID, entity.ID);
        Role role = Role.FindByID(RoleID);
        cb.Checked = role.Permissions.ContainsKey(entity.ID);
        cb.ToolTip = pf.ToString();

        // 如果有子节点，则不显示
        if (entity.Childs != null && entity.Childs.Count > 0)
        {
            //cb.Visible = false;
            cblist.Visible = false;
            return;
        }

        // 检查权限
        Dictionary<PermissionFlags, String> flags = EnumHelper.GetDescriptions<PermissionFlags>();
        cblist.Items.Clear();
        foreach (PermissionFlags item in flags.Keys)
        {
            if (item == PermissionFlags.None) continue;
            if (!IsFullPermission && item > PermissionFlags.Delete) continue;

            ListItem li = new ListItem(flags[item], ((Int32)item).ToString());
            if ((pf & item) == item) li.Selected = true;
            cblist.Items.Add(li);
        }
    }

    protected void CheckBox1_CheckedChanged(object sender, EventArgs e)
    {
        if (RoleID < 1) return;

        CheckBox cb = sender as CheckBox;
        if (cb == null) return;

        GridViewRow row = cb.BindingContainer as GridViewRow;
        if (row == null) return;

        IMenu menu = CommonManageProvider.Provider.MenuRoot.AllChilds[row.DataItemIndex] as IMenu;
        if (menu == null) return;

        // 检查权限
        PermissionFlags pf = FindByRoleAndMenu(RoleID, menu.ID);
        if (cb.Checked)
        {
            // 没有权限，增加
            if (pf == PermissionFlags.None)
            {
                if (!Manager.Acquire(PermissionFlags.Insert))
                {
                    WebHelper.Alert("没有添加权限！");
                    return;
                }

                Role role = Role.FindByID(RoleID);
                role.Set(menu.ID, PermissionFlags.All);
                role.Save();

                // 如果父级没有授权，则授权
                CheckAndAddParent(RoleID, menu);
            }
        }
        else
        {
            // 如果有权限，删除
            if (pf != PermissionFlags.None)
            {
                if (!Manager.Acquire(PermissionFlags.Delete))
                {
                    WebHelper.Alert("没有删除权限！");
                    return;
                }

                //(rm as IEntity).Delete();
                Role role = Role.FindByID(RoleID);
                role.Remove(menu.ID);
                role.Save();
            }
        }

        // 清空缓存，否则一会绑定的时候会绑定旧数据
        //_rms = null;
        gv.DataBind();
    }

    protected void CheckBoxList1_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (RoleID < 1) return;

        CheckBoxList cb = sender as CheckBoxList;

        //只需判断cb是否为空，该角色只有“查看”权限时cb.SelectedItem为空。
        //if (cb == null || cb.SelectedItem == null) return;
        if (cb == null) return;

        GridViewRow row = cb.BindingContainer as GridViewRow;
        if (row == null) return;

        IMenu menu = CommonManageProvider.Provider.MenuRoot.AllChilds[row.DataItemIndex] as IMenu;
        if (menu == null) return;

        // 检查权限
        PermissionFlags pf = FindByRoleAndMenu(RoleID, menu.ID);
        Role role = Role.FindByID(RoleID);
        // 没有权限，增加
        if (pf == PermissionFlags.None)
        {
            if (!Manager.Acquire(PermissionFlags.Insert))
            {
                WebHelper.Alert("没有添加权限！");
                return;
            }

            role.Set(menu.ID, PermissionFlags.None);
        }

        // 遍历权限项
        PermissionFlags flag = PermissionFlags.None;
        foreach (ListItem item in cb.Items)
        {
            if (item.Selected) flag |= (PermissionFlags)(Int32.Parse(item.Value));
        }

        if (pf != flag)
        {
            if (!Manager.Acquire(PermissionFlags.Update))
            {
                WebHelper.Alert("没有编辑权限！");
                return;
            }

            role.Permissions[menu.ID] = flag;

            // 如果父级没有授权，则授权
            CheckAndAddParent(RoleID, menu);
        }
        role.Save();

        //// 清空缓存，否则一会绑定的时候会绑定旧数据
        //_rms = null;
        gv.DataBind();
    }

    void CheckAndAddParent(Int32 roleid, IMenu menu)
    {
        // 如果父级没有授权，则授权
        while ((menu = menu.Parent) != null)
        {
            //IRoleMenu rm = FindByRoleAndMenu(roleid, menu.ID);
            //if (rm == null)
            //{
            //    rm = Factory.Create(false) as IRoleMenu;
            //    rm.RoleID = roleid;
            //    rm.MenuID = menu.ID;
            //    rm.PermissionFlag = PermissionFlags.All;
            //    rm.Save();
            //}
        }
    }

    //EntityList<IEntity> _rms;
    PermissionFlags FindByRoleAndMenu(Int32 roleID, Int32 menuID)
    {
        Role role = Role.FindByID(roleID);
        return role == null ? PermissionFlags.None : role.Get(menuID);
    }
}