<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Customer.aspx.cs" Inherits="MacDoc_Customer"
    MasterPageFile="~/Admin/ManagerPage.master" Title="客户管理" %>

<asp:Content ID="content1" runat="server" ContentPlaceHolderID="C">
    <div>
        <div class="toolbar">
            <XCL:LinkBox ID="Button2" runat="server" Url="CustomerForm.aspx" IconLeft="~/Admin/images/icons/new.gif"
                BoxWidth="440px" BoxHeight="530px"><b>添加客户</b></XCL:LinkBox>
            关键字：<asp:TextBox ID="txtName" runat="server"></asp:TextBox>
            <asp:Button ID="Button1" runat="server" Text="查询" />
        </div>
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
            DataSourceID="ObjectDataSource1" AllowPaging="True" AllowSorting="True" CssClass="m_table"
            PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True">
            <Columns>
                <asp:BoundField DataField="ID" HeaderText="ID" SortExpression="ID" InsertVisible="False"
                    ReadOnly="True" />
                <asp:BoundField DataField="No" HeaderText="客户编号" SortExpression="No" />
                <XCL:LinkBoxField DataNavigateUrlFields="ID" DataNavigateUrlFormatString="CustomerForm.aspx?ID={0}"
                    DataTextField="Name" HeaderText="名称" Height="530px" Width="440px" />
                <asp:TemplateField HeaderText="客户类型" SortExpression="TypeID">
                    <ItemTemplate>
                        <asp:Label ID="Label1" runat="server" Text='<%# Bind("CustomerType.Name") %>'></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Linkman" HeaderText="联系人" SortExpression="Linkman" />
                <asp:BoundField DataField="Department" HeaderText="部门" SortExpression="Department" />
                <asp:BoundField DataField="Tel" HeaderText="电话" SortExpression="Tel" />
                <XCL:LinkBoxField DataNavigateUrlFields="ID" DataNavigateUrlFormatString="CustomerForm.aspx?ID={0}"
                    Text="编辑" HeaderText="编辑" Height="530px" Width="440px" Title="编辑">
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
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="NewLife.YWS.Entities.Customer"
            DeleteMethod="Delete" EnablePaging="True" OldValuesParameterFormatString="original_{0}"
            SelectCountMethod="SearchCount" SelectMethod="Search" SortParameterName="orderClause"
            TypeName="NewLife.YWS.Entities.Customer">
            <SelectParameters>
                <asp:ControlParameter ControlID="txtName" Name="key" PropertyName="Text" Type="String" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter Name="startRowIndex" Type="Int32" />
                <asp:Parameter Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <XCL:GridViewExtender ID="gvExt" runat="server">
        </XCL:GridViewExtender>
    </div>
</asp:Content>
