<%@ Page Title="手册管理" Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true" CodeFile="ManualForm.aspx.cs" Inherits="Manual_ManualForm"%>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2">手册</th>
        </tr>
        <tr>
            <td align="right">资源：</td>
            <td><asp:TextBox ID="frmUrl" runat="server" Width="200px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">摘要：</td>
            <td><asp:TextBox ID="frmSummary" runat="server" Width="300px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">内容：</td>
            <td><asp:TextBox ID="frmContent" runat="server" TextMode="MultiLine" Width="300px" Height="80px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">作者：</td>
            <td><asp:TextBox ID="frmUserName" runat="server" Width="150px"></asp:TextBox></td>
        </tr>
<tr>
            <td align="right">创建时间：</td>
            <td><XCL:DateTimePicker ID="frmCreateTime" runat="server"></XCL:DateTimePicker></td>
        </tr>
<tr>
            <td align="right">更新时间：</td>
            <td><XCL:DateTimePicker ID="frmUpdateTime" runat="server"></XCL:DateTimePicker></td>
        </tr>
    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' />
                &nbsp;<asp:Button ID="btnCopy" runat="server" CausesValidation="True" Text='另存为新手册' />
                &nbsp;<asp:Button ID="btnReturn" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;" Text="返回" />
            </td>
        </tr>
    </table>
</asp:Content>