<%@ Page Title="权限管理" Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true"
    CodeFile="Role.aspx.cs" Inherits="Common_Role" %>

<asp:Content ID="g" ContentPlaceHolderID="H" runat="server">
    <style type="text/css">
        .btn
        {
            vertical-align: top;
        }
    </style>
</asp:Content>
<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <div class="row-fluid">
        <Custom:OpenForm BtText="添加权限" runat="server" ID="lbAdd" DialogWidth="500px" DialogHeight="154px"
            Url="RoleForm.aspx" Title="Role"></Custom:OpenForm>
        关键字：<asp:TextBox ID="txtKey" runat="server"></asp:TextBox>
        <asp:Button ID="btnSearch" runat="server" class="btn btn-primary" Text="查询" />
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
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:BoundField>
                        <asp:BoundField DataField="Name" HeaderText="角色名称" SortExpression="Name">
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" />
                        </asp:BoundField>
                        <asp:TemplateField HeaderText="编辑">
                            <ItemTemplate>
                                <Custom:OpenForm BtText="编辑" runat="server" ID="OpenForm1" DialogWidth="500px" DialogHeight="154px"
                                    Url='<%# "RoleForm.aspx?ID="+ Eval("ID")%>' Title="NewRole" IsButtonStyle="false">
                                </Custom:OpenForm>
                            </ItemTemplate>
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" Width="30px" />
                        </asp:TemplateField>
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
