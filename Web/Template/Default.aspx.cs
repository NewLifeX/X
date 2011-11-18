using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;

public partial class Template_Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            IMenu root = CommonManageProvider.Provider.MenuRoot;
            if (root != null)
            {
                root.CheckMenuName("Template", "模版子系统")
                    .CheckMenuName(@"Template\TemplateManage", "管理模版");
            }
        }
    }
}