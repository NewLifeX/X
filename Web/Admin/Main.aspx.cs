using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using NewLife.Reflection;

public partial class Pages_Main : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            GridView1.DataSource = GetSource();
            GridView1.DataBind();
        }
    }

    List<AssemblyX> GetSource()
    {
        List<AssemblyX> list = new List<AssemblyX>();
        foreach (AssemblyX assem in AssemblyX.GetAssemblies())
        {
            if (assem.Company == "新生命开发团队" || assem.Company == "NewLife")
            {
                //ShowVer(ver);
                list.Add(assem);
            }
        }
        return list;

        //return AssemblyX.GetAssemblies().Where(e => e.Company == "新生命开发团队" || e.Company == "NewLife").ToList();
    }

    protected void GridView1_Sorting(object sender, GridViewSortEventArgs e)
    {
        List<AssemblyX> list = GetSource();
        list.Sort(delegate(AssemblyX item1, AssemblyX item2)
        {
            Int32 d = e.SortDirection == SortDirection.Ascending ? 1 : -1;
            if (e.SortExpression == "Compile")
                return d * item1.Compile.CompareTo(item2.Compile);
            else if (e.SortExpression == "Title")
                return d * item1.Title.CompareTo(item2.Title);
            else if (e.SortExpression == "FileVersion")
                return d * item1.FileVersion.CompareTo(item2.FileVersion);
            else if (e.SortExpression == "Version")
                return d * item1.Version.CompareTo(item2.Version);
            else if (e.SortExpression == "Name")
                return d * item1.Name.CompareTo(item2.Name);
            else
                return d * item1.Name.CompareTo(item2.Name);
        });

        GridView1.DataSource = list;
        GridView1.DataBind();
    }
}