using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

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
                DropDownList5, DropDownList6, DropDownList7, DropDownList8,
                DropDownList9, DropDownList10, DropDownList11, DropDownList12
            };
        }
    }

    public XControl.DropDownList[] UseSelectedItems
    {
        get
        {
            return new XControl.DropDownList[] {
                DropDownList9, DropDownList10, DropDownList11, DropDownList12
            };
        }
    }

    private void Select()
    {
        foreach (XControl.DropDownList item in All)
        {
            if (Request.QueryString["badval"] != null)
            {
                SetValue(item, "设置为无效值");
            }
            else
            {
                SetValue(item, "3");
            }
        }
    }

    private void SetValue(XControl.DropDownList item, string value)
    {
        if (new List<XControl.DropDownList>(UseSelectedItems).Contains(item))
        {
            ListItem i = item.Items.FindByValue(value);
            if (i != null)
            {
                i.Selected = true;
            }
        }
        else
        {
            item.SelectedValue = value;
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