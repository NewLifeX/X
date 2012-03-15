<%@ Page Language="C#" AutoEventWireup="true" CodeFile="MessageClientTest.aspx.cs"
    Inherits="MessageClientTest" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:Button ID="Button1" runat="server" Text="登录" onclick="Button1_Click" />
        &nbsp;<asp:Button ID="Button2" runat="server" Text="获取管理员" 
            onclick="Button2_Click" />
    </div>
    </form>
</body>
</html>
