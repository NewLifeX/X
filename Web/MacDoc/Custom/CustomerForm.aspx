<%@ Page Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true"
    CodeFile="CustomerForm.aspx.cs" Inherits="CustomerForm" %>

<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2">
                客户
            </th>
        </tr>
        <tr>
            <td width="18%" align="right">
                类别<font color="red">*</font>：
            </td>
            <td width="75%">
                <XCL:DropDownList ID="typeList" runat="server" DataTextField="Name" DataValueField="ID"
                    Rows="5" Width="200px"></XCL:DropDownList>
            </td>
        </tr>
        <tr>
            <td width="18%" align="right">
                客户地址<font color="red">*</font>：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmAddress" runat="server" Text='<%# Entity.Address %>' Width="200px"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="18%" align="right">
                客户编号<font color="red">*</font>：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmNo" runat="server" Text='<%# Entity.No %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="18%" align="right">
                名称<font color="red">*</font>：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmName" runat="server" Text='<%# Entity.Name %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="18%" align="right">
                联系人：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmLinkman" runat="server" Text='<%# Entity.Linkman %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="18%" align="right">
                部门：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmDepartment" runat="server" Text='<%# Entity.Department %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="18%" align="right">
                电话：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmTel" runat="server" Text='<%# Entity.Tel %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="18%" align="right">
                传真：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmFax" runat="server" Text='<%# Entity.Fax %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="18%" align="right">
                邮箱：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmEmail" runat="server" Text='<%# Entity.Email %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="18%" align="right">
                QQ：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmQQ" runat="server" Text='<%# Entity.QQ %>'></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td width="18%" align="right">
                MSN：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmMSN" runat="server" Text='<%# Entity.MSN %>'></asp:TextBox>
            </td>
        </tr>
        <%--<tr>
            <td width="18%" align="right">
                添加时间：
            </td>
            <td width="75%">
                <XCL:DateTimePicker ID="frmAddTime" runat="server" Text='<%# Entity.AddTime %>'></XCL:DateTimePicker>
            </td>
        </tr>--%>
        <tr>
            <td width="18%" align="right">
                备注：
            </td>
            <td width="75%">
                <asp:TextBox ID="frmRemark" runat="server" Text='<%# Entity.Remark %>' TextMode="MultiLine"
                    Width="200Px" Height="60"></asp:TextBox>
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
</asp:Content>
