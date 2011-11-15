<%@ Page Language="C#" AutoEventWireup="true" CodeFile="CustomerType.aspx.cs" Inherits="Pages_CustomerType"
    MasterPageFile="~/Admin/MasterPage.master" Title="客户类型管理" %>

<asp:Content ID="content1" runat="server" ContentPlaceHolderID="ContentPlaceHolder1">
    <div>
        <div class="toolbar">
            <XCL:LinkBox ID="lbAdd" runat="server" BoxHeight="370px" BoxWidth="440px" Url="CustomerTypeForm.aspx"
                IconLeft="~/Admin/images/icons/new.gif"><b>添加客户类型</b></XCL:LinkBox>
        </div>
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
            AllowPaging="True" AllowSorting="True" CssClass="m_table" CellPadding="0" GridLines="None"
            PageSize="20" EnableModelValidation="True" DataSourceID="ObjectDataSource1">
            <Columns>
                <asp:BoundField DataField="ID" HeaderText="编号" InsertVisible="False" ReadOnly="True"
                    SortExpression="ID">
                    <HeaderStyle Width="40px" />
                    <ItemStyle CssClass="key" HorizontalAlign="Center" />
                </asp:BoundField>
                <asp:TemplateField HeaderText="名称" SortExpression="Name">
                    <ItemTemplate>
                        <%# new String('　', (Convert.ToInt32(Eval("Deepth"))-1)*2)%><asp:Label ID="Label1"
                            runat="server" Text='<%# Eval("Name") %>'></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="ParentName" HeaderText="父类型" SortExpression="ParentID" />
                <asp:BoundField DataField="AddTime" HeaderText="添加时间" SortExpression="AddTime" />
                <asp:BoundField DataField="Operator2" HeaderText="添加人" SortExpression="Operator2" />
                <XCL:LinkBoxField HeaderText="编辑" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="CustomerTypeForm.aspx?ID={0}"
                    Height="370px" Text="编辑" Width="440px" Title="编辑客户类型">
                    <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="60px" />
                </XCL:LinkBoxField>
                <asp:TemplateField ShowHeader="False" HeaderText="删除">
                    <ItemTemplate>
                        <asp:LinkButton ID="btnDelete" runat="server" CausesValidation="False" CommandName="Delete"
                            OnClientClick='return confirm("确定删除吗？")' Text="删除"></asp:LinkButton>
                    </ItemTemplate>
                    <HeaderStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="30px" />
                    <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="60px" />
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                没有符合条件的数据！
            </EmptyDataTemplate>
        </asp:GridView>
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="NewLife.YWS.Entities.CustomerType"
            DeleteMethod="Delete" OldValuesParameterFormatString="original_{0}" SelectMethod="FindAllChildsByParent"
            TypeName="NewLife.YWS.Entities.CustomerType">
            <SelectParameters>
                <asp:Parameter DefaultValue="0" Name="parentKey" Type="Object" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <XCL:GridViewExtender ID="gvExt" runat="server">
        </XCL:GridViewExtender>
    </div>
</asp:Content>
