<%@ Page Title="设置管理" Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true" CodeFile="SettingForm.aspx.cs" Inherits="Common_SettingForm"%>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2">设置</th>
        </tr>
        <tr>
            <td align="right">父编号：</td>
            <td><XCL:NumberBox ID="frmParentID" runat="server" Width="80px"></XCL:NumberBox></td>
        </tr>
<tr>
            <td align="right">名称：</td>
            <td><asp:TextBox ID="frmName" runat="server" Width="150px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">值类型：</td>
            <td><XCL:NumberBox ID="frmKind" runat="server" Width="80px"></XCL:NumberBox></td>
        </tr>
<tr>
            <td align="right">值：</td>
            <td><asp:TextBox ID="frmValue" runat="server" TextMode="MultiLine" Width="300px" Height="80px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">显示名：</td>
            <td><asp:TextBox ID="frmDisplayName" runat="server" Width="150px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">排序：</td>
            <td><XCL:NumberBox ID="frmSort" runat="server" Width="80px"></XCL:NumberBox></td>
        </tr>
    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' />
                &nbsp;<asp:Button ID="btnCopy" runat="server" CausesValidation="True" Text='另存为新设置' />
                &nbsp;<asp:Button ID="btnReturn" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;" Text="返回" />
            </td>
        </tr>
    </table>
</asp:Content>