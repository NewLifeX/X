<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ControlTest.aspx.cs" Inherits="ControlTest"
    Trace="true" %>

<%@ Register Assembly="XControl" Namespace="XControl" TagPrefix="XCL" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        日期选择：<XCL:DateBox ID="DateBox1" runat="server"></XCL:DateBox>
        <br />
        整型选择：<XCL:IntCheckBox ID="IntCheckBox1" runat="server" />
        <br />
        IP输入：<XCL:IPBox ID="IPBox1" runat="server"></XCL:IPBox>
        <br />
        邮箱输入：<XCL:MailBox ID="MailBox1" runat="server"></XCL:MailBox>
        <br />
        整数输入：<XCL:NumberBox ID="NumberBox1" runat="server" Value="222">222</XCL:NumberBox>
        <br />
        浮点输入：<XCL:RealBox ID="RealBox1" runat="server"></XCL:RealBox>
        <XCL:DateTimePicker ID="DateTimePicker1" runat="server" Skin="绿色"></XCL:DateTimePicker>
        <br />
        <XCL:ChooseButton ID="ChooseButton1" runat="server" ControlID="" Url="SelectArea.aspx?areacode={value}"
            Value="654326" Text="请选择城市！" />
        <br />
        <br />
        <asp:Button ID="Button1" runat="server" Text="Button" />
        <br />
        <br />
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
            DataSourceID="ObjectDataSource1" EnableModelValidation="True" EnableViewState="False">
            <Columns>
                <asp:BoundField DataField="ID" HeaderText="ID" InsertVisible="False" ReadOnly="True"
                    SortExpression="ID" />
                <asp:BoundField DataField="Code" HeaderText="Code" SortExpression="Code" />
                <asp:BoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                <asp:BoundField DataField="ParentCode" HeaderText="ParentCode" SortExpression="ParentCode" />
                <asp:BoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
            </Columns>
        </asp:GridView>
        <XCL:DataPager ID="DataPager2" runat="server" OnPageCommand="DataPager2_PageCommand"
            OnPageIndexChanged="DataPager2_PageIndexChanged" OnPageIndexChanging="DataPager2_PageIndexChanging"
            DataSourceID="ObjectDataSource1" PageIndex2="1">
            <PagerSettings Mode="NumericFirstLast" />
            <PagerTemplate>
                共<asp:Label ID="Label6" runat="server" Text='<%# Eval("TotalRowCount") %>'></asp:Label>条
                每页<asp:Label ID="Label3" runat="server" Text='<%# Eval("PageSize") %>'></asp:Label>条
                当前第
                <asp:Label ID="LabelCurrentPage" runat="server" Text='<%# Eval("PageIndex2") %>'
                    OnDataBinding="Label4_DataBinding"></asp:Label>
                页/共
                <asp:Label ID="LabelPageCount" runat="server" Text='<%# Eval("PageCount") %>'></asp:Label>
                页
                <asp:LinkButton ID="LinkButtonFirstPage" runat="server" CommandArgument="First" CommandName="Page"
                    Visible='<%# !(Boolean)Eval("IsFirstPage") %>'>首页</asp:LinkButton>
                <asp:LinkButton ID="LinkButtonPreviousPage" runat="server" CommandArgument="Prev"
                    CommandName="Page" Visible='<%# !(Boolean)Eval("IsFirstPage") %>'>上一页</asp:LinkButton>
                <asp:LinkButton ID="LinkButtonNextPage" runat="server" CommandArgument="Next" CommandName="Page"
                    Visible='<%# !(Boolean)Eval("IsLastPage") %>'>下一页</asp:LinkButton>
                <asp:LinkButton ID="LinkButtonLastPage" runat="server" CommandArgument="Last" CommandName="Page"
                    Visible='<%# !(Boolean)Eval("IsLastPage") %>'>尾页</asp:LinkButton>
                转到第
                <input type="textbox" id="txtNewPageIndex" style="width: 40px;" value='<%# Eval("PageIndex2") %>' />页
                <input type="button" id="btnGo" value="GO" onclick="javascript:__doPostBack('<%# ((DataPager)Container.NamingContainer).UniqueID %>','Page$'+document.getElementById('txtNewPageIndex').value)" />
            </PagerTemplate>
        </XCL:DataPager>
        <br />
        <asp:Label ID="Label1" runat="server" Text="Label"></asp:Label>
        <br />
        <asp:Label ID="Label2" runat="server" Text="Label"></asp:Label>
        <br />
        <asp:Label ID="Label4" runat="server" Text="Label"></asp:Label>
        <br />
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="XSite.Entities.Area"
            DeleteMethod="Delete" EnablePaging="True" InsertMethod="Insert" OldValuesParameterFormatString="original_{0}"
            SelectCountMethod="FindCountByName" SelectMethod="FindAllByName" TypeName="XSite.Entities.Area"
            UpdateMethod="Save">
            <SelectParameters>
                <asp:Parameter Name="name" Type="String" />
                <asp:Parameter Name="value" Type="Object" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter Name="startRowIndex" Type="Int32" />
                <asp:Parameter Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <asp:Repeater ID="Repeater1" runat="server" DataSourceID="ObjectDataSource1" EnableViewState="False">
            <ItemTemplate>
                名称：<asp:Label ID="Label5" runat="server" Text='<%# Eval("Name")%>' OnDataBinding="Label5_DataBinding"></asp:Label><br />
            </ItemTemplate>
        </asp:Repeater>
        <br />
        <XCL:LinkBox runat="server" IconLeft="aa.jpg" IconRight="bb.gif">Test</XCL:LinkBox>
    </div>
    </form>
</body>
</html>
