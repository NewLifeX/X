<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Menu.aspx.cs" Inherits="Pages_Menu"
    MasterPageFile="~/Admin/ManagerPage.master" Title="菜单管理" MaintainScrollPositionOnPostback="true"
    EnableViewState="false" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <div class="toolbar">
        <XCL:LinkBox ID="lbAdd" runat="server" BoxHeight="400px" BoxWidth="370px" Url="MenuForm.aspx"
            IconLeft="~/Admin/images/icons/new.gif" EnableViewState="False"><b>添加菜单</b></XCL:LinkBox>
        <asp:Label ID="Label_Info" runat="server" ForeColor="Red"></asp:Label>
        &nbsp;&nbsp;&nbsp;<asp:Button ID="Button2" runat="server" Text="导出" OnClick="Button2_Click" CssClass="btn btn-success" />
        &nbsp;&nbsp;&nbsp;
        <asp:FileUpload ID="FileUpload1" runat="server" />
        &nbsp;<asp:Button ID="Button3" runat="server" Text="导入" OnClick="Button3_Click" CssClass="btn btn-danger" />
        &nbsp;<asp:Button ID="Button1" runat="server" Text="扫描目录" OnClick="Button1_Click" CssClass="btn btn-warning" />
    </div>
    <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
        CssClass="m_table" CellPadding="0" GridLines="None" PageSize="15" EnableModelValidation="True"
        DataSourceID="ods" OnRowCommand="gv_RowCommand" EnableViewState="False">
        <Columns>
            <asp:BoundField DataField="ID" HeaderText="编号" InsertVisible="False" ReadOnly="True"
                SortExpression="ID">
                <HeaderStyle Width="40px" />
                <ItemStyle CssClass="key" HorizontalAlign="Center" />
            </asp:BoundField>
            <asp:BoundField DataField="TreeNodeName" HeaderText="名称" SortExpression="Name" />
            <asp:BoundField DataField="Url" HeaderText="链接" SortExpression="Url" />
            <asp:BoundField DataField="ParentMenuName" HeaderText="父菜单" SortExpression="ParentID" />
            <asp:BoundField DataField="Sort" HeaderText="序号" SortExpression="Sort" />
            <asp:BoundField DataField="Permission" HeaderText="权限" SortExpression="Permission" />
            <asp:TemplateField HeaderText="显示">
                <ItemTemplate>
                    <asp:CheckBox ID="checkebox1" runat="server" Enabled="false" Checked='<%# Bind("IsShow") %>' />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Remark" HeaderText="备注" SortExpression="Remark" />
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
            <XCL:LinkBoxField HeaderText="添加子菜单" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="MenuForm.aspx?ParentID={0}"
                Height="400px" Text="添加子菜单" Width="370px" Title="添加子菜单">
                <ItemStyle HorizontalAlign="Center" />
            </XCL:LinkBoxField>
            <XCL:LinkBoxField HeaderText="编辑" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="MenuForm.aspx?ID={0}"
                Height="400px" Text="编辑" Width="370px" Title="编辑菜单">
                <ItemStyle HorizontalAlign="Center" />
            </XCL:LinkBoxField>
            <asp:TemplateField HeaderText="删除" ShowHeader="False">
                <ItemTemplate>
                    <asp:LinkButton ID="LinkButton1" runat="server" CausesValidation="False" CommandName="Delete"
                        OnClientClick="return confirm('确定删除？');" Text="删除"></asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:CheckBoxField />
        </Columns>
        <EmptyDataTemplate>
            没有符合条件的数据！
        </EmptyDataTemplate>
    </asp:GridView>
    <asp:ObjectDataSource ID="ods" runat="server" DeleteMethod="Delete" SelectMethod="FindAllChildsNoParent"
        EnableViewState="False">
        <SelectParameters>
            <asp:Parameter DefaultValue="0" Name="parentKey" Type="Object" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <XCL:GridViewExtender ID="gvExt" runat="server">
    </XCL:GridViewExtender>
</asp:Content>
