using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Log;
using NewLife.Web;
using NewLife.CommonEntity;

public partial class Common_RoleForm : MyEntityForm<Role>
{
    protected void Page_Load(object sender, EventArgs e)
    {
        MasterPage.SetToolBar(false);
    }
}