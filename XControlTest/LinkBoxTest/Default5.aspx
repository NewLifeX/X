<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default5.aspx.cs" Inherits="Default5" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body style="margin:3em;">
    <form id="form1" runat="server">
    <div>
        <div style="background-color:<%=RandomColor() %>"><%=DateTime.Now.ToLongTimeString() %></div>

        <hr/>
        <XCL:BoxControl ID="BoxControl1" runat="server"></XCL:BoxControl>
        <iframe width="100%" height="800" src="default4.aspx"></iframe>
    </div>
    </form>
</body>
</html>
