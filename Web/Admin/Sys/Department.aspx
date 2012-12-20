<%@ Page Title="部门管理" Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true"
    CodeFile="Department.aspx.cs" Inherits="Common_Department" %>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <div class="toolbar">
        <XCL:LinkBox ID="lbAdd" runat="server" BoxHeight="409px" BoxWidth="440px" Url="DepartmentForm.aspx"
            IconLeft="~/Admin/images/icons/new.gif" EnableViewState="false"><b>添加部门</b></XCL:LinkBox>
        关键字：<asp:TextBox ID="txtKey" runat="server"></asp:TextBox>
        <asp:Button ID="btnSearch" runat="server" Text="查询" />
    </div>
    <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
        DataSourceID="ods" AllowPaging="True" AllowSorting="True" CssClass="m_table"
        PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True" OnRowCommand="gv_RowCommand"
        EnableViewState="False">
        <Columns>
            <asp:BoundField DataField="ID" HeaderText="编号" SortExpression="ID" InsertVisible="False"
                ReadOnly="True">
                <HeaderStyle Width="40px" />
                <ItemStyle CssClass="key" HorizontalAlign="Center" />
            </asp:BoundField>
            <asp:BoundField DataField="TreeNodeName" HeaderText="名称" SortExpression="Name" />
            <asp:BoundField DataField="Code" HeaderText="代码" SortExpression="Code" />
            <asp:BoundField DataField="ParentName" HeaderText="上级" SortExpression="ParentID" />
            <asp:BoundField DataField="Level" HeaderText="等级" SortExpression="Level" DataFormatString="{0:n0}">
                <ItemStyle HorizontalAlign="Right" Font-Bold="True" />
            </asp:BoundField>
            <asp:BoundField DataField="LevelName" HeaderText="等级名称" SortExpression="LevelName">
                <ItemStyle HorizontalAlign="Center" Font-Bold="True" />
            </asp:BoundField>
            <asp:BoundField DataField="Manager" HeaderText="管理者" SortExpression="Manager" />
            <asp:BoundField DataField="Sort" HeaderText="排序" SortExpression="Sort" DataFormatString="{0:n0}">
                <ItemStyle HorizontalAlign="Center" Font-Bold="True" />
            </asp:BoundField>
            <asp:TemplateField HeaderText="升" ShowHeader="False">
                <ItemTemplate>
                    <asp:LinkButton ID="LinkButton2" runat="server" CausesValidation="False" CommandArgument='<%# Eval("ID") %>'
                        CommandName="Up" Text="↑" Font-Size="12pt" ForeColor="Red" Visible='<%# !IsFirst(Container.DataItem) %>'></asp:LinkButton>
                </ItemTemplate>
                <ItemStyle Font-Size="12pt" ForeColor="Red" />
            </asp:TemplateField>
            <asp:TemplateField HeaderText="降" ShowHeader="False">
                <ItemTemplate>
                    <asp:LinkButton ID="LinkButton3" runat="server" CausesValidation="False" CommandArgument='<%# Eval("ID") %>'
                        CommandName="Down" Text="↓" Font-Size="12pt" ForeColor="Green" Visible='<%# !IsLast(Container.DataItem) %>'></asp:LinkButton>
                </ItemTemplate>
                <ItemStyle Font-Size="12pt" ForeColor="Green" />
            </asp:TemplateField>
            <XCL:LinkBoxField HeaderText="添加下级" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="DepartmentForm.aspx?ParentID={0}"
                Height="409px" Width="440px" DataTitleField="NextLevelName" DataTitleFormatString="添加{0}"
                DataTextField="NextLevelName" DataTextFormatString="添加{0}" Text="添加下级部门">
                <ItemStyle HorizontalAlign="Center" />
            </XCL:LinkBoxField>
            <XCL:LinkBoxField HeaderText="编辑" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="DepartmentForm.aspx?ID={0}"
                Height="409px" Text="编辑" Width="440px" DataTitleField="LevelName" DataTitleFormatString="编辑{0}">
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" />
                <HeaderStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="30px" />
            </XCL:LinkBoxField>
            <asp:TemplateField ShowHeader="False" HeaderText="删除">
                <ItemTemplate>
                    <asp:LinkButton ID="btnDelete" runat="server" CausesValidation="False" CommandName="Delete"
                        OnClientClick='return confirm("确定删除吗？")' Text="删除"></asp:LinkButton>
                </ItemTemplate>
                <HeaderStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="30px" />
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            没有符合条件的数据！
        </EmptyDataTemplate>
    </asp:GridView>
    <asp:ObjectDataSource ID="ods" runat="server" SelectMethod="FindAllChildsNoParent"
        EnableViewState="false">
        <SelectParameters>
            <asp:Parameter DefaultValue="0" Name="parentKey" Type="Object" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <XCL:GridViewExtender ID="gvExt" runat="server">
    </XCL:GridViewExtender>
</asp:Content>
