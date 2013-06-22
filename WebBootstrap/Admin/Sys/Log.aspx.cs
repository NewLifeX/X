using System;
using NewLife.CommonEntity;
using XCode;

public partial class Pages_Log : MyEntityList<NewLife.CommonEntity.Log>
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Type type = CommonManageProvider.Provider.LogType;
        ods.TypeName = type.FullName;
        ods.DataObjectTypeName = type.FullName;
        odsCategory.TypeName = type.FullName;
        odsCategory.DataObjectTypeName = type.FullName;

        if (!IsPostBack)
        {
            IEntityOperate eop = EntityFactory.CreateOperate(CommonManageProvider.Provider.AdminstratorType);
            if (eop != null)
            {
                // 管理员选项最多只要50个
                ddlAdmin.DataSource = eop.FindAll(null, null, null, 0, 50);
                ddlAdmin.DataBind();
            }
        }
    }
}