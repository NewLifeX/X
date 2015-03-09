using System;
using NewLife.CommonEntity;
using NewLife.Web;
using XCode;
using XCode.Web;

public partial class Pages_Log : MyEntityList
{
    public Grid grid = new Grid(Log.Meta.Factory);

    protected void Page_Load(object sender, EventArgs e)
    {
        grid.DefaultPageSize = 10;

        Type type = CommonManageProvider.Provider.LogType;
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