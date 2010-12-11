<%@ Page Language="C#" AutoEventWireup="true" CodeFile="<#=ClassName#>.aspx.cs" Inherits="Pages_<#=ClassName#>" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title><#=ClassDescription#>管理</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div class="toolbar">
            <XCL:LinkBox ID="lbAdd" runat="server" BoxHeight="370px" BoxWidth="440px" Url="<#=ClassName#>Form.aspx"
                IconLeft="../images/icons/icon005a2.gif"><b>添加<#=ClassDescription#></b></XCL:LinkBox>
        </div>
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
            DataSourceID="ObjectDataSource1" AllowPaging="True" AllowSorting="True" CssClass="table" PageSize="20">
            <Columns><#
String PKName=null; 
foreach(XField Field in Table.Fields){
    String pname = GetPropertyName(Field);
    if(Field.PrimaryKey) { PKName=pname; } 
    // 密码字段和大文本字段不输出
    if(!pname.Equals("Password", StringComparison.OrdinalIgnoreCase) && 
       !pname.Equals("Pass", StringComparison.OrdinalIgnoreCase) && 
       Field.Length>0 && Field.Length<300){#>
                <asp:BoundField DataField="<#=pname#>" HeaderText="<#=GetPropertyDescription(Field)#>" SortExpression="<#=pname#>" <# if(Field.PrimaryKey){#>InsertVisible="False" ReadOnly="True" <#}#>/><#
}}#>
                <XCL:LinkBoxField HeaderText="编辑" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="<#=ClassName#>Form.aspx?ID={0}" Height="370px" Text="编辑" Width="440px" Title="编辑<#=ClassDescription#>">
                <ItemStyle HorizontalAlign="Center" />
                </XCL:LinkBoxField>
                <asp:TemplateField ShowHeader="False" HeaderText="删除">
                    <ItemTemplate>
                        <asp:LinkButton ID="btnDelete" runat="server" CausesValidation="False" CommandName="Delete"
                            OnClientClick='return confirm("确定删除吗？")' Text="删除"></asp:LinkButton>
                    </ItemTemplate>
                    <HeaderStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="30px" />
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                没有符合条件的数据！
            </EmptyDataTemplate>
            <PagerTemplate>
                共<asp:Label ID="lbTotalCount" runat="server" Text="<%# TotalCountStr %>"></asp:Label>条
                每页<asp:Label ID="lbPageSize" runat="server" Text="<%# ((GridView)Container.NamingContainer).PageSize %>"></asp:Label>条
                当前第
                <asp:Label ID="lbCurrentPage" runat="server" Text="<%# ((GridView)Container.NamingContainer).PageIndex + 1 %>"></asp:Label>
                页/共
                <asp:Label ID="lbPageCount" runat="server" Text="<%# ((GridView)Container.NamingContainer).PageCount %>"></asp:Label>
                页
                <asp:LinkButton ID="LinkButtonFirstPage" runat="server" CommandArgument="First" CommandName="Page"
                    Visible='<%#((GridView)Container.NamingContainer).PageIndex != 0 %>'>首页</asp:LinkButton>
                <asp:LinkButton ID="LinkButtonPreviousPage" runat="server" CommandArgument="Prev"
                    CommandName="Page" Visible='<%# ((GridView)Container.NamingContainer).PageIndex != 0 %>'>上一页</asp:LinkButton>
                <asp:LinkButton ID="LinkButtonNextPage" runat="server" CommandArgument="Next" CommandName="Page"
                    Visible='<%# ((GridView)Container.NamingContainer).PageIndex != ((GridView)Container.NamingContainer).PageCount - 1 %>'>下一页</asp:LinkButton>
                <asp:LinkButton ID="LinkButtonLastPage" runat="server" CommandArgument="Last" CommandName="Page"
                    Visible='<%# ((GridView)Container.NamingContainer).PageIndex != ((GridView)Container.NamingContainer).PageCount - 1 %>'>尾页</asp:LinkButton>
                转到第
                <input type="textbox" id="txtNewPageIndex" style="width: 40px;" value='<%# ((GridView)Container.Parent.Parent).PageIndex + 1 %>' />页
                <input type="button" id="btnGo" value="GO" onclick="javascript:__doPostBack('<%# ((GridView)Container.NamingContainer).UniqueID %>','Page$'+document.getElementById('txtNewPageIndex').value)" />
            </PagerTemplate>
        </asp:GridView>
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="<#=Config.NameSpace#>.<#=ClassName#>"
            DeleteMethod="Delete" EnablePaging="True" OldValuesParameterFormatString="original_{0}"
            SelectCountMethod="FindCountByName" SelectMethod="FindAllByName" SortParameterName="orderClause"
            TypeName="<#=Config.NameSpace#>.<#=ClassName#>" UpdateMethod="Update" OnSelected="ObjectDataSource1_Selected">
            <SelectParameters>
                <asp:Parameter Name="name" Type="String" />
                <asp:Parameter Name="value" Type="Object" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter Name="startRowIndex" Type="Int32" />
                <asp:Parameter Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
    </div>
    </form>
</body>
</html>

<script runat="server">
    /// <summary>
    /// 总记录数
    /// </summary>
    public Int32 TotalCount = 0;

    /// <summary>
    /// 总记录数
    /// </summary>
    public String TotalCountStr { get { return TotalCount.ToString("n0"); } }

    /// <summary>
    /// 获取总记录数
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void ObjectDataSource1_Selected(object sender, ObjectDataSourceStatusEventArgs e)
    {
        if (e.ReturnValue is Int32) TotalCount = (Int32)e.ReturnValue;
    }
</script>