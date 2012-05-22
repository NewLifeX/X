using System;
using System.Collections.Generic;

public partial class DropDownList : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string p = Request.QueryString["data"];
        if (p == null)
        {
            if (Request.QueryString["badval"] != null) Select();
        }
        else if (p == "1")
        {
            Select();
            BindData();
        }
        else if (p == "2")
        {
            BindData();
            Select();
        }
    }

    private XControl.DropDownList[] All
    {
        get
        {
            return new XControl.DropDownList[] {
                DropDownList1, DropDownList2, DropDownList3, DropDownList4,
                DropDownList5, DropDownList6, DropDownList7, DropDownList8
            };
        }
    }

    private void Select()
    {
        foreach (XControl.DropDownList item in All)
        {
            if (Request.QueryString["badval"] != null)
            {
                item.SelectedValue = "设置为无效值";
            }
            else
            {
                item.SelectedValue = "3";
            }
        }
    }

    private void BindData()
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data["1"] = "测试1";
        data["2"] = "测试2";
        data["3"] = "测试3";
        data["4"] = "测试4";
        data["5"] = "测试5";

        foreach (XControl.DropDownList item in All)
        {
            item.DataTextField = "value";
            item.DataValueField = "key";
            item.DataSource = data;
            item.DataBind();
        }
    }
}