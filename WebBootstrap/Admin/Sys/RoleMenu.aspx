<%@ Page Title="角色和菜单管理" Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true"
    CodeFile="RoleMenu.aspx.cs" Inherits="Common_RoleMenu" %>

<asp:Content ID="h" runat="server" ContentPlaceHolderID="H">
    <link rel="stylesheet" href="<%= ResolveUrl("~/UI/css/uniform.css")%>" type="text/css" />
    <script src="<%= ResolveUrl("~/UI/js/jquery.uniform.js")%>" type="text/javascript"></script>
    <script src="<%= ResolveUrl("~/UI/js/unicorn.form_common.js")%>" type="text/javascript"></script>
</asp:Content>
<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <div class="row-fluid">
        &nbsp;角色：<asp:DropDownList ID="ddlRole" runat="server" DataSourceID="odsRole" DataTextField="Name"
            DataValueField="ID" OnSelectedIndexChanged="ddlRole_SelectedIndexChanged" AutoPostBack="True">
        </asp:DropDownList>
        &nbsp;大类：<asp:DropDownList ID="ddlCategory" runat="server" DataTextField="Name" DataValueField="ID"
            AutoPostBack="True" AppendDataBoundItems="True">
            <asp:ListItem Value="0">全部</asp:ListItem>
        </asp:DropDownList>
    </div>
    <div class="row-fluid">
        <div class="widget-box">
            <div class="widget-content nopadding">
                <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
                    DataSourceID="ods" AllowPaging="True" AllowSorting="True" CssClass="table table-bordered table-striped"
                    PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True" OnRowDataBound="gv_RowDataBound"
                    EnableViewState="False">
                    <Columns>
                        <%--<asp:TemplateField>
                <ItemTemplate>
                    <asp:CheckBox ID="cb" runat="server" />
                </ItemTemplate>
                <HeaderStyle Width="20px" />
                <ItemStyle HorizontalAlign="Center" />
            </asp:TemplateField>--%>
                        <asp:BoundField DataField="ID" HeaderText="编号" InsertVisible="False" ReadOnly="True"
                            SortExpression="ID">
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" Width="40px"/>
                            <ItemStyle CssClass="key" HorizontalAlign="Center" />
                        </asp:BoundField>
                        <asp:TemplateField HeaderText="权限名称" SortExpression="Permission">
                            <ItemTemplate>
                                <%# new String('　', (Convert.ToInt32(Eval("Deepth"))-1)*2)%><asp:Label ID="Label1"
                                    runat="server" Text='<%# Eval("Permission") %>'></asp:Label>
                            </ItemTemplate>
                            <ItemStyle Width="200px" />
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="授权">
                            <ItemTemplate>
                                <asp:CheckBox ID="CheckBox1" runat="server" AutoPostBack="True" BorderWidth="0px"
                                    OnCheckedChanged="CheckBox1_CheckedChanged" />
                            </ItemTemplate>
                            <ItemStyle Width="40px" />
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="操作权限">
                            <ItemTemplate>
                                <asp:CheckBoxList ID="CheckBoxList1" runat="server" AutoPostBack="True" OnSelectedIndexChanged="CheckBoxList1_SelectedIndexChanged"
                                    RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="inline">
                                </asp:CheckBoxList>
                            </ItemTemplate>
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        没有符合条件的数据！
                    </EmptyDataTemplate>
                </asp:GridView>
            </div>
        </div>
    </div>
    <asp:ObjectDataSource ID="ods" runat="server" SelectMethod="FindAllChildsNoParent">
        <SelectParameters>
            <%--<asp:Parameter DefaultValue="0" Name="parentKey" Type="Object" />--%>
            <asp:ControlParameter ControlID="ddlCategory" Name="parentKey" PropertyName="SelectedValue"
                Type="Int32" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <asp:ObjectDataSource ID="odsRole" runat="server" SelectMethod="FindAllByName">
        <SelectParameters>
            <asp:Parameter Name="name" Type="String" />
            <asp:Parameter Name="value" Type="Object" />
            <asp:Parameter Name="orderClause" Type="String" />
            <asp:Parameter Name="startRowIndex" Type="Int32" />
            <asp:Parameter Name="maximumRows" Type="Int32" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <XCL:GridViewExtender ID="gvExt" runat="server">
    </XCL:GridViewExtender>
</asp:Content>
