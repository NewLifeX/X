<%@ Page Title="菜单管理" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    CodeFile="Menu.aspx.cs" Inherits="Common_Menu" %>

<asp:Content ID="Content2" runat="server" ContentPlaceHolderID="H">
    <link rel="stylesheet" href="<%= ResolveUrl("~/UI/css/uniform.css")%>" type="text/css" />
    <script src="<%= ResolveUrl("~/UI/js/jquery.uniform.js")%>" type="text/javascript"></script>
    <script src="<%= ResolveUrl("~/UI/js/unicorn.form_common.js")%>" type="text/javascript"></script>
</asp:Content>
<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <div class="row-fluid">
        <Custom:OpenForm BtText="添加菜单" runat="server" ID="lbAdd" DialogWidth="700px" DialogHeight="450px"
            Url="MenuForm.aspx" Title="Menu"></Custom:OpenForm>
        &nbsp;&nbsp;&nbsp;<asp:Button ID="Button2" runat="server" Text="导出" OnClick="Button2_Click" CssClass="btn btn-success" />
        &nbsp;&nbsp;&nbsp;<asp:FileUpload ID="FileUpload1" runat="server" _CssClass="uploader focus" />
        &nbsp;&nbsp;&nbsp;<asp:Button ID="Button3" runat="server" Text="导入" OnClick="Button3_Click" CssClass="btn btn-danger" />
        &nbsp;&nbsp;&nbsp;<asp:Button ID="Button1" runat="server" Text="扫描目录" OnClick="Button1_Click" CssClass="btn btn-warning" />
    </div>
    <div class="row-fluid">
        <div class="widget-box">
            <div class="widget-content nopadding">
                <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
                    DataSourceID="ods" AllowPaging="True" AllowSorting="True" CssClass="table table-bordered table-striped"
                    PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True" OnRowCommand="gv_RowCommand">
                    <Columns>
                        <asp:BoundField DataField="ID" HeaderText="编号" InsertVisible="False" ReadOnly="True"
                            SortExpression="ID">
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" Width="40px" />
                            <ItemStyle CssClass="key" HorizontalAlign="Center" />
                        </asp:BoundField>
                        <asp:BoundField DataField="TreeNodeName" HeaderText="名称" SortExpression="Name">
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:BoundField>
                        <asp:BoundField DataField="Url" HeaderText="链接" SortExpression="Url">
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:BoundField>
                        <asp:BoundField DataField="ParentMenuName" HeaderText="父菜单" SortExpression="ParentID">
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:BoundField>
                        <asp:BoundField DataField="Sort" HeaderText="序号" SortExpression="Sort">
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:BoundField>
                        <asp:BoundField DataField="Permission" HeaderText="权限" SortExpression="Permission">
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:BoundField>
                        <asp:TemplateField HeaderText="显示">
                            <ItemTemplate>
                                <asp:CheckBox ID="checkebox1" runat="server" Enabled="false" Checked='<%# Bind("IsShow") %>' />
                            </ItemTemplate>
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:TemplateField>
                        <asp:BoundField DataField="Remark" HeaderText="备注" SortExpression="Remark">
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:BoundField>
                        <asp:TemplateField HeaderText="升" ShowHeader="False">
                            <ItemTemplate>
                                <asp:LinkButton ID="LinkButton2" runat="server" CausesValidation="False" CommandArgument='<%# Eval("ID") %>'
                                    CommandName="Up" Text="↑" Font-Size="12pt" ForeColor="Red" Visible='<%# !IsFirst(Container.DataItem) %>'></asp:LinkButton>
                            </ItemTemplate>
                            <ItemStyle Font-Size="12pt" ForeColor="Red" />
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="降" ShowHeader="False">
                            <ItemTemplate>
                                <asp:LinkButton ID="LinkButton3" runat="server" CausesValidation="False" CommandArgument='<%# Eval("ID") %>'
                                    CommandName="Down" Text="↓" Font-Size="12pt" ForeColor="Green" Visible='<%# !IsLast(Container.DataItem) %>'></asp:LinkButton>
                            </ItemTemplate>
                            <ItemStyle Font-Size="12pt" ForeColor="Green" />
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:TemplateField>
                        <XCL:LinkBoxField HeaderText="添加子菜单" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="MenuForm.aspx?ParentID={0}"
                            Height="400px" Text="添加子菜单" Width="370px" Title="添加子菜单">
                            <ItemStyle HorizontalAlign="Center" />
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </XCL:LinkBoxField>
                        <asp:TemplateField HeaderText="编辑">
                            <ItemTemplate>
                                <Custom:OpenForm BtText="编辑" runat="server" ID="OpenForm1" DialogWidth="700px" DialogHeight="450px"
                                    Url='<%# "MenuForm.aspx?ID="+ Eval("ID")%>' Title="Menu" IsButtonStyle="false">
                                </Custom:OpenForm>
                            </ItemTemplate>
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" Width="30px" />
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="删除" ShowHeader="False">
                            <ItemTemplate>
                                <asp:LinkButton ID="LinkButton1" runat="server" CausesValidation="False" CommandName="Delete"
                                    OnClientClick="return confirm('确定删除？');" Text="删除"></asp:LinkButton>
                            </ItemTemplate>
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" Width="30px" />
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        没有符合条件的数据！
                    </EmptyDataTemplate>
                </asp:GridView>
            </div>
        </div>
    </div>
    <asp:ObjectDataSource ID="ods" runat="server" DeleteMethod="Delete" SelectMethod="FindAllChildsNoParent"
        EnableViewState="False">
        <SelectParameters>
            <asp:Parameter DefaultValue="0" Name="parentKey" Type="Object" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <XCL:GridViewExtender ID="gvExt" runat="server">
    </XCL:GridViewExtender>
</asp:Content>
