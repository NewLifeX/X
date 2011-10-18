using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Reflection;

using Menu = NewLife.CommonEntity.Menu;
using NewLife.Web;
using System.Reflection;

public partial class Pages_RoleMenu : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            DropDownList1.DataBind();
        }
    }

    public Int32 RoleID
    {
        get
        {
            return String.IsNullOrEmpty(DropDownList1.SelectedValue) ? 0 : Convert.ToInt32(DropDownList1.SelectedValue);
        }
    }

    protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e)
    {
        GridView1.DataBind();
    }

    protected void GridView1_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row == null) return;

        Menu entity = e.Row.DataItem as Menu;
        if (entity == null) return;

        CheckBox cb = e.Row.FindControl("CheckBox1") as CheckBox;
        CheckBoxList cblist = e.Row.FindControl("CheckBoxList1") as CheckBoxList;

        // 检查权限
        RoleMenu rm = RoleMenu.FindByRoleAndMenu(RoleID, entity.ID);
        cb.Checked = (rm != null);
        if (rm != null) cb.ToolTip = rm.PermissionFlag.ToString();
        //if (rm != null) cb.Text = rm.PermissionFlag.ToString();

        // 如果有子节点，则不显示
        if (entity.Childs != null && entity.Childs.Count > 0)
        {
            //cb.Visible = false;
            cblist.Visible = false;
            return;
        }

        // 检查权限
        Dictionary<PermissionFlags, String> flags = GetDescriptions();
        cblist.Items.Clear();
        foreach (PermissionFlags item in flags.Keys)
        {
            if (item == PermissionFlags.None) continue;

            ListItem li = new ListItem(flags[item], ((Int32)item).ToString());
            if (rm != null && (rm.PermissionFlag & item) == item) li.Selected = true;
            cblist.Items.Add(li);
        }
    }
    string formtitle = string.Empty;
    protected void CheckBox1_CheckedChanged(object sender, EventArgs e)
    {
        if (RoleID < 1) return;

        CheckBox cb = sender as CheckBox;
        if (cb == null) return;

        GridViewRow row = cb.BindingContainer as GridViewRow;
        if (row == null) return;

        Menu entity = Menu.Root.AllChilds[row.DataItemIndex] as Menu;
        if (entity == null) return;
        formtitle = entity.Name;

        // 检查权限
        RoleMenu rm = RoleMenu.FindByRoleAndMenu(RoleID, entity.ID);
        if (cb.Checked)
        {
            // 没有权限，增加
            if (rm == null)
            {
                if (!Acquire(PermissionFlags.Insert))
                {
                    WebHelper.Alert("没有添加权限！");
                    return;
                }

                rm = new RoleMenu();
                rm.RoleID = RoleID;
                rm.MenuID = entity.ID;
                rm.PermissionFlag = PermissionFlags.All;
                rm.Save();
            }
        }
        else
        {
            // 如果有权限，删除
            if (rm != null)
            {
                if (!Acquire(PermissionFlags.Delete))
                {
                    WebHelper.Alert("没有删除权限！");
                    return;
                }

                rm.Delete();
            }
        }

        GridView1.DataBind();
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

        Menu entity = Menu.Root.AllChilds[row.DataItemIndex] as Menu;
        if (entity == null) return;
        formtitle = entity.Name;

        // 检查权限
        RoleMenu rm = RoleMenu.FindByRoleAndMenu(RoleID, entity.ID);
        // 没有权限，增加
        if (rm == null)
        {
            if (!Acquire(PermissionFlags.Insert))
            {
                WebHelper.Alert("没有添加权限！");
                return;
            }

            rm = new RoleMenu();
            rm.RoleID = RoleID;
            rm.MenuID = entity.ID;
        }

        // 遍历权限项
        PermissionFlags flag = PermissionFlags.None;
        foreach (ListItem item in cb.Items)
        {
            if (item.Selected) flag |= (PermissionFlags)(Int32.Parse(item.Value));
        }

        if (rm.PermissionFlag != flag)
        {
            if (!Acquire(PermissionFlags.Update))
            {
                WebHelper.Alert("没有编辑权限！");
                return;
            }

            rm.PermissionFlag = flag;
            rm.Save();
        }

        GridView1.DataBind();
    }

    static Dictionary<PermissionFlags, String> flagCache;
    static Dictionary<PermissionFlags, String> GetDescriptions()
    {
        if (flagCache != null) return flagCache;

        flagCache = new Dictionary<PermissionFlags, string>();

        TypeX type = typeof(PermissionFlags);
        foreach (FieldInfo item in type.BaseType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (!item.IsStatic) continue;

            // 这里的快速访问方法会报错
            //FieldInfoX fix = FieldInfoX.Create(item);
            //PermissionFlags value = (PermissionFlags)fix.GetValue(null);
            PermissionFlags value = (PermissionFlags)item.GetValue(null);

            String des = item.Name;
            DescriptionAttribute att = AttributeX.GetCustomAttribute<DescriptionAttribute>(item, false);
            if (att != null && !String.IsNullOrEmpty(att.Description)) des = att.Description;
            flagCache.Add(value, des);
        }

        return flagCache;
    }

    //重写权限验证方法，对网站配置文件加强控制
    public override bool Acquire(PermissionFlags flag)
    {
        if (!string.IsNullOrEmpty(formtitle) && (formtitle == "网站配置" || formtitle == "网站数据库" || formtitle == "网站日志"))
        {
            if ((Administrator<NewLife.YWS.Entities.Admin>.Current.Role.Menus.Find(RoleMenu._.MenuID, MyMenu.ID).PermissionFlag & PermissionFlags.Custom1) == 0) return false;
        }
        return base.Acquire(flag);
    }
}