using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Reflection;

public partial class Pages_Main : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (ManageProvider.Provider.Current == null)
        {
            Response.Write("无权访问！");
            Response.End();
            return;
        }

        if (Request["Act"] == "Restart")
        {
            HttpRuntime.UnloadAppDomain();
            Response.Redirect(Path.GetFileName(Request.Path));
            return;
        }

        if (!IsPostBack)
        {
            gv.DataSource = GetSource();
            gv.DataBind();
        }
    }

    List<AssemblyX> GetSource()
    {
        String bin = HttpRuntime.BinDirectory.ToLower();
        List<AssemblyX> list = new List<AssemblyX>();
        foreach (AssemblyX asmx in AssemblyX.GetAssemblies())
        {
            if (String.IsNullOrEmpty(asmx.FileVersion)) continue;
            String file = asmx.Asm.CodeBase;
            if (String.IsNullOrEmpty(file)) continue;
            file = file.ToLower();
            if (file.StartsWith("file:///")) file = file.Substring("file:///".Length);
            file = file.Replace("/", "\\");
            if (!file.StartsWith(bin)) continue;

            list.Add(asmx);
        }

        return list;
    }

    protected void gv_Sorting(object sender, GridViewSortEventArgs e)
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

        gv.DataSource = list;
        gv.DataBind();
    }

    protected String GetWebServerName()
    {
        String name = Request.ServerVariables["Server_SoftWare"];
        if (String.IsNullOrEmpty(name)) name = Process.GetCurrentProcess().ProcessName;

        // 检测集成管道，低版本.Net不支持
        try
        {
            if (UsingIntegratedPipeline()) name += " [集成管道]";
        }
        catch { }

        return name;
    }

    Boolean UsingIntegratedPipeline()
    {
        return HttpRuntime.UsingIntegratedPipeline;
    }
}