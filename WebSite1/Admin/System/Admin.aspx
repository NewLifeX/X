<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Admin.aspx.cs" Inherits="Pages_Admin"
    Title="管理员管理" MasterPageFile="~/Admin/Main.master" %>

<asp:Content ID="C" ContentPlaceHolderID="C" runat="server">
    <div class="toolbar">
        <asp:Label Text="关键字" runat="server" />
        &nbsp;<asp:TextBox runat="server" ID="TB_key" />
        角色：<XCL:DropDownList ID="DropdownList1" runat="server" DataSourceID="ObjectDataSource2"
            AppendDataBoundItems="true" DataTextField="Name" DataValueField="ID" AutoPostBack="True">
            <asp:ListItem Value="0">请选择</asp:ListItem>
        </XCL:DropDownList>
        &nbsp;<asp:Button ID="Button1" runat="server" Text="查询" />
        <asp:Label ID="Label_Info" runat="server" ForeColor="#FF3300"></asp:Label>
        <XCL:LinkBox ID="lbAdd" runat="server" BoxHeight="370px" BoxWidth="440px" Url="AdminForm.aspx"
            IconLeft="~/Admin/images/icons/new.gif"><b>添加管理员</b></XCL:LinkBox>
    </div>
    <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
        DataSourceID="ods" CssClass="m_table" CellPadding="0" GridLines="None"
        EnableModelValidation="True" EnableViewState="False">
        <Columns>
            <asp:BoundField DataField="ID" HeaderText="编号" SortExpression="ID" InsertVisible="False"
                ReadOnly="True">
                <HeaderStyle Width="40px" />
                <ItemStyle CssClass="key" HorizontalAlign="Center" />
            </asp:BoundField>
            <XCL:LinkBoxField HeaderText="用户名" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="AdminForm.aspx?ID={0}"
                Height="400px" DataTextField="Name" Width="370px" Title="编辑管理员">
                <ItemStyle HorizontalAlign="Center" />
            </XCL:LinkBoxField>
            <XCL:LinkBoxField HeaderText="友好名称" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="AdminForm.aspx?ID={0}"
                Height="400px" DataTextField="FriendName" Text="友好名称" Width="370px" Title="编辑管理员">
                <ItemStyle HorizontalAlign="Center" />
            </XCL:LinkBoxField>
            <asp:BoundField DataField="RoleName" HeaderText="角色" SortExpression="RoleID" />
            <asp:BoundField DataField="Logins" HeaderText="登录次数" SortExpression="Logins" />
            <asp:BoundField DataField="LastLogin" HeaderText="最后登录" SortExpression="LastLogin"
                DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" />
            <asp:BoundField DataField="LastLoginIP" HeaderText="最后登陆IP" SortExpression="LastLoginIP" />
            <asp:TemplateField HeaderText="是否启用" SortExpression="IsEnable">
                <ItemTemplate>
                    <asp:CheckBox ID="checkbox1" runat="server" Enabled="false" Checked='<%# Bind("IsEnable")%>' />
                </ItemTemplate>
                <EditItemTemplate>
                    <asp:CheckBox ID="checkbox2" runat="server" Checked='<%# Bind("IsEnable")%>' />
                </EditItemTemplate>
            </asp:TemplateField>
            <XCL:LinkBoxField HeaderText="编辑" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="AdminForm.aspx?ID={0}"
                Height="400px" Text="编辑" Width="370px" Title="编辑管理员">
                <ItemStyle HorizontalAlign="Center" />
            </XCL:LinkBoxField>
            <asp:TemplateField HeaderText="删除" ShowHeader="False">
                <ItemTemplate>
                    <asp:LinkButton ID="LinkButton1" runat="server" CausesValidation="False" CommandName="Delete"
                        OnClientClick="return confirm('确定删除？');" Text="删除"></asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            没有数据!
        </EmptyDataTemplate>
    </asp:GridView>
    <asp:ObjectDataSource ID="ods" runat="server" SelectMethod="Search" DeleteMethod="Delete">
        <SelectParameters>
            <asp:ControlParameter ControlID="TB_key" Name="key" Type="String" PropertyName="Text" />
            <asp:ControlParameter ControlID="DropdownList1" Name="roleID" Type="Int32" PropertyName="SelectedValue" />
            <asp:Parameter Name="orderClause" Type="String" />
            <asp:Parameter DefaultValue="0" Name="startRowIndex" Type="Int32" />
            <asp:Parameter DefaultValue="2000" Name="maximumRows" Type="Int32" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <asp:ObjectDataSource ID="ObjectDataSource2" runat="server" SelectMethod="FindAllByName">
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
