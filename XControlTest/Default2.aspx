<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default2.aspx.cs" Inherits="Default2" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:TextBox runat="server" ID="TextBox1" Text=""></asp:TextBox>
        <%--<asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="TextBox1" ErrorMessage="请填写"></asp:RequiredFieldValidator>--%>
        <%--<asp:RangeValidator ID="RangeValidator1" runat="server" ErrorMessage="错误消息" Text="" ControlToValidate="TextBox1" MaximumValue="300" MinimumValue="90" Type="Integer"></asp:RangeValidator>--%>
        <XCL:VerifyCodeBox runat="server" ID="VerifyCodeBox1" ControlToValidate="TextBox1" ErrorMessage="请输入验证码" Text="" Display="Dynamic"></XCL:VerifyCodeBox>
        <asp:Button runat="server" ID="Button1" Text="提交"/>
    </div>
    </form>
</body>
</html>
