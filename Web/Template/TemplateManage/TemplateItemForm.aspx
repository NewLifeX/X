<%@ Page Title="模版项管理" Language="C#" MasterPageFile="~/Admin/ManagerPage.master"
    AutoEventWireup="true" CodeFile="TemplateItemForm.aspx.cs" Inherits="Common_TemplateItemForm"
    ValidateRequest="false" %>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <td style="width: 50%;">
                模版：
                <asp:Label ID="frmTemplateName" runat="server" />
            </td>
            <td>
                模版项名称：
                <asp:TextBox ID="frmName" runat="server" Width="150px"></asp:TextBox>
                &nbsp;版本：<asp:Label ID="frmVersion" runat="server" Font-Bold="true" ForeColor="Red" />
            </td>
        </tr>
        <%--<tr>
            <td align="right">
                模版种类：
            </td>
            <td>
                <asp:TextBox ID="frmKind" runat="server" Width="150px"></asp:TextBox>
            </td>
        </tr>--%>
        <tr>
            <td colspan="2">
                <asp:TextBox ID="frmContent" runat="server" TextMode="MultiLine" Width="780px" Height="480px"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td align="right">
                备注：
                <asp:TextBox ID="frmRemark" runat="server" TextMode="MultiLine" Width="300px" Height="80px"></asp:TextBox>
            </td>
            <td>
            </td>
        </tr>
    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' />
                &nbsp;<asp:Button ID="btnReturn" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;"
                    Text="返回" />
            </td>
        </tr>
    </table>
</asp:Content>
