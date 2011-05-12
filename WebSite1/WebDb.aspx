<%@ Page Language="C#" AutoEventWireup="true" CodeFile="WebDb.aspx.cs" Inherits="Admin_System_WebDb"
    Title="网站数据库" MasterPageFile="~/MasterPage.master" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="toolbar">
        数据库链接：<asp:DropDownList ID="ddlConn" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlConn_SelectedIndexChanged">
        </asp:DropDownList>
        &nbsp;<asp:TextBox ID="txtConnStr" runat="server" Width="803px"></asp:TextBox>
    </div>
    <div class="toolbar" style="height: auto;">
        <asp:Panel ID="Panel1" runat="server">
            数据表：<asp:DropDownList ID="ddlTable" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlTable_SelectedIndexChanged">
            </asp:DropDownList>
            &nbsp;数据架构：<asp:DropDownList ID="ddlSchema" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlSchema_SelectedIndexChanged">
            </asp:DropDownList>
        </asp:Panel>
    </div>
    <asp:GridView ID="gvTable" runat="server" CssClass="m_table" CellPadding="0" CellSpacing="1"
        GridLines="None" OnRowDataBound="gvTable_RowDataBound" EnableModelValidation="True">
    </asp:GridView>
    SQL语句：（主键，分页时用）<asp:TextBox ID="txtKey" runat="server" Width="48px">ID</asp:TextBox>
    &nbsp;<asp:Button ID="Button1" runat="server" Text="查询" OnClick="Button1_Click" />
    <br />
    <asp:TextBox ID="txtSql" runat="server" Height="138px" Width="738px" TextMode="MultiLine"></asp:TextBox>
    &nbsp;<br />
    结果：<asp:Label ID="lbResult" runat="server" ForeColor="Red"></asp:Label>
    <br />
    <asp:GridView ID="gvResult" runat="server" CssClass="m_table" CellPadding="0" CellSpacing="1"
        GridLines="None" OnRowDataBound="gvTable_RowDataBound">
    </asp:GridView>
    <XCL:GridViewExtender ID="GridViewExtender1" runat="server">
    </XCL:GridViewExtender>
    <XCL:DataPager ID="DataPager1" runat="server" OnPageIndexChanging="DataPager2_PageIndexChanging"
        DataSourceID="ObjectDataSource1" PageIndex2="1">
        <PagerSettings Mode="NumericFirstLast" />
        <PagerTemplate>
            共<asp:Label ID="Label6" runat="server" Text='<%# Eval("TotalRowCount") %>'></asp:Label>条
            每页<asp:Label ID="Label3" runat="server" Text='<%# Eval("PageSize") %>'></asp:Label>条
            当前第
            <asp:Label ID="LabelCurrentPage" runat="server" Text='<%# Eval("PageIndex2") %>'></asp:Label>
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
            <input type="button" id="btnGo" value="GO" onclick="javascript:__doPostBack('<%# ((XControl.DataPager)Container.NamingContainer).UniqueID %>','Page$'+document.getElementById('txtNewPageIndex').value)" />
        </PagerTemplate>
    </XCL:DataPager>
    <asp:Literal ID="RunTime" runat="server"></asp:Literal>
</asp:Content>
