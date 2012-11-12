using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using <#=Config.NameSpace#>;

public partial class <#=Config.EntityConnName+"_"+Table.Name#> : MyEntityList<<#=Table.Name#>>
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            btnDelete.OnClientClick = gv.GetNoSelectionAlertReference("至少选择一项！");
            btnNew.OnClientClick = win.GetShowReference("<#=Table.Name#>Form.aspx", "新增 - <#=Table.DisplayName#>");
        }
    }

    protected void btnDelete_Click(object sender, EventArgs e)
    {
        gv.DeleteSelected();
    }
}