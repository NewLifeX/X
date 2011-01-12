<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default3.aspx.cs" Inherits="Default3" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
            DataSourceID="ObjectDataSource1" EnableModelValidation="True" EnableViewState="False"
            AllowPaging="True">
            <Columns>
                <asp:TemplateField>
                    <ItemTemplate>
                        <asp:CheckBox ID="CheckBox1" runat="server" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="ID" HeaderText="ID" InsertVisible="False" ReadOnly="True"
                    SortExpression="ID" />
                <asp:BoundField DataField="Code" HeaderText="Code" SortExpression="Code" />
                <asp:BoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                <asp:BoundField DataField="ParentCode" HeaderText="ParentCode" SortExpression="ParentCode" />
                <asp:BoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
            </Columns>
            <PagerTemplate>
                共<asp:Label runat="server"></asp:Label>
                条&nbsp;每页<asp:Label runat="server"></asp:Label>
                条&nbsp;当前第<asp:Label runat="server"></asp:Label>
                页/共<asp:Label runat="server"></asp:Label>
                页&nbsp;<asp:LinkButton runat="server" CommandArgument="First" CommandName="Page">首页</asp:LinkButton>
                <asp:LinkButton runat="server" CommandArgument="Prev" CommandName="Page">上一页</asp:LinkButton>
                <asp:LinkButton runat="server" CommandArgument="Next" CommandName="Page">下一页</asp:LinkButton>
                <asp:LinkButton runat="server" CommandArgument="Last" CommandName="Page">尾页</asp:LinkButton>
                转到第<asp:TextBox runat="server" style="text-align:right;" Width="40px"></asp:TextBox>
                页<asp:Button runat="server" Text="GO" UseSubmitBehavior="False" />
            </PagerTemplate>
        </asp:GridView>
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" EnablePaging="True" OldValuesParameterFormatString="original_{0}"
            SelectCountMethod="FindCountByName" SelectMethod="FindAllByName" TypeName="NewLife.CommonEntity.Area">
            <SelectParameters>
                <asp:Parameter Name="name" Type="String" />
                <asp:Parameter Name="value" Type="Object" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter Name="startRowIndex" Type="Int32" />
                <asp:Parameter Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <XCL:GridViewExtender ID="GridViewExtender1" runat="server" SelectedRowBackColor="Cornsilk">
        </XCL:GridViewExtender>
        <asp:Button ID="Button1" runat="server" onclick="Button1_Click" Text="Button" />
        <br />
        <asp:Label ID="Label1" runat="server" Text="Label"></asp:Label>
        <br />
        <asp:Label ID="Label2" runat="server" Text="Label"></asp:Label>
    </div>
    </form>
</body>
</html>
