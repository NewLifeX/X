<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SelectAdmin.aspx.cs" Inherits="Admin_SelectAdmin" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>选择管理员</title>
    <base target="_self" />
    <link href="<%= ResolveUrl("~/Admin/images/css.css")%>" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div class="toolbar" style="text-align: center;">
            关键字:<asp:TextBox ID="txtKey" runat="server"></asp:TextBox>&nbsp; 角色：<XCL:DropDownList
                ID="ddlRole" runat="server" DataSourceID="odsRole" AppendDataBoundItems="true"
                DataTextField="Name" DataValueField="ID">
                <asp:ListItem Value="0">请选择</asp:ListItem>
            </XCL:DropDownList>
            <asp:Button ID="Button3" runat="server" Text="搜索" CssClass="btn btn-primary" />
            &nbsp; 选择了：<asp:Label ID="lblmsg" runat="server" ForeColor="Red">未选择
            </asp:Label>（提示：双击选中）
        </div>
        <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
            DataSourceID="ods" AllowPaging="True" AllowSorting="True" CssClass="m_table"
            PageSize="20" EnableModelValidation="True" GridLines="None">
            <Columns>
                <asp:BoundField DataField="ID" HeaderText="编号" SortExpression="ID" InsertVisible="False"
                    ReadOnly="True">
                    <HeaderStyle Width="40px" />
                    <ItemStyle CssClass="key" HorizontalAlign="Center" />
                </asp:BoundField>
                <asp:BoundField DataField="Name" HeaderText="用户名" />
                <asp:BoundField DataField="FriendName" HeaderText="友好名称" />
                <asp:BoundField DataField="RoleName" HeaderText="角色" SortExpression="RoleID" />
            </Columns>
            <EmptyDataTemplate>
                没有符合条件的数据！
            </EmptyDataTemplate>
        </asp:GridView>
        <asp:ObjectDataSource ID="ods" runat="server" DataObjectTypeName=""
            EnablePaging="True" OldValuesParameterFormatString="original_{0}"
            SelectCountMethod="SearchCount" SelectMethod="Search" SortParameterName="orderClause"
            TypeName="">
            <SelectParameters>
                <asp:ControlParameter ControlID="txtKey" Name="key" PropertyName="Text" Type="String" />
                <asp:ControlParameter ControlID="ddlRole" Name="roleId" Type="Int32" PropertyName="Text" />
                <asp:Parameter Name="isEnable" Type="Boolean" DefaultValue="true" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter Name="startRowIndex" Type="Int32" />
                <asp:Parameter Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <asp:ObjectDataSource ID="odsRole" runat="server" OldValuesParameterFormatString="original_{0}"
            SelectMethod="FindAllByName" TypeName="" DataObjectTypeName="">
            <SelectParameters>
                <asp:Parameter Name="name" Type="String" />
                <asp:Parameter Name="value" Type="Object" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter Name="startRowIndex" Type="Int32" />
                <asp:Parameter Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <XCL:GridViewExtender ID="gvExt" runat="server" OnRowDoubleClientClick="window.returnValue='{datakey}|||{cell2}';window.close();">
        </XCL:GridViewExtender>
    </div>
    </form>
</body>
</html>
