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
        ObjectDataSource1.TypeName = type.FullName;
        ObjectDataSource1.DataObjectTypeName = type.FullName;

        type = CommonManageProvider.Provider.RoleType;
        ObjectDataSource2.TypeName = type.FullName;
        ObjectDataSource2.DataObjectTypeName = type.FullName;
    }
}