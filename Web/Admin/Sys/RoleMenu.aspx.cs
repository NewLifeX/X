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

public partial class Pages_RoleMenu : MyEntityList
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

        IRole role = Role.FindByID(RoleID);
        Role.RowDataBound(sender, e, role, IsFullPermission);
    }

    protected void CheckBox1_CheckedChanged(object sender, EventArgs e)
    {
        if (RoleID < 1) return;

        IRole role = Role.FindByID(RoleID);
        if (!Role.CheckedChanged(sender, e, role)) return;

        // 清空缓存，否则一会绑定的时候会绑定旧数据
        //_rms = null;
        gv.DataBind();
    }

    protected void CheckBoxList1_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (RoleID < 1) return;

        IRole role = Role.FindByID(RoleID);
        if (!Role.SelectedIndexChanged(sender, e, role)) return;

        //// 清空缓存，否则一会绑定的时候会绑定旧数据
        //_rms = null;
        gv.DataBind();
    }
}