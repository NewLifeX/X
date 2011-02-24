<%@ Page Language="C#" AutoEventWireup="true" CodeFile="<#=ClassName#>.aspx.cs" Inherits="Pages_<#=ClassName#>" MasterPageFile="~/Admin/MasterPage.master" Title="<#=ClassDescription#>管理" %>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="ContentPlaceHolder1">
    <#
// 表单页面，普通行行高
Int32 LineHeight=27;
// 大文本行行高
Int32 LineHeight2=90;

// 表单窗口高度，初始值（Toolbar、表单头、提交按钮、运行时）
Int32 boxHeight = 31+29+29+14;

foreach(XField Field in Table.Fields){
    if(Field.PrimaryKey) continue;
    if(Field.DataType==typeof(String) && (Field.Length>300 || Field.Length<0))
        boxHeight += LineHeight2;
    else
        boxHeight += LineHeight;
}
#><div class="toolbar">
        <XCL:LinkBox ID="lbAdd" runat="server" BoxHeight="<#=boxHeight#>px" BoxWidth="440px" Url="<#=ClassName#>Form.aspx"
            IconLeft="../images/icons/icon005a2.gif"><b>添加<#=ClassDescription#></b></XCL:LinkBox>
        关键字：<asp:TextBox ID="txtKey" runat="server"></asp:TextBox>
        <asp:Button ID="btnSearch" runat="server" Text="查询" />
    </div>
    <asp:GridView ID="gv<#=ClassName#>" runat="server" AutoGenerateColumns="False" DataKeyNames="ID" DataSourceID="ObjectDataSource1" AllowPaging="True" AllowSorting="True" CssClass="m_table" PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True">
        <Columns><#
String PKName=null; 
foreach(XField Field in Table.Fields){
    String pname = GetPropertyName(Field);
    if(Field.PrimaryKey) { PKName=pname; } 
    if(Field.Identity){#>
            <asp:BoundField DataField="<#=pname#>" HeaderText="<#=GetPropertyDescription(Field)#>" SortExpression="<#=pname#>" <# if(Field.PrimaryKey){#>InsertVisible="False" ReadOnly="True" <#}#>>
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="60px" CssClass="key" />
            </asp:BoundField><#}
    else if(Field.DataType == typeof(DateTime)){#>
            <asp:BoundField DataField="<#=pname#>" HeaderText="<#=GetPropertyDescription(Field)#>" SortExpression="<#=pname#>" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" <# if(Field.PrimaryKey){#>InsertVisible="False" ReadOnly="True" <#}#>>
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="120px" />
            </asp:BoundField><#}
    // 密码字段和大文本字段不输出
    else if(!pname.Equals("Password", StringComparison.OrdinalIgnoreCase) && 
       !pname.Equals("Pass", StringComparison.OrdinalIgnoreCase) && 
       !pname.Equals("Pwd", StringComparison.OrdinalIgnoreCase) && 
       Field.Length>0 && Field.Length<300){#>
            <asp:BoundField DataField="<#=pname#>" HeaderText="<#=GetPropertyDescription(Field)#>" SortExpression="<#=pname#>" <# if(Field.PrimaryKey){#>InsertVisible="False" ReadOnly="True" <#}#>/><#
}}#>
            <XCL:LinkBoxField HeaderText="编辑" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="<#=ClassName#>Form.aspx?ID={0}" Height="<#=boxHeight#>px" Text="编辑" Width="440px" Title="编辑<#=ClassDescription#>">
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" />
                <HeaderStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="30px" />
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
    </asp:GridView>
    <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="<#=Config.NameSpace#>.<#=ClassName#>"
        DeleteMethod="Delete" EnablePaging="True" OldValuesParameterFormatString="original_{0}"
        SelectCountMethod="SearchCount" SelectMethod="Search" SortParameterName="orderClause"
        TypeName="<#=Config.NameSpace#>.<#=ClassName#>">
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