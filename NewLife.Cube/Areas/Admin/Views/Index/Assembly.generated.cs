﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    
    #line 2 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
    using System.Collections;
    
    #line default
    #line hidden
    using System.Collections.Generic;
    
    #line 1 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
    using System.Diagnostics;
    
    #line default
    #line hidden
    using System.IO;
    using System.Linq;
    using System.Net;
    
    #line 3 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
    using System.Reflection;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
    using System.Runtime.Versioning;
    
    #line default
    #line hidden
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using NewLife;
    
    #line 5 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
    using NewLife.Common;
    
    #line default
    #line hidden
    using NewLife.Cube;
    using NewLife.Reflection;
    using NewLife.Web;
    using XCode;
    using XCode.Membership;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Areas/Admin/Views/Index/Assembly.cshtml")]
    public partial class _Areas_Admin_Views_Index_Assembly_cshtml : System.Web.Mvc.WebViewPage<dynamic>
    {
        public _Areas_Admin_Views_Index_Assembly_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 6 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
  
    Layout = NewLife.Cube.Setting.Current.Layout;

    ViewBag.Title = "程序集列表";

    var asm = Assembly.GetExecutingAssembly();
    var att = asm.GetCustomAttribute<TargetFrameworkAttribute>();
    var ver = att.FrameworkDisplayName ?? att.FrameworkName;

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n<table");

WriteLiteral(" class=\"table table-bordered table-hover table-striped table-condensed\"");

WriteLiteral(">\r\n    <tr>\r\n        <th");

WriteLiteral(" colspan=\"6\"");

WriteLiteral(">\r\n            程序集列表(");

            
            #line 19 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
             Write(AppDomain.CurrentDomain.FriendlyName);

            
            #line default
            #line hidden
WriteLiteral(" )\r\n        </th>\r\n    </tr>\r\n    <tr>\r\n        <th>名称</th>\r\n        <th>标题</th>\r" +
"\n        <th>文件版本</th>\r\n        <th>内部版本</th>\r\n        <th>编译时间</th>\r\n        <t" +
"h>路径</th>\r\n    </tr>\r\n");

            
            #line 30 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
    
            
            #line default
            #line hidden
            
            #line 30 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
     foreach (AssemblyX item in ViewBag.Asms)
    {

            
            #line default
            #line hidden
WriteLiteral("        <tr>\r\n            <td>\r\n");

WriteLiteral("                ");

            
            #line 34 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
           Write(item.Name);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </td>\r\n            <td>\r\n");

WriteLiteral("                ");

            
            #line 37 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
           Write(item.Title);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </td>\r\n            <td>\r\n");

WriteLiteral("                ");

            
            #line 40 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
           Write(item.FileVersion);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </td>\r\n            <td>");

            
            #line 42 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
           Write(item.Version);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n            <td>");

            
            #line 43 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
           Write(item.Compile.ToFullString());

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n            <td>\r\n");

            
            #line 45 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
                
            
            #line default
            #line hidden
            
            #line 45 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
                  
                    var location = String.Empty;
                    try
                    {
                        location = item.Asm.Location;
                    }
                    catch { }
                
            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                ");

            
            #line 53 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
           Write(location);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </td>\r\n        </tr>\r\n");

            
            #line 56 "..\..\Areas\Admin\Views\Index\Assembly.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("</table>");

        }
    }
}
#pragma warning restore 1591