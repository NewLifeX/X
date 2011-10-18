<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Feedliquor.aspx.cs" Inherits="Pages_Feedliquor"
    Title="液料规格" MasterPageFile="~/Admin/MasterPage.master" %>

<asp:Content ID="content1" runat="server" ContentPlaceHolderID="ContentPlaceHolder1">
    <div>
        <div class="toolbar">
            <XCL:LinkBox ID="Button2" runat="server" Url="FeedliquorForm.aspx" IconLeft="../images/icons/icon005a2.gif"
                BoxWidth="1020px" BoxHeight="500px"><b>添加液料规格</b></XCL:LinkBox>&nbsp; 客户：<asp:TextBox
                    ID="txtName" runat="server"></asp:TextBox>
            胶水组别：<asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>&nbsp;
            <asp:Button ID="Button1" runat="server" Text="查询" />
        </div>
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
            DataSourceID="ObjectDataSource1" AllowPaging="True" AllowSorting="True" CssClass="m_table"
            PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True">
            <Columns>
                <asp:BoundField DataField="ID" HeaderText="ID" SortExpression="ID" InsertVisible="False"
                    ReadOnly="True" />
                <XCL:LinkBoxField DataNavigateUrlFields="ID" DataNavigateUrlFormatString="FeedliquorForm.aspx?ID={0}"
                    DataTextField="ProductNo" HeaderText="产品编号" Height="500px" Width="1020px" />
                <asp:BoundField DataField="Manufacturer" HeaderText="制造商" SortExpression="Manufacturer" />
                <asp:BoundField DataField="Tel" HeaderText="联系电话" SortExpression="Tel" />
                <asp:BoundField DataField="Address" HeaderText="联系地址" SortExpression="Address" />
                <asp:BoundField DataField="CementGroup" HeaderText="胶水组别" SortExpression="CementGroup" />
                <XCL:LinkBoxField DataNavigateUrlFields="ID" DataNavigateUrlFormatString="FeedliquorForm.aspx?ID={0}"
                    Text="编辑" HeaderText="编辑" Height="500px" Width="1020px" Title="编辑">
                    <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="60px" />
                </XCL:LinkBoxField>
                <asp:TemplateField ShowHeader="False" HeaderText="删除">
                    <ItemTemplate>
                        <asp:LinkButton ID="LinkButton1" runat="server" CausesValidation="False" CommandName="Delete"
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
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="NewLife.YWS.Entities.Feedliquor"
            DeleteMethod="Delete" EnablePaging="True" OldValuesParameterFormatString="original_{0}"
            SelectCountMethod="SearchCount" SelectMethod="Search" SortParameterName="orderClause"
            TypeName="NewLife.YWS.Entities.Feedliquor">
            <SelectParameters>
                <asp:ControlParameter ControlID="txtName" Name="name" PropertyName="Text" Type="String" />
                <asp:ControlParameter ControlID="TextBox1" Name="cementGroup" PropertyName="Text"
                    Type="String" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter Name="startRowIndex" Type="Int32" />
                <asp:Parameter Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <XCL:GridViewExtender ID="gvExt" runat="server">
        </XCL:GridViewExtender>
    </div>
</asp:Content>
