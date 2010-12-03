using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class ControlTest : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            //DataPager2.TotalRowCount = Area.FindCount();

            bind(0);
        }

        ObjectDataSource1.Selecting += new ObjectDataSourceSelectingEventHandler(ObjectDataSource1_Selecting);
        ObjectDataSource1.Selected += new ObjectDataSourceStatusEventHandler(ObjectDataSource1_Selected);
    }

    void ObjectDataSource1_Selecting(object sender, ObjectDataSourceSelectingEventArgs e)
    {
        if (!e.ExecutingSelectCount)
        {
            //e.Arguments.StartRowIndex = DataPager2.StartRowIndex;
            //e.Arguments.MaximumRows = DataPager2.PageSize;
            //e.Arguments.RetrieveTotalRowCount = true;
        }
    }

    void ObjectDataSource1_Selected(object sender, ObjectDataSourceStatusEventArgs e)
    {
        //if (e.ReturnValue is Int32) DataPager2.TotalRowCount = (Int32)e.ReturnValue;
    }

    void bind(Int32 index)
    {
        //Repeater1.DataSource = Area.FindAllByName(null, null, null, index * DataPager2.PageSize, DataPager2.PageSize);
        //Repeater1.DataBind();
    }
    protected void DataPager2_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        Label1.Text = String.Format("PageIndexChanging {0}", e.NewPageIndex);

        bind(e.NewPageIndex);
    }
    protected void DataPager2_PageIndexChanged(object sender, EventArgs e)
    {
        Label2.Text = String.Format("PageIndexChanged {0}", DataPager2.PageIndex);
    }
    protected void DataPager2_PageCommand(object sender, CommandEventArgs e)
    {
        Label3.Text = String.Format("PageCommand {0}={1}", e.CommandName, e.CommandArgument);
    }
    protected void Label4_DataBinding(object sender, EventArgs e)
    {

    }
    protected void Label5_DataBinding(object sender, EventArgs e)
    {

    }
    protected void LabelCurrentPage_DataBinding(object sender, EventArgs e)
    {

    }
}