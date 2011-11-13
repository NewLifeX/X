<%@ Page Title="模版项管理" Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true" CodeFile="TemplateItemForm.aspx.cs" Inherits="Common_TemplateItemForm"%>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2">模版项</th>
        </tr>
        <tr>
            <td align="right">模版：</td>
            <td><XCL:NumberBox ID="frmTemplateID" runat="server" Width="80px"></XCL:NumberBox></td>
        </tr>
<tr>
            <td align="right">名称：</td>
            <td><asp:TextBox ID="frmName" runat="server" Width="150px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">模版种类：</td>
            <td><asp:TextBox ID="frmKind" runat="server" Width="150px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">备注：</td>
            <td><asp:TextBox ID="frmRemark" runat="server" TextMode="MultiLine" Width="300px" Height="80px"></asp:TextBox></td>
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
</asp:Content>