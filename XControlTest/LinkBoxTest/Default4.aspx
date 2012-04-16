<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default4.aspx.cs" Inherits="Default4" %>

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
        <iframe src="Default3.aspx" height="600" width="800"></iframe>
    </div>
    </form>
</body>
</html>
