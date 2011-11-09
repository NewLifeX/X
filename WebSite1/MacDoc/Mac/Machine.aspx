<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Machine.aspx.cs" Inherits="Pages_Machine"
    Title="机器零件规格" MasterPageFile="~/Admin/MasterPage.master" %>

<asp:Content ID="content1" runat="server" ContentPlaceHolderID="ContentPlaceHolder1">
    <div>
        <div class="toolbar">
            <XCL:LinkBox ID="Button2" runat="server" Url="MachineForm.aspx" IconLeft="~/Admin/images/icons/new.gif"
                BoxWidth="720px" BoxHeight="500px"><b>添加机器零件规格</b></XCL:LinkBox>
            名称：<asp:TextBox ID="txtName" runat="server"></asp:TextBox>&nbsp;客户：<asp:TextBox ID="txtcustomer"
                runat="server"></asp:TextBox>&nbsp;
            <asp:Button ID="Button1" runat="server" Text="查询" />
        </div>
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
            DataSourceID="ObjectDataSource1" AllowPaging="True" AllowSorting="True" CssClass="m_table"
            PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True">
            <Columns>
                <asp:BoundField DataField="ID" HeaderText="ID" SortExpression="ID" InsertVisible="False"
                    ReadOnly="True" />
                <%-- <asp:BoundField DataField="Name" HeaderText="名称" SortExpression="Name" />--%>
                <XCL:LinkBoxField DataNavigateUrlFields="ID" DataNavigateUrlFormatString="MachineForm.aspx?ID={0}"
                    HeaderText="名称" Height="500px" Width="720px" DataTextField="Name" />
                <asp:TemplateField HeaderText="客户" SortExpression="CustomerID">
                    <EditItemTemplate>
                        <asp:TextBox ID="TextBox1" runat="server" Text='<%# Bind("CustomerID") %>'></asp:TextBox>
                    </EditItemTemplate>
                    <ItemTemplate>
                        <asp:Label ID="Label1" runat="server" Text='<%# Bind("Customer.Name") %>'></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Transactor" HeaderText="经手人" SortExpression="Transactor" />
                <asp:BoundField DataField="Type" HeaderText="点胶阀门类型" SortExpression="Type" />
                <asp:BoundField DataField="Model" HeaderText="混合管型号" SortExpression="Model" />
                <asp:BoundField DataField="OutlineSize" HeaderText="机器外形尺寸" SortExpression="OutlineSize" />
                <asp:BoundField DataField="Kind" HeaderText="数据显示屏种类" SortExpression="Kind" />
                <asp:BoundField DataField="LeaveTime" HeaderText="出厂日期" SortExpression="LeaveTime" />
                <XCL:LinkBoxField DataNavigateUrlFields="ID" DataNavigateUrlFormatString="MachineForm.aspx?ID={0}"
                    Text="编辑" HeaderText="编辑" Height="500px" Width="720px" Title="编辑">
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
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="NewLife.YWS.Entities.Machine"
            DeleteMethod="Delete" EnablePaging="True" OldValuesParameterFormatString="original_{0}"
            SelectCountMethod="SearchCount" SelectMethod="Search" SortParameterName="orderClause"
            TypeName="NewLife.YWS.Entities.Machine">
            <SelectParameters>
                <asp:ControlParameter ControlID="txtName" Name="name" PropertyName="Text" Type="String" />
                <asp:Parameter Name="groups" Type="String" />
                <asp:ControlParameter ControlID="txtcustomer" Name="customer" PropertyName="Text"
                    Type="String" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter Name="startRowIndex" Type="Int32" />
                <asp:Parameter Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <XCL:GridViewExtender ID="gvExt" runat="server">
        </XCL:GridViewExtender>
    </div>
</asp:Content>
