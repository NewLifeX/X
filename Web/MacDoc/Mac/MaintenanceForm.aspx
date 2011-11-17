<%@ Page Language="C#" AutoEventWireup="true" CodeFile="MaintenanceForm.aspx.cs"
    Title="维修保养记录" MasterPageFile="~/Admin/ManagerPage.master" Inherits="Pages_MaintenanceForm"
    ValidateRequest="false" %>

<asp:Content ID="content1" runat="server" ContentPlaceHolderID="C">
    <div>
        <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
            <tr>
                <th colspan="2">
                    维修保养
                </th>
            </tr>
            <tr>
                <td align="right">
                    <font color="red">*</font>客户：
                </td>
                <td>
                    <XCL:ChooseButton ID="customerList" Text='<%# Entity.Customer!=null?(Entity.Customer.No+" "+Entity.Customer.Name):"请选择客户" %>'
                        Value='<%# Entity.CustomerID %>' Url="SelectCustomer.aspx?ID={value}&Name={text}"
                        ModalDialogOptions="dialogWidth:'750px',dialogHeight:'550px'" runat="server" />
                    <%-- <XCL:DropDownList ID="customerList" runat="server" DataSourceID="odsCustomer" DataTextField="Name"
                        DataValueField="ID" Rows="5" Width="300px" AppendDataBoundItems="true" SelectedValue='<%# Entity.CustomerID %>'>
                        <asp:ListItem Value="0">请选择</asp:ListItem>
                    </XCL:DropDownList>
                    <asp:ObjectDataSource ID="odsCustomer" runat="server" DataObjectTypeName="NewLife.YWS.Entities.Customer"
                        DeleteMethod="Delete" OldValuesParameterFormatString="original_{0}" SelectMethod="FindAllByName"
                        TypeName="NewLife.YWS.Entities.Customer">
                        <SelectParameters>
                            <asp:Parameter Name="name" Type="String" />
                            <asp:Parameter Name="value" Type="Object" />
                            <asp:Parameter Name="orderClause" Type="String" />
                            <asp:Parameter Name="startRowIndex" Type="Int32" />
                            <asp:Parameter Name="maximumRows" Type="Int32" />
                        </SelectParameters>
                    </asp:ObjectDataSource>--%>
                </td>
            </tr>
            <tr>
                <td align="right">
                    <font color="red">*</font>机器：
                </td>
                <td>
                    <%--  <XCL:DropDownList ID="ListBox1" runat="server" DataSourceID="odsMachine" DataTextField="Name"
                        DataValueField="ID" Rows="5" Width="300px" AppendDataBoundItems="true" SelectedValue='<%# Entity.MachineID %>'>
                        <asp:ListItem Value="0">请选择</asp:ListItem>
                    </XCL:DropDownList>
                    <asp:ObjectDataSource ID="odsMachine" runat="server" DataObjectTypeName="NewLife.YWS.Entities.Machine"
                        DeleteMethod="Delete" OldValuesParameterFormatString="original_{0}" SelectMethod="FindAllByName"
                        TypeName="NewLife.YWS.Entities.Machine">
                        <SelectParameters>
                            <asp:Parameter Name="name" Type="String" />
                            <asp:Parameter Name="value" Type="Object" />
                            <asp:Parameter Name="orderClause" Type="String" />
                            <asp:Parameter Name="startRowIndex" Type="Int32" />
                            <asp:Parameter Name="maximumRows" Type="Int32" />
                        </SelectParameters>
                    </asp:ObjectDataSource>--%>
                    <XCL:ChooseButton ID="ChooseButton1" Text='<%# Entity.MachineID>0?(" "+Entity.MachineID):"请选择机器" %>'
                        Value='<%# Entity.MachineID %>' Url="SelectMachine.aspx?ID={value}&Name={text}"
                        ModalDialogOptions="dialogWidth:'750px',dialogHeight:'550px'" runat="server" />
                </td>
            </tr>
            <tr>
                <td align="right">
                    技术员：
                </td>
                <td>
                    <asp:TextBox ID="frmTechnician" runat="server" Text='<%#Entity.Technician%>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    故障原因：
                </td>
                <td>
                    <asp:TextBox ID="frmReason" runat="server" Text='<%# Entity.Reason %>' TextMode="MultiLine"
                        Width="300px" Height="60px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    更换配件：
                </td>
                <td>
                    <asp:TextBox ID="frmFittings" runat="server" Text='<%# Entity.Fittings %>' Width="300px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    改进建议：
                </td>
                <td>
                    <asp:TextBox ID="frmPropose" runat="server" Text='<%# Entity.Propose %>' TextMode="MultiLine"
                        Width="300px" Height="60px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    维修备注：
                </td>
                <td>
                    <asp:TextBox ID="frmRemark" runat="server" Text='<%# Entity.Remark %>' TextMode="MultiLine"
                        Width="300px" Height="60"></asp:TextBox>
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
