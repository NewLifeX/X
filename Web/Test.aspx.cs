using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Log;
using NewLife.YWS.Entities;
using XCode;
using System.Reflection;
using System.IO;

public partial class Test : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Assembly asm = Assembly.LoadFrom(@"E:\X\代码生成\XCoder.exe");
        Template.ImportFromAssembly(0, asm, "XCoder.Template");
    }
}