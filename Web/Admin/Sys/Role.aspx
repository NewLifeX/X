<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Role.aspx.cs" Inherits="Pages_Role"
    MasterPageFile="~/Admin/ManagerPage.master" Title="角色管理" MaintainScrollPositionOnPostback="true"
    EnableViewState="false" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <div class="toolbar">
        角色名称：
        <asp:TextBox ID="txtName" runat="server" CssClass="textfield"></asp:TextBox>
        &nbsp;<asp:Button ID="btnAdd" runat="server" Text="添加" OnClick="Button1_Click" />
        &nbsp;模版角色：<asp:TextBox ID="txtRoleTemplate" runat="server" CssClass="textfield"></asp:TextBox>
        &nbsp;<asp:Button ID="btnCopyRole" runat="server" Text="批量复制权限" 
            OnClientClick='return confirm("确定批量操作吗？")' onclick="btnCopyRole_Click" />
    </div>
    <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
        DataSourceID="ods" AllowPaging="True" AllowSorting="True" CssClass="m_table"
        CellPadding="0" GridLines="None" PageSize="20" EnableModelValidation="True">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:CheckBox ID="cb" runat="server" />
                </ItemTemplate>
                <HeaderStyle Width="20px" />
                <ItemStyle HorizontalAlign="Center" />
            </asp:TemplateField>
            <asp:BoundField DataField="ID" HeaderText="编号" InsertVisible="False" ReadOnly="True"
                SortExpression="ID">
                <HeaderStyle Width="40px" />
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" CssClass="key" />
            </asp:BoundField>
            <asp:BoundField DataField="Name" HeaderText="名称" SortExpression="Name" />
            <asp:CommandField HeaderText="编辑" ShowEditButton="True">
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="60px" />
            </asp:CommandField>
            <asp:TemplateField HeaderText="删除" ShowHeader="False">
                <ItemTemplate>
                    <asp:LinkButton ID="LinkButton1" runat="server" CausesValidation="False" CommandName="Delete"
                        OnClientClick="return confirm('确定删除？');" Text="删除"></asp:LinkButton>
                </ItemTemplate>
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="60px" />
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            没有符合条件的数据！
        </EmptyDataTemplate>
    </asp:GridView>
    <asp:ObjectDataSource ID="ods" runat="server" EnablePaging="True" OldValuesParameterFormatString="original_{0}"
        SelectCountMethod="FindCountByName" SelectMethod="FindAllByName" SortParameterName="orderClause"
        InsertMethod="Insert" DeleteMethod="Delete" UpdateMethod="Update">
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
