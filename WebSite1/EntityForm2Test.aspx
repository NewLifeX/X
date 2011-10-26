<%@ Page Language="C#" AutoEventWireup="true" CodeFile="EntityForm2Test.aspx.cs"
    Inherits="EntityForm2Test" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        ID:<asp:Label ID="frmID" runat="server"></asp:Label>

        <br />
        Name:<asp:TextBox ID="frmTextBox" runat="server"></asp:TextBox>

        <br />
        DisplayName:<asp:TextBox ID="frmDisplayName" runat="server"></asp:TextBox>

        <br />
        Phone:<asp:TextBox ID="frmPhone" runat="server"></asp:TextBox>
        <br />
        QQ:
        <XCL:NumberBox ID="frmQQ" runat="server" Width="165px"></XCL:NumberBox>
<%--        QQ:<asp:TextBox ID="frmQQ" runat="server"></asp:TextBox>
--%>        <br />
        MSN:<XCL:MailBox ID="frmMSN" runat="server" Width="165px"></XCL:MailBox>
        <br />
        Email:<XCL:MailBox ID="frmEmail" runat="server" Width="165px"></XCL:MailBox>
        <br />
        Logins:
        <asp:TextBox ID="frmLogins" runat="server"></asp:TextBox>
        <br />
        LastLogin:<XCL:DateTimePicker ID="frmLastLogin" runat="server"></XCL:DateTimePicker>
        <br />
        IsEnable:<asp:CheckBox ID="frmIsEnable" runat="server" />
        <br />
        <asp:Button ID="btnSave" runat="server" Text="保存" />
    </div>
    </form>
</body>
</html>
