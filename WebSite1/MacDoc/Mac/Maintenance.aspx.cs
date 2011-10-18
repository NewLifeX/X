using System;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;

using NewLife.YWS.Entities;

public partial class Pages_Maintenance : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // 添加按钮需要添加权限
            Button2.Visible = Acquire(PermissionFlags.Insert);
            // 最后一列是删除列，需要删除权限
            GridView1.Columns[GridView1.Columns.Count - 1].Visible = Acquire(PermissionFlags.Delete);
        }
    }
   
}