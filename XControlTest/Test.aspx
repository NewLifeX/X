<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Test.aspx.cs" Inherits="Test" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <%for (int i = 0; i < 10; i++)
      {
          %>序号 <%=i%> <%
      } %>

    <%for (int i = 0; i < 10; i++)
      {
          %>
          序号 <%=i%> <%
      } %>

    <%for (int i = 0; i < 10; i++)
      {
          %>
          序号 <%=i%> 
          <%
      } %>

    </div>
    </form>
</body>
</html>
