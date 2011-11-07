using System;
using NewLife.CommonEntity;

public partial class Pages_AdminForm : MyEntityForm
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return CommonManageProvider.Provider.AdminstratorType; } set { base.EntityType = value; } }

    protected override void OnInitComplete(EventArgs e)
    {
        base.OnInitComplete(e);
        ods.DataObjectTypeName = ods.TypeName = CommonManageProvider.Provider.RoleType.FullName;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}