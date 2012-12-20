using System;
using NewLife.CommonEntity;
using NewLife.Log;

public partial class Test : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        ICommonManageProvider provider = CommonManageProvider.Provider;
        XTrace.WriteLine(provider.AdminstratorType + "");
        XTrace.WriteLine(provider.RoleType + "");
        XTrace.WriteLine(provider.MenuType + "");
        XTrace.WriteLine(provider.MenuRoot + "");
    }
}