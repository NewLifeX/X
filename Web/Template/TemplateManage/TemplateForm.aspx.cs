/*
 * XCoder v4.5.2011.1108
 * 作者：nnhy/NEWLIFE
 * 时间：2011-11-14 17:39:37
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;

public partial class Common_TemplateForm : MyEntityForm
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return typeof(Template); } set { base.EntityType = value; } }

    protected override void OnPreLoad(EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            // 在OnPreLoad之前初始化父菜单列表，因为EntityForm会在OnPreLoad阶段给表单赋值
            frmParentID.Items.Add(new ListItem("|-根", "0"));
            foreach (Template item in Template.Root.AllChilds)
            {
                String spaces = new String('　', item.Deepth);
                frmParentID.Items.Add(new ListItem(spaces + "|- " + item.Name, item.ID.ToString()));
            }
        }

        base.OnPreLoad(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}