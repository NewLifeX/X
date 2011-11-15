<%@ Page Language="C#" AutoEventWireup="true" CodeFile="RecordForm.aspx.cs" Inherits="Admin_Center_RecordForm" MasterPageFile="~/Admin/MasterPage.master" Title="交易记录" %>

<asp:Content ID="content1" runat="server" ContentPlaceHolderID="ContentPlaceHolder1">
    <div>
       <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
            <tr>
                <th colspan="4">
                    交易记录
                </th>
            </tr>
            <tr>
                <td align="right">
                    客户<font color="red">*</font>：
                </td>
                <td>
                    <XCL:ChooseButton ID="customerList" Text='<%# Entity.Customer!=null?(Entity.Customer.No+" "+Entity.Customer.Name):"请选择客户" %>'
                        Value='<%# Entity.CustomerID %>' Url="SelectCustomer.aspx?ID={value}&Name={text}"
                        ModalDialogOptions="dialogWidth:'750px',dialogHeight:'550px'" runat="server" />
                </td>
                <td align="right">
                    机器<font color="red">*</font>：
                </td>
                <td>
                    <XCL:ChooseButton ID="ChooseButton1" Text='<%# Entity.MachineID>0?(" "+Entity.MachineID):"请选择机器" %>'
                        Value='<%# Entity.MachineID %>' Url="SelectMachine.aspx?ID={value}&Name={text}"
                        ModalDialogOptions="dialogWidth:'750px',dialogHeight:'550px'" runat="server" />
                </td>
            </tr>
            <tr>
                <td align="right">
                    &nbsp;经手人<font color="red">*</font>：
                </td>
                <td>
                    <asp:TextBox ID="frmTransactor" runat="server" Text='<%# Entity.Transactor%>' ToolTip="请输入经手人！"></asp:TextBox>
                </td>
                <td align="right">
                    出厂日期<font color="red">*</font>：
                </td>
                <td>
                    <XCL:DateTimePicker ID="frmLeaveTime" runat="server" Text='<%# Entity.LeaveTime%>'
                        ToolTip="请输入出厂日期！"></XCL:DateTimePicker>
                </td>
            </tr>
            <tr>
                <td align="right">
                    备注：
                </td>
                <td>
                    <asp:TextBox ID="frmRemark" runat="server" Text='<%# Entity.Remark %>' TextMode="MultiLine"
                        Width="150px" Height="80px"></asp:TextBox>
                </td>
                <td align="right">
                    附送配件：
                </td>
                <td>
                    <asp:TextBox ID="frmAttachment" runat="server" Text='<%# Entity.Attachment %>' TextMode="MultiLine"
                        Width="150px" Height="80px"></asp:TextBox>
                </td>
            </tr>
        </table>
        <table border="0" align="Center" width="100%">
            <tr>
                <td align="center">
                    <asp:Button ID="UpdateButton" runat="server" CausesValidation="True" Text='<%# EntityID>0?"更新":"新增" %>'
                        OnClick="UpdateButton_Click" />&nbsp;
                    <asp:Button ID="Button2" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;"
                        Text="返回" />
                </td>
            </tr>
        </table>
    </div>
</asp:Content>
