using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using <#=Config.NameSpace#>;

public partial class Pages_<#=Table.Alias#> : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // 添加按钮需要添加权限
            lbAdd.Visible = Acquire(PermissionFlags.Insert);
            // 最后一列是删除列，需要删除权限
            gv<#=Table.Alias#>.Columns[gv<#=Table.Alias#>.Columns.Count - 1].Visible = Acquire(PermissionFlags.Delete);
        }
    }
}