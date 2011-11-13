/*
 * XCoder v4.5.2011.1108
 * 作者：nnhy/X
 * 时间：2011-11-13 22:43:45
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.CommonEntity;

public partial class Common_TemplateContent : MyEntityList
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return typeof(TemplateContent); } set { base.EntityType = value; } }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}