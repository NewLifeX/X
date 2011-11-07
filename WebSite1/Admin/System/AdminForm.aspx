<%@ Page Language="C#" MasterPageFile="~/Admin/Main.master" AutoEventWireup="true"
    CodeFile="AdminForm.aspx.cs" Inherits="Pages_AdminForm" Title="管理员管理" %>

<asp:Content ID="C" ContentPlaceHolderID="C" runat="server">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2">
                系统管理员
            </th>
        </tr>
        <tr>
            <td align="right">
                名称：
            </td>
            <td>
                <asp:TextBox ID="frmName" runat="server" Text='<%# Entity.Name %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                密码：
            </td>
            <td>
                <asp:TextBox ID="frmPassword" runat="server" Text='' TextMode="Password"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                显示名：
            </td>
            <td>
                <asp:TextBox ID="frmDisplayName" runat="server" Text='<%# Entity.DisplayName %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                角色：
            </td>
            <td>
                <XCL:DropDownList ID="frmRoleID" runat="server" DataSourceID="ObjectDataSource1"
                    DataTextField="Name" DataValueField="ID" AppendDataBoundItems="true">
                    <asp:ListItem Value="0">请选择</asp:ListItem>
                </XCL:DropDownList>
            </td>
        </tr>
        <%-- <tr>
            <td align="right">
                登录次数：
            </td>
            <td>
                <XCL:NumberBox ID="frmLogins" runat="server" Text='<%# Entity.Logins %>' 
                    Width="80px" ReadOnly="True"></XCL:NumberBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                最后登录：
            </td>
            <td>
                <XCL:DateTimePicker ID="frmLastLogin" runat="server" 
                    Text='<%# Entity.LastLogin %>' ReadOnly="True"></XCL:DateTimePicker>
            </td>
        </tr>
        <tr>
            <td align="right">
                最后登陆IP：
            </td>
            <td>
                <XCL:IPBox ID="frmLastLoginIP" runat="server" Text='<%# Entity.LastLoginIP %>' 
                    ReadOnly="True"></XCL:IPBox>
            </td>
        </tr>--%>
        <tr>
            <td align="right">
                是否使用：
            </td>
            <td>
                <asp:CheckBox ID="frmIsEnable" runat="server" Text="是否使用" Checked='<%# Entity.IsEnable %>' />
            </td>
        </tr>
        <tr>
            <td align="right">
                QQ：
            </td>
            <td>
                <asp:TextBox ID="frmQQ" runat="server" Text='<%# Entity["QQ"] %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                MSN：
            </td>
            <td>
                <asp:TextBox ID="frmMSN" runat="server" Text='<%# Entity["MSN"] %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                邮箱：
            </td>
            <td>
                <XCL:MailBox ID="frmEmail" runat="server" Text='<%# Entity["Email"] %>'></XCL:MailBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                电话：
            </td>
            <td>
                <asp:TextBox ID="frmPhone" runat="server" Text='<%# Entity["Phone"] %>'></asp:TextBox>
            </td>
        </tr>
    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="UpdateButton" runat="server" CausesValidation="True" Text='<%# EntityID>0?"更新":"新增" %>'
                    OnClick="UpdateButton_Click" />
                &nbsp;<asp:Button ID="Button2" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;"
                    Text="返回" />
            </td>
        </tr>
    </table>
    <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" TypeName="NewLife.CommonEntity.Role"
        DataObjectTypeName="NewLife.CommonEntity.Role " SelectMethod="FindAll"></asp:ObjectDataSource>
</asp:Content>
