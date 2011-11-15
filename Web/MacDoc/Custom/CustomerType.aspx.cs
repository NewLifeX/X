/*
 * XCoder v3.2.2010.1014
 * 作者：SUN/SUN-PC
 * 时间：2010-12-22 20:05:34
 * 版权：版权所有 (C) 新生命开发团队 2010
*/
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;

using NewLife.YWS.Entities;

public partial class Pages_CustomerType : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // 添加按钮需要添加权限
            lbAdd.Visible = Acquire(PermissionFlags.Insert);
            // 最后一列是删除列，需要删除权限
            GridView1.Columns[GridView1.Columns.Count - 1].Visible = Acquire(PermissionFlags.Delete);
        }
    }
}