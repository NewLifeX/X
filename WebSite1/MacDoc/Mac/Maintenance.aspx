<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Maintenance.aspx.cs" Inherits="Pages_Maintenance"
    Title="维修保养记录" MasterPageFile="~/Admin/MasterPage.master" %>

<asp:Content ContentPlaceHolderID="ContentPlaceHolder1" runat="server" ID="content1">
    <div>
        <div class="toolbar">
            <XCL:LinkBox ID="Button2" runat="server" Url="MaintenanceForm.aspx" IconLeft="~/Admin/images/icons/new.gif"
                BoxWidth="440px" BoxHeight="535px"><b>添加维修保养记录</b></XCL:LinkBox>&nbsp; 客户：<asp:TextBox
                    ID="TextBox1" runat="server"></asp:TextBox>&nbsp; 机器名称：<asp:TextBox ID="txtNo" runat="server"></asp:TextBox>&nbsp;
            <asp:Button ID="Button1" runat="server" Text="查询" />
        </div>
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
            DataSourceID="ObjectDataSource1" AllowPaging="True" AllowSorting="True" CssClass="m_table"
            PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True">
            <Columns>
                <asp:BoundField DataField="ID" HeaderText="ID" SortExpression="ID" InsertVisible="False"
                    ReadOnly="True" />
                <asp:TemplateField HeaderText="客户" SortExpression="CustomerID">
                    <EditItemTemplate>
                        <asp:TextBox ID="TextBox1" runat="server" Text='<%# Bind("CustomerID") %>'></asp:TextBox>
                    </EditItemTemplate>
                    <ItemTemplate>
                        <asp:Label ID="Label1" runat="server" Text='<%# Bind("Customer.Name") %>'></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="机器" SortExpression="MachineID">
                    <EditItemTemplate>
                        <asp:TextBox ID="TextBox2" runat="server" Text='<%# Bind("MachineID") %>'></asp:TextBox>
                    </EditItemTemplate>
                    <ItemTemplate>
                        <asp:Label ID="Label2" runat="server" Text='<%# Bind("Machine.Name") %>'></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Technician" HeaderText="技术员" SortExpression="Technician" />
                <asp:BoundField DataField="Reason" HeaderText="故障原因" SortExpression="Reason" />
                <asp:BoundField DataField="Fittings" HeaderText="更换配件" SortExpression="Fittings" />
                <asp:BoundField DataField="Propose" HeaderText="改进建议" SortExpression="Propose" />
                <asp:BoundField DataField="Remark" HeaderText="维修备注" SortExpression="Remark" />
                <XCL:LinkBoxField DataNavigateUrlFields="ID" DataNavigateUrlFormatString="MaintenanceForm.aspx?ID={0}"
                    Text="编辑" HeaderText="编辑" Height="535px" Width="440px" Title="编辑">
                    <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="60px" />
                </XCL:LinkBoxField>
                <asp:TemplateField ShowHeader="False" HeaderText="删除">
                    <ItemTemplate>
                        <asp:LinkButton ID="LinkButton1" runat="server" CausesValidation="False" CommandName="Delete"
                            OnClientClick='return confirm("确定删除吗？")' Text="删除"></asp:LinkButton>
                    </ItemTemplate>
                    <HeaderStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="30px" />
                    <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="60px" />
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                没有符合条件的数据！
            </EmptyDataTemplate>
        </asp:GridView>
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="NewLife.YWS.Entities.Maintenance"
            DeleteMethod="Delete" EnablePaging="True" OldValuesParameterFormatString="original_{0}"
            SelectCountMethod="SearchCount" SelectMethod="Search" SortParameterName="orderClause"
            TypeName="NewLife.YWS.Entities.Maintenance">
            <SelectParameters>
                <asp:ControlParameter ControlID="TextBox1" Name="name" PropertyName="Text" Type="String" />
                <asp:ControlParameter ControlID="txtNo" Name="machineName" PropertyName="Text" Type="String" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter Name="startRowIndex" Type="Int32" />
                <asp:Parameter Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <XCL:GridViewExtender ID="gvExt" runat="server">
        </XCL:GridViewExtender>
    </div>
</asp:Content>
