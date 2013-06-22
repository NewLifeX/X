<%@ Page Title="用户管理" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="UserForm.aspx.cs" Inherits="Common_UserForm"%>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2">用户</th>
        </tr>
        <tr>
            <td align="right">账号：</td>
            <td><asp:TextBox ID="frmAccount" runat="server" Width="150px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">密码：</td>
            <td><asp:TextBox ID="frmPassword" runat="server" TextMode="Password"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">是否管理员：</td>
            <td><asp:CheckBox ID="frmIsAdmin" runat="server" Text="是否管理员" /></td>
        </tr>
<tr>
            <td align="right">是否启用：</td>
            <td><asp:CheckBox ID="frmIsEnable" runat="server" Text="是否启用" /></td>
        </tr>
    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' />
                &nbsp;<asp:Button ID="btnCopy" runat="server" CausesValidation="True" Text='另存为新用户' />
                &nbsp;<asp:Button ID="btnReturn" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;" Text="返回" />
            </td>
        </tr>
    </table>
</asp:Content>