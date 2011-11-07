<%@ Page Language="C#" MasterPageFile="~/Admin/Main.master" AutoEventWireup="true"
    CodeFile="AdminInfo.aspx.cs" Inherits="Pages_AdminInfo" Title="用户信息" %>

<asp:Content ID="C" ContentPlaceHolderID="C" runat="server">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2">
                用户信息
            </th>
        </tr>
        <tr>
            <td align="right">
                名称：
            </td>
            <td>
                <asp:TextBox ID="frmName" runat="server" ReadOnly="true"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                密码：
            </td>
            <td>
                <asp:TextBox ID="frmPassword" runat="server" TextMode="Password"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                显示名：
            </td>
            <td>
                <asp:TextBox ID="frmDisplayName" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                角色：
            </td>
            <td>
                <asp:Label ID="frmRoleName" runat="server"></asp:Label>
            </td>
        </tr>
        <tr>
            <td align="right">
                登录次数：
            </td>
            <td>
                <asp:Label ID="frmLogins" runat="server"></asp:Label>
            </td>
        </tr>
        <tr>
            <td align="right">
                最后登录：
            </td>
            <td>
                <asp:Label ID="frmLastLogin" runat="server"></asp:Label>
            </td>
        </tr>
        <tr>
            <td align="right">
                最后登陆IP：
            </td>
            <td>
                <asp:Label ID="frmLastLoginIP" runat="server"></asp:Label>
            </td>
        </tr>
        <tr>
            <td align="right">
                QQ：
            </td>
            <td>
                <asp:TextBox ID="frmQQ" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                MSN：
            </td>
            <td>
                <asp:TextBox ID="frmMSN" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                邮箱：
            </td>
            <td>
                <XCL:MailBox ID="frmEmail" runat="server"></XCL:MailBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                电话：
            </td>
            <td>
                <asp:TextBox ID="frmPhone" runat="server"></asp:TextBox>
            </td>
        </tr>
    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' />
            </td>
        </tr>
    </table>
    <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" TypeName="NewLife.CommonEntity.Role"
        DataObjectTypeName="NewLife.CommonEntity.Role " SelectMethod="FindAll"></asp:ObjectDataSource>
</asp:Content>
