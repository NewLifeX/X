using RazorEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class file : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string template = File.ReadAllText(Server.MapPath("~/file.cshtml"));
        string result2 = Razor.Parse(template, new { Name = "World" }, "Sample");
        Response.Write(result2);
    }
}