<%@ Page Language="C#" MasterPageFile="~/Admin/MasterPage.master" AutoEventWireup="true"
    CodeFile="MenuForm.aspx.cs" Title="菜单管理" Inherits="Pages_MenuForm" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2">
                菜单
            </th>
        </tr>
        <tr>
            <td width="25%" align="right">
                名称：
            </td>
            <td width="65%">
                <asp:TextBox ID="frmName" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="25%" align="right">
                链接：
            </td>
            <td width="65%">
                <asp:TextBox ID="frmUrl" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="25%" align="right">
                上级：
            </td>
            <td width="65%">
                <XCL:DropDownList ID="frmParentID" runat="server" DataTextField="Title" DataValueField="ID"
                    AppendDataBoundItems="True">
                </XCL:DropDownList>
            </td>
        </tr>
        <tr>
            <td width="25%" align="right">
                序号：
            </td>
            <td width="65%">
                <XCL:NumberBox ID="frmSort" runat="server" Width="80px"></XCL:NumberBox>
            </td>
        </tr>
        <tr>
            <td width="25%" align="right">
                权限：
            </td>
            <td width="65%">
                <asp:TextBox ID="frmPermission" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="25%" align="right">
                显示：
            </td>
            <td width="65%">
                <asp:CheckBox ID="frmIsShow" runat="server" Text="是否显示" />
            </td>
        </tr>
        <tr>
            <td width="25%" align="right">
                备注：
            </td>
            <td width="65%">
                <asp:TextBox ID="frmRemark" runat="server"></asp:TextBox>
            </td>
        </tr>
    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="UpdateButton" runat="server" CausesValidation="True" Text="更新" />
                &nbsp;<asp:Button ID="Button2" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;"
                    Text="返回" />
            </td>
        </tr>
    </table>
</asp:Content>
