<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default3.aspx.cs" Inherits="Default3" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <div style="background-color:<%=RandomColor() %>"><%=DateTime.Now.ToLongTimeString() %></div>
        
        <hr/>
        <XCL:LinkBox ID="LinkBox0" runat="server" Text="当前窗口弹出" Url="InIframe.aspx" InWindow="window"></XCL:LinkBox>
        <XCL:LinkBox ID="LinkBox1" runat="server" Text="上一层弹出" Url="InIframe.aspx" InWindow="parent"></XCL:LinkBox>
        <XCL:LinkBox ID="LinkBox2" runat="server" Text="顶级弹出" Url="InIframe.aspx"></XCL:LinkBox>
        
        <hr/>
        <div>
            <div>当前窗口打开,但是刷新其它窗口</div>
        <XCL:LinkBox ID="LinkBox3" runat="server" Text="刷新当前帧" Url="InIframe.aspx" InWindow="window"></XCL:LinkBox>
        <XCL:LinkBox ID="LinkBox4" runat="server" Text="刷新上一层帧" Url="InIframe.aspx" InWindow="window" ParentWindow="parent"></XCL:LinkBox>
        <XCL:LinkBox ID="LinkBox5" runat="server" Text="刷新顶级帧" Url="InIframe.aspx" InWindow="window" ParentWindow="top"></XCL:LinkBox>

        <hr/>
        <XCL:LinkBox ID="LinkBox6" runat="server" Text="默认尽可能顶级打开,只刷新当前帧" Url="InIframe.aspx"></XCL:LinkBox>

        </div>
    </div>
    </form>
</body>
</html>
