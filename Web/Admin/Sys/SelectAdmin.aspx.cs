using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;

public partial class Admin_SelectAdmin : MyEntityList
{
    protected override void OnPreLoad(EventArgs e)
    {
        Manager.ValidatePermission = false;

        base.OnPreLoad(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            lblmsg.Text = Request["Name"];
        }

        Type type = CommonManageProvider.Provider.AdminstratorType;
        ods.TypeName = type.FullName;
        ods.DataObjectTypeName = type.FullName;

        type = CommonManageProvider.Provider.RoleType;
        odsRole.TypeName = type.FullName;
        odsRole.DataObjectTypeName = type.FullName;
    }
}