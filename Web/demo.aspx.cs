using RazorEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class demo : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string template = "Hello @Model.Name! Welcome to Razor!";
        string result2 = Razor.Parse(template, new { Name = "World" }, "Sample");
        Response.Write(result2);
    }
}