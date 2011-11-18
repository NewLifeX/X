<%@ Page Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true"
    CodeFile="AdminForm.aspx.cs" Inherits="Pages_AdminForm" Title="管理员管理" %>

<asp:Content ID="C" ContentPlaceHolderID="C" runat="server">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2">管理员</th>
        </tr>
        <tr>
            <td align="right">名称：</td>
            <td><asp:TextBox ID="frmName" runat="server" Width="150px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">密码：</td>
            <td><asp:TextBox ID="frmPassword" runat="server" TextMode="Password"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">显示名：</td>
            <td><asp:TextBox ID="frmDisplayName" runat="server" Width="150px"></asp:TextBox></td>
        </tr>
        <tr>
            <td align="right">
                角色：
            </td>
            <td>
                <XCL:DropDownList ID="frmRoleID" runat="server" DataSourceID="ods"
                    DataTextField="Name" DataValueField="ID" AppendDataBoundItems="true">
                    <asp:ListItem Value="0">请选择</asp:ListItem>
                </XCL:DropDownList>
            </td>
        </tr>
<tr>
            <td align="right">是否使用：</td>
            <td><asp:CheckBox ID="frmIsEnable" runat="server" Text="是否使用" /></td>
        </tr>
        <tr>
            <td align="right">
                QQ：
            </td>
            <td>
                <asp:TextBox ID="frmQQ" runat="server" ></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                MSN：
            </td>
            <td>
                <asp:TextBox ID="frmMSN" runat="server" ></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                邮箱：
            </td>
            <td>
                <XCL:MailBox ID="frmEmail" runat="server" ></XCL:MailBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                电话：
            </td>
            <td>
                <asp:TextBox ID="frmPhone" runat="server" ></asp:TextBox>
            </td>
        </tr>
    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' />
                &nbsp;<asp:Button ID="btnReturn" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;" Text="返回" />
            </td>
        </tr>
    </table>
    <asp:ObjectDataSource ID="ods" runat="server" SelectMethod="FindAllWithCache"></asp:ObjectDataSource>
</asp:Content>