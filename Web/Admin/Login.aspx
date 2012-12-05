<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Login.aspx.cs" Inherits="Admin_Login" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>管理平台</title>
    <style type="text/css">
        body
        {
            margin-left: 0px;
            margin-top: 0px;
            margin-right: 0px;
            margin-bottom: 0px;
            background-color: #016aa9;
            overflow: hidden;
        }
        .STYLE1
        {
            color: #000000;
            font-size: 12px;
        }
        .login
        {
            background-image: url(images/dl.gif);
            background-repeat: no-repeat;
            width: 49px;
            height: 18px;
            cursor: pointer;
            border: 0px;
        }
        .field-container .verify-code-text,
        .field-container .verify-code-input-container 
        {
            display:none;
        }
        /* <% if(NewLife.Web.ControlHelper.FindControl<XControl.VerifyCodeBox>(Page, "VerifyCodeBox") != null) { %> */
        .field-container
        {
            position:relative;
        }
        .field-container .verify-code-text,
        .field-container .verify-code-input-container
        {
            position:absolute;
            top:1.8em;
        }
        .field-container .verify-code-text
        {
            display: block;
            width:5em;
            left:-1.6em;
        }
        .field-container .verify-code-input-container
        {
            top:1.5em;
            display: block;
            width:200px;
        }
        .field-container .verify-code-input-container .verify-code
        {
            vertical-align:top;
        }
        .field-container .verify-code-input-container .verify-code-box img
        {
            height:28px;
        }
        .field-container .verify-code-input-container .verify-code-box span
        {
            display:block;
        }
        /* <% } %> */
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <table width="100%" height="100%" border="0" cellpadding="0" cellspacing="0">
        <tr>
            <td align="center">
                <table width="962" border="0" align="center" cellpadding="0" cellspacing="0">
                    <tr>
                        <td height="235" background="images/login_03.gif">
                            &nbsp;
                        </td>
                    </tr>
                    <tr>
                        <td height="53">
                            <table width="100%" border="0" cellspacing="0" cellpadding="0">
                                <tr>
                                    <td width="384" height="53" background="images/login_05.gif">
                                        &nbsp;
                                    </td>
                                    <td width="216" background="images/login_06.gif">
                                        <table width="100%" border="0" cellspacing="0" cellpadding="0">
                                            <tr>
                                                <td width="19%" height="25">
                                                    <div align="right">
                                                        <span class="STYLE1">用户：</span></div>
                                                </td>
                                                <td width="57%" height="25">
                                                    <div style="text-align: left;">
                                                        <asp:TextBox ID="UserName" runat="server" Style="width: 105px; height: 17px; background-color: #292929;
                                                            border: solid 1px #7dbad7; font-size: 12px; color: #6cd0ff"></asp:TextBox>
                                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="UserName"
                                                            ErrorMessage="必须填写“用户名”。" ToolTip="必须填写“用户名”。" ValidationGroup="LoginControl">*</asp:RequiredFieldValidator>
                                                    </div>
                                                </td>
                                                <td width="24%" height="25">
                                                    &nbsp;
                                                </td>
                                            </tr>
                                            <tr>
                                                <td height="25">
                                                    <div class="field-container" align="right">
                                                        <span class="STYLE1">密码：</span>
                                                        <span class="STYLE1 verify-code-text">验证码：</span>
                                                    </div>
                                                </td>
                                                <td height="25">
                                                    <div class="field-container" style="text-align: left;">
                                                        <asp:TextBox ID="Password" runat="server" TextMode="Password" Style="width: 105px;
                                                            height: 17px; background-color: #292929; border: solid 1px #7dbad7; font-size: 12px;
                                                            color: #6cd0ff"></asp:TextBox>
                                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ControlToValidate="Password" ErrorMessage="必须填写“密码”。" ToolTip="必须填写“密码”。" ValidationGroup="LoginControl">*</asp:RequiredFieldValidator>
                                                        <div class="verify-code-input-container">
                                                            <asp:TextBox ID="VerifyCode" CssClass="verify-code" runat="server" MaxLength="5" Style="width:3em;height: 17px; background-color: #292929;border: solid 1px #7dbad7; font-size: 12px;color: #6cd0ff"></asp:TextBox>
                                                            <%--<XCL:VerifyCodeBox ID="VerifyCodeBox" CssClass="STYLE1 verify-code-box" runat="server" ControlToValidate="VerifyCode" ValidationGroup="LoginControl"></XCL:VerifyCodeBox>--%>
                                                        </div>
                                                    </div>
                                                </td>
                                                <td height="25">
                                                    <div class="field-container" align="left">
                                                        <asp:Button ID="Button1" runat="server" BackColor="White" BorderColor="#CC9966" BorderWidth="0px"
                                                            CommandName="Login" Font-Size="0.8em" ForeColor="#990000" Text="" CssClass="login"
                                                            ValidationGroup="LoginControl" OnClick="LoginButton_Click" /></div>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                    <td width="362" background="images/login_07.gif">
                                        &nbsp;
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td height="213" background="images/login_08.gif">
                            &nbsp;
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    </form>
</body>
</html>
