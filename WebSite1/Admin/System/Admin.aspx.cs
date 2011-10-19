using System;
using NewLife.CommonEntity;

public partial class Pages_Admin : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // 添加按钮需要添加权限
            lbAdd.Visible = Acquire(PermissionFlags.Insert);
            // 最后一列是删除列，需要删除权限
            GridView1.Columns[GridView1.Columns.Count - 1].Visible = Acquire(PermissionFlags.Delete);
        }

        Type type = CommonManageProvider.Provider.AdminstratorType;
        ObjectDataSource1.TypeName = type.FullName;
        ObjectDataSource1.DataObjectTypeName = type.FullName;

        type = CommonManageProvider.Provider.RoleType;
        ObjectDataSource2.TypeName = type.FullName;
        ObjectDataSource2.DataObjectTypeName = type.FullName;
    }
}