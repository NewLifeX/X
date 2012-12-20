﻿using System;
using NewLife.CommonEntity;

public partial class Common_DepartmentForm : MyEntityForm<Department>
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            String name = "";
            if (EntityForm.IsNew)
                name = Department.GetLevelName((Entity.Parent != null ? Entity.Parent.Level : 0) + 1);
            else
                name = Entity.LevelName;

            if (String.IsNullOrEmpty(name)) name = "部门";
            lbTitle.Text = name;
            btnCopy.Text = "另存" + name;
        }
    }
}