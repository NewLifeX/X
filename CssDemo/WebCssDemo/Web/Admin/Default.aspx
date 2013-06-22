<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="Admin_Default" %>

<%@ Import Namespace="NewLife.CommonEntity" %>

<!DOCTYPE html>
<html lang="en">
<head id="Head1" runat="server">
    <title>NewLife</title>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link rel="stylesheet" href="<%= ResolveUrl("~/UI/css/bootstrap.min.css")%>" type="text/css" />
    <link rel="stylesheet" href="<%= ResolveUrl("~/UI/css/bootstrap-responsive.min.css")%>" type="text/css" />
    <link rel="stylesheet" href="<%= ResolveUrl("~/UI/css/unicorn.main.css")%>" type="text/css" />
    <link rel="stylesheet" href="<%= ResolveUrl("~/UI/css/unicorn.grey.css")%>" class="skin-color" type="text/css" />
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <script src="<%= ResolveUrl("~/UI/js/excanvas.min.js")%>" type="text/javascript"></script>
    <script src="<%= ResolveUrl("~/UI/js/jquery.min.js")%>" type="text/javascript"></script>
    <script src="<%= ResolveUrl("~/UI/js/jquery.ui.custom.js")%>" type="text/javascript"></script>
    <script src="<%= ResolveUrl("~/UI/js/bootstrap.min.js")%>" type="text/javascript"></script>
    <script src="<%= ResolveUrl("~/UI/js/unicorn.js")%>" type="text/javascript"></script>
    <script src="<%= ResolveUrl("~/Scripts/Common.js")%>" type="text/javascript"></script>
    <style type="text/css">
        #maincontent {
            position: absolute;
            width: 100%;
            margin-top: -38px;
            z-index: 19;
            border-radius: 8px 8px 8px 8px;
            min-height: 600px;
        }

        #perch {
            position: absolute;
            width: 100%;
            margin-top: -38px;
            height: 600px;
            z-index: 18;
            border-radius: 8px 8px 8px 8px;
            background-color: White;
        }

        body {
            overflow: hidden;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div id="header">
            <h1>
                <a href="javascript:void(0)"><%=Config.DisplayName%> v<%=Config.Version%><font style="font-size: 12px; color: Blue;">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;——<%=Config.Company%></font></a></h1>
        </div>
        <div id="search">
            <input type="text" placeholder="Search here..." />
            <button type="submit" class="tip-right" title="Search"><i class="icon-search icon-white"></i></button>
        </div>
        <Custom:HeaderTool ID="HeaderTool" runat="server" />
        <Custom:LeftMenu ID="LeftMenu" runat="server"></Custom:LeftMenu>
        <div id="style-switcher">
            <i class="icon-arrow-left icon-white"></i><span>样式:</span> <a href="#grey" style="background-color: #555555; border-color: #aaaaaa;" title="灰色"></a><a href="#blue" style="background-color: #2D2F57;" title="蓝色"></a><a href="#red" style="background-color: #673232;" title="红色"></a>
        </div>
        <div id="content">
            <iframe id="maincontent" src="Main.aspx" name="maincontent" frameborder="0"></iframe>
            <div id="perch"></div>
        </div>
    </form>
</body>
</html>
