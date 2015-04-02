<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage" %>

<!DOCTYPE html>

<script runat="server">

</script>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            我是大石头<%= DateTime.Now.ToString() %>
            <br />
            数据：
            <% foreach (var item in RouteData.Values)
               {%>
            <li><%=item.Key%>=<%=item.Value%></li>

            <% }%>
        </div>
    </form>
</body>
</html>
