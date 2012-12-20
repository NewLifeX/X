﻿using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;

public partial class Common_Department : MyEntityList<Department>
{
    protected void Page_Load(object sender, EventArgs e)
    {
    }

    protected void gv_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Up")
        {
            IDepartment entity = Department.FindByID(Convert.ToInt32(e.CommandArgument));
            if (entity != null)
            {
                entity.Up();
                gv.DataBind();
            }
        }
        else if (e.CommandName == "Down")
        {
            IDepartment entity = Department.FindByID(Convert.ToInt32(e.CommandArgument));
            if (entity != null)
            {
                entity.Down();
                gv.DataBind();
            }
        }
    }

    public Boolean IsFirst(Object dataItem)
    {
        IDepartment entity = dataItem as IDepartment;
        if (entity == null || entity.Parent == null) return true;
        return entity.ID == entity.Parent.Childs[0].ID;
    }

    public Boolean IsLast(Object dataItem)
    {
        IDepartment entity = dataItem as IDepartment;
        if (entity == null || entity.Parent == null) return false;
        return entity.ID == entity.Parent.Childs[entity.Parent.Childs.Count - 1].ID;
    }
}