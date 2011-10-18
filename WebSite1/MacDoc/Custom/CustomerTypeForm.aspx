
<%@ Page Language="C#" MasterPageFile="~/Admin/MasterPage.master" AutoEventWireup="true" CodeFile="CustomerTypeForm.aspx.cs" Inherits="CustomerTypeForm" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2">客户类型</th>
        </tr>
        <tr>
            <td width="15%" align="right">名称：</td>
            <td width="75%"><asp:TextBox ID="frmName" runat="server" Text='<%# Entity.Name %>'></asp:TextBox></td>
        </tr>
<tr>
            <td width="25%" align="right">上级：</td>
            <td width="65%"><XCL:DropDownList ID="frmParentID" runat="server" 
                    DataTextField="Name" DataValueField="ID" AppendDataBoundItems="True">
                </XCL:DropDownList></td>
        </tr>
<%--<tr>
            <td width="15%" align="right">添加时间：</td>
            <td width="75%"><XCL:DateTimePicker ID="frmAddTime" runat="server" Text='<%# Entity.AddTime %>'></XCL:DateTimePicker></td>
        </tr>
<tr>
            <td width="15%" align="right">添加人：</td>
            <td width="75%"><asp:TextBox ID="frmOperator" runat="server" Text='<%# Entity.Operator %>'></asp:TextBox></td>
        </tr>--%>
    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="UpdateButton" runat="server" CausesValidation="True" Text='<%# EntityID>0?"更新":"新增" %>' OnClick="UpdateButton_Click" />
                &nbsp;<asp:Button ID="Button2" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;" Text="返回" />
            </td>
        </tr>
    </table>
</asp:Content>