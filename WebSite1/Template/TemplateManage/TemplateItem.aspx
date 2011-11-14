<%@ Page Title="模版项管理" Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true" CodeFile="TemplateItem.aspx.cs" Inherits="Common_TemplateItem" %>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <div class="toolbar">
        <XCL:LinkBox ID="lbAdd" runat="server" BoxHeight="274px" BoxWidth="440px" Url="TemplateItemForm.aspx"
            IconLeft="~/Admin/images/icons/new.gif"><b>添加模版项</b></XCL:LinkBox>
        关键字：<asp:TextBox ID="txtKey" runat="server"></asp:TextBox>
        <asp:Button ID="btnSearch" runat="server" Text="查询" />
    </div>
    <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID" DataSourceID="ods" AllowPaging="True" AllowSorting="True" CssClass="m_table" PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True">
        <Columns>
            <%--<asp:TemplateField>
                <ItemTemplate>
                    <asp:CheckBox ID="cb" runat="server" />
                </ItemTemplate>
                <HeaderStyle Width="20px" />
                <ItemStyle HorizontalAlign="Center" />
            </asp:TemplateField>--%>
            <asp:BoundField DataField="ID" HeaderText="编号" SortExpression="ID" InsertVisible="False" ReadOnly="True" >
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" CssClass="key" />
            </asp:BoundField>
            <%--<asp:BoundField DataField="TemplateID" HeaderText="模版" SortExpression="TemplateID" >
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" CssClass="key" />
            </asp:BoundField>--%>
            <asp:BoundField DataField="TemplateName" HeaderText="模版" SortExpression="TemplateID" />
            <asp:BoundField DataField="Name" HeaderText="名称" SortExpression="Name" />
            <asp:BoundField DataField="Kind" HeaderText="模版种类" SortExpression="Kind" />
            <XCL:LinkBoxField HeaderText="编辑" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="TemplateItemForm.aspx?ID={0}" Height="274px" Text="编辑" Width="440px" Title="编辑模版项">
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" />
                <HeaderStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="30px" />
            </XCL:LinkBoxField>
            <asp:TemplateField ShowHeader="False" HeaderText="删除">
                <ItemTemplate>
                    <asp:LinkButton ID="btnDelete" runat="server" CausesValidation="False" CommandName="Delete" OnClientClick='return confirm("确定删除吗？")' Text="删除"></asp:LinkButton>
                </ItemTemplate>
                <HeaderStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="30px" />
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            没有符合条件的数据！
        </EmptyDataTemplate>
    </asp:GridView>
    <asp:ObjectDataSource ID="ods" runat="server" EnablePaging="True" SelectCountMethod="SearchCount" SelectMethod="Search" SortParameterName="orderClause">
        <SelectParameters>
            <asp:ControlParameter ControlID="txtKey" Name="key" PropertyName="Text" Type="String" />
            <asp:Parameter Name="orderClause" Type="String" />
            <asp:Parameter Name="startRowIndex" Type="Int32" />
            <asp:Parameter Name="maximumRows" Type="Int32" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <XCL:GridViewExtender ID="gvExt" runat="server">
    </XCL:GridViewExtender>
</asp:Content>