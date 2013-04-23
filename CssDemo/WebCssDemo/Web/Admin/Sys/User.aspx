<%@ Page Title="用户管理" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    CodeFile="User.aspx.cs" Inherits="Common_User" %>

    <asp:Content ID="g" ContentPlaceHolderID="H" runat="server">
<style type="text/css">
.btn
{
    vertical-align:top;
    }
</style>
</asp:Content>
<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <div class="row-fluid">
        <XCL:LinkBox ID="lbAdd" runat="server" BoxHeight="211px" BoxWidth="440px" Url="UserForm.aspx"
            IconLeft="~/Admin/images/icons/new.gif" EnableViewState="false" CssClass="btn"><b>添加用户</b></XCL:LinkBox>
        关键字：<asp:TextBox ID="txtKey" runat="server"></asp:TextBox>
        <asp:Button ID="btnSearch" runat="server" Text="查询" CssClass="btn btn-primary" />
    </div>
    <div class="row-fluid">
        <div class="widget-box">
            <div class="widget-content nopadding">
                <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
                    DataSourceID="ods" AllowPaging="True" AllowSorting="True" CssClass="table table-bordered table-striped"
                    PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True">
                    <Columns>
                        <%--<asp:TemplateField>
                <ItemTemplate>
                    <asp:CheckBox ID="cb" runat="server" />
                </ItemTemplate>
                <HeaderStyle Width="20px" />
                <ItemStyle HorizontalAlign="Center" />
            </asp:TemplateField>--%>
                        <asp:BoundField DataField="ID" HeaderText="编号" SortExpression="ID" InsertVisible="False"
                            ReadOnly="True">
                            <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" CssClass="Ikey" />
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" Width="40px" />
                        </asp:BoundField>
                        <asp:BoundField DataField="Account" HeaderText="账号" SortExpression="Account">
                        <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:BoundField>
                        <asp:TemplateField HeaderText="是否管理员" SortExpression="IsAdmin">
                            <ItemTemplate>
                                <asp:Label ID="IsAdmin1" runat="server" Text="√" Visible='<%# Eval("IsAdmin") %>'
                                    Font-Bold="True" Font-Size="14pt" ForeColor="Green"></asp:Label>
                                <asp:Label ID="IsAdmin2" runat="server" Text="×" Visible='<%# !(Boolean)Eval("IsAdmin") %>'
                                    Font-Bold="True" Font-Size="16pt" ForeColor="Red"></asp:Label>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="是否启用" SortExpression="IsEnable">
                            <ItemTemplate>
                                <asp:Label ID="IsEnable1" runat="server" Text="√" Visible='<%# Eval("IsEnable") %>'
                                    Font-Bold="True" Font-Size="14pt" ForeColor="Green"></asp:Label>
                                <asp:Label ID="IsEnable2" runat="server" Text="×" Visible='<%# !(Boolean)Eval("IsEnable") %>'
                                    Font-Bold="True" Font-Size="16pt" ForeColor="Red"></asp:Label>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:TemplateField>
                        <XCL:LinkBoxField HeaderText="编辑" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="UserForm.aspx?ID={0}"
                            Height="211px" Text="编辑" Width="440px" Title="编辑用户">
                            <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" />
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" Width="30px" />
                        </XCL:LinkBoxField>
                        <asp:TemplateField ShowHeader="False" HeaderText="删除">
                            <ItemTemplate>
                                <asp:LinkButton ID="btnDelete" runat="server" CausesValidation="False" CommandName="Delete"
                                    OnClientClick='return confirm("确定删除吗？")' Text="删除"></asp:LinkButton>
                            </ItemTemplate>
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" Width="30px" />
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        没有符合条件的数据！
                    </EmptyDataTemplate>
                </asp:GridView>
            </div>
        </div>
    </div>
    <asp:ObjectDataSource ID="ods" runat="server" EnablePaging="True" SelectCountMethod="SearchCount"
        SelectMethod="Search" SortParameterName="orderClause" EnableViewState="false">
        <SelectParameters>
            <asp:ControlParameter ControlID="txtKey" Name="key" PropertyName="Text" Type="String" />
            <asp:Parameter Name="orderClause" Type="String" />
            <asp:Parameter Name="startRowIndex" Type="Int32" />
            <asp:Parameter Name="maximumRows" Type="Int32" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <XCL:GridViewExtender ID="gvExt" runat="server">
    </XCL:GridViewExtender>
</asp:Content>
