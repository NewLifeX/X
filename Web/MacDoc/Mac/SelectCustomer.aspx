<%@ Page Language="C#" AutoEventWireup="true" EnableEventValidation="false" CodeFile="SelectCustomer.aspx.cs"
    Inherits="Pages_SelectCustomer" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>选择客户</title>
    <base target="_self" />
    <link href="<%= ResolveUrl("~/Admin/images/css.css")%>" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div class="toolbar" style="text-align: center;">
            关键字:<asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>&nbsp;<asp:Button ID="Button3"
                runat="server" Text="搜索" />
            &nbsp; 选择了：<asp:Label ID="lblmsg" runat="server" ForeColor="Red">未选择
            </asp:Label>（提示：双击选中）
        </div>
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
            DataSourceID="ObjectDataSource1" AllowPaging="True" AllowSorting="True" PageSize="20"
            EnableModelValidation="True" HorizontalAlign="Center" CellPadding="0" CellSpacing="1"
            CssClass="m_table" GridLines="None">
            <Columns>
                <asp:BoundField DataField="No" HeaderText="编号" SortExpression="No" />
                <asp:BoundField DataField="Name" HeaderText="名称" SortExpression="Name" />
                <asp:BoundField DataField="Department" HeaderText="部门" SortExpression="Department" />
                <asp:BoundField DataField="Linkman" HeaderText="联系人" SortExpression="Linkman" />
            </Columns>
            <EmptyDataTemplate>
                没有符合条件的数据！
            </EmptyDataTemplate>
        </asp:GridView>
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" EnablePaging="True" OldValuesParameterFormatString="original_{0}"
            SelectMethod="Search" TypeName="NewLife.YWS.Entities.Customer" SelectCountMethod="SearchCount"
            SortParameterName="orderClause">
            <SelectParameters>
                <asp:ControlParameter ControlID="TextBox1" Name="key" PropertyName="Text" Type="String" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter DefaultValue="0" Name="startRowIndex" Type="Int32" />
                <asp:Parameter DefaultValue="1000" Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <XCL:GridViewExtender ID="gvExt" runat="server" OnRowDoubleClientClick="window.returnValue='{datakey}|||{cell0}';window.close();">
        </XCL:GridViewExtender>
    </div>
    </form>
</body>
</html>
