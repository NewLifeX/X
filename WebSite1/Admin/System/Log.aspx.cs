using System;
using NewLife.CommonEntity;

public partial class Pages_Log : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Type type = CommonManageProvider.Provider.LogType;
        ObjectDataSource1.TypeName = type.FullName;
        ObjectDataSource1.DataObjectTypeName = type.FullName;
        ObjectDataSource3.TypeName = type.FullName;
        ObjectDataSource3.DataObjectTypeName = type.FullName;

        type = CommonManageProvider.Provider.AdminstratorType;
        ObjectDataSource2.TypeName = type.FullName;
        ObjectDataSource2.DataObjectTypeName = type.FullName;
    }
}