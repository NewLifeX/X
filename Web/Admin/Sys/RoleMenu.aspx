<%@ Page Language="C#" AutoEventWireup="true" CodeFile="RoleMenu.aspx.cs" Inherits="Pages_RoleMenu"
    Title="权限管理" MasterPageFile="~/Admin/ManagerPage.master" MaintainScrollPositionOnPostback="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <div class="toolbar">
        &nbsp;角色：<asp:DropDownList ID="ddlRole" runat="server" DataSourceID="odsRole" DataTextField="Name"
            DataValueField="ID" OnSelectedIndexChanged="ddlRole_SelectedIndexChanged" AutoPostBack="True">
        </asp:DropDownList>
        &nbsp;大类：<asp:DropDownList ID="ddlCategory" runat="server" DataTextField="Name" DataValueField="ID"
            AutoPostBack="True" AppendDataBoundItems="True">
            <asp:ListItem Value="0">全部</asp:ListItem>
        </asp:DropDownList>
    </div>
    <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
        CssClass="m_table" CellPadding="0" GridLines="None" PageSize="15" EnableModelValidation="True"
        DataSourceID="ods" OnRowDataBound="gv_RowDataBound" EnableViewState="False">
        <Columns>
            <asp:BoundField DataField="ID" HeaderText="编号" InsertVisible="False" ReadOnly="True"
                SortExpression="ID">
                <HeaderStyle Width="40px" />
                <ItemStyle CssClass="key" HorizontalAlign="Center" />
            </asp:BoundField>
            <asp:TemplateField HeaderText="权限名称" SortExpression="Permission">
                <ItemTemplate>
                    <%# new String('　', (Convert.ToInt32(Eval("Deepth"))-1)*2)%><asp:Label ID="Label1"
                        runat="server" Text='<%# Eval("Permission") %>'></asp:Label>
                </ItemTemplate>
                <ItemStyle Width="200px" />
            </asp:TemplateField>
            <asp:TemplateField HeaderText="授权">
                <ItemTemplate>
                    <asp:CheckBox ID="CheckBox1" runat="server" AutoPostBack="True" BorderWidth="0px"
                        OnCheckedChanged="CheckBox1_CheckedChanged" />
                </ItemTemplate>
                <ItemStyle Width="40px" />
            </asp:TemplateField>
            <asp:TemplateField HeaderText="操作权限">
                <ItemTemplate>
                    <asp:CheckBoxList ID="CheckBoxList1" runat="server" AutoPostBack="True" OnSelectedIndexChanged="CheckBoxList1_SelectedIndexChanged"
                        RepeatDirection="Horizontal" RepeatLayout="Flow">
                    </asp:CheckBoxList>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            没有符合条件的数据！
        </EmptyDataTemplate>
    </asp:GridView>
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
