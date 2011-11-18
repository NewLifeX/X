using System;
using NewLife.CommonEntity;

public partial class Pages_Admin : MyEntityList
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return CommonManageProvider.Provider.AdminstratorType; } set { base.EntityType = value; } }

    protected void Page_Load(object sender, EventArgs e)
    {
        Type type = CommonManageProvider.Provider.RoleType;
        ObjectDataSource2.TypeName = type.FullName;
        ObjectDataSource2.DataObjectTypeName = type.FullName;
    }
}