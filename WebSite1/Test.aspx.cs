using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Log;
using NewLife.YWS.Entities;
using XCode;

public partial class Test : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        EntityList<Admin> list = Admin.FindAll();

        ICommonManageProvider provider = CommonManageProvider.Provider;
        Response.Write(provider.AdminstratorType.FullName);
    }
}