<%@ Page Language="C#" AutoEventWireup="true" CodeFile="RoleMenu.aspx.cs" Inherits="Pages_RoleMenu"
    Title="权限管理" MasterPageFile="~/Admin/MasterPage.master" MaintainScrollPositionOnPostback="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="toolbar">
        &nbsp;角色：<asp:DropDownList ID="DropDownList1" runat="server" DataSourceID="ObjectDataSource2"
            DataTextField="Name" DataValueField="ID" OnSelectedIndexChanged="DropDownList1_SelectedIndexChanged"
            AutoPostBack="True">
        </asp:DropDownList>
    </div>
    <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
        CssClass="m_table" CellPadding="0" GridLines="None" PageSize="15" EnableModelValidation="True"
        DataSourceID="ObjectDataSource1" OnRowDataBound="GridView1_RowDataBound" 
        EnableViewState="False">
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
    <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="NewLife.CommonEntity.Menu"
        DeleteMethod="Delete" OldValuesParameterFormatString="original_{0}" SelectMethod="FindAllChildsByParent"
        TypeName="NewLife.CommonEntity.Menu">
        <SelectParameters>
            <asp:Parameter DefaultValue="0" Name="parentKey" Type="Object" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <asp:ObjectDataSource ID="ObjectDataSource2" runat="server" OldValuesParameterFormatString="original_{0}"
        SelectMethod="FindAllByName" TypeName="NewLife.CommonEntity.Role" DataObjectTypeName="NewLife.CommonEntity.Role"
        DeleteMethod="Delete" InsertMethod="Insert" UpdateMethod="Update">
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
