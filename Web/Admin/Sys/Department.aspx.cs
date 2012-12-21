﻿using System;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;

public partial class Common_Department : MyEntityList<Department>
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            String name = Department.GetLevelName(1);
            if (String.IsNullOrEmpty(name)) name = "部门";
            lbAdd.Title = lbAdd.Text = "新" + name;
        }
    }

    protected void gv_RowCommand(object sender, GridViewCommandEventArgs e)
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
        if (entity == null) return true;
        IDepartment parent = entity.Parent ?? Department.Root;
        return entity.ID == parent.Childs[0].ID;
    }

    public Boolean IsLast(Object dataItem)
    {
        IDepartment entity = dataItem as IDepartment;
        if (entity == null) return true;
        IDepartment parent = entity.Parent ?? Department.Root;
        return entity.ID == parent.Childs[parent.Childs.Count - 1].ID;
    }
}