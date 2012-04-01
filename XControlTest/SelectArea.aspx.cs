using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class window_SelectArea : System.Web.UI.Page
{
    public int areacode
    {
        get
        {
            string id = Request.QueryString["areacode"];
            int result = 0;
            if (string.IsNullOrEmpty(id)) return 0;
            else
            {
                if (int.TryParse(id, out result)) return result;
                else return 0;

            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {

            //List<Area> list = Area.FindAllByParent(0);
            //if (list == null) list = new List<Area>();
            //Area entity = new Area();
            //entity.Name = "--请选择--";
            //list.Insert(0, entity);
            //DropDownList1.DataSource = list;
            //DropDownList1.DataTextField = "Name";
            //DropDownList1.DataValueField = "Code";
            //DropDownList1.DataBind();
            //DropDownList1.SelectedIndex = 0;

            //if (areacode > 1)
            //{
            //    string code = areacode.ToString();
            //    string province = code.Substring(2, 4);
            //    string city = code.Substring(4, 2);

            //    if (province == "0000")
            //    {

            //        DropDownList1.SelectedValue = code;

            //    }
            //    else if (city == "00")
            //    {
            //        Area area = Area.FindByCode(areacode);

            //        DropDownList1.SelectedValue = area.ParentCode.ToString();
            //        DropDownList2_DataBind(area.ParentCode);

            //        DropDownList2.SelectedValue = area.Code.ToString();
            //    }
            //}
        }
    }

    protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e)
    {
        int result = 0;
        if (int.TryParse(DropDownList1.SelectedValue, out result))
        {
            if (result == 0)
                result = -1;
            DropDownList2_DataBind(result);
        }
    }

    private void DropDownList2_DataBind(int code)
    {
        //DropDownList2.DataSource = Area.FindAllByParent(code, "--请选择--");
        //DropDownList2.DataTextField = "Name";
        //DropDownList2.DataValueField = "Code";
        //DropDownList2.DataBind();
    }
}