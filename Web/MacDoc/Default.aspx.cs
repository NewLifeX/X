using System;
using NewLife.CommonEntity;

public partial class MacDoc_Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            IMenu root = CommonManageProvider.Provider.MenuRoot;
            if (root != null)
                root
                    .CheckMenuName("MacDoc", "机器档案子系统")
                    .CheckMenuName(@"MacDoc\Custom", "客户")
                    .CheckMenuName(@"MacDoc\Mac", "机器")
                    ;
        }
    }
}