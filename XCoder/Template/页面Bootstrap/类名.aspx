<%@ Page Title="<#=Table.DisplayName#>管理" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="<#=Table.Name#>.aspx.cs" Inherits="<#=Config.EntityConnName+"_"+Table.Name#>" %>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <#
// 表单页面，普通行行高
Int32 LineHeight=27;
// 大文本行行高
Int32 LineHeight2=90;

// 表单窗口高度，初始值（Toolbar、表单头、提交按钮、运行时）
Int32 boxHeight = 100;

foreach(IDataColumn Field in Table.Columns){
    if(Field.PrimaryKey) continue;
    if(Field.DataType==typeof(String) && (Field.Length>300 || Field.Length<0))
        boxHeight += LineHeight2;
    else
        boxHeight += LineHeight;
}
#><div class="row-fluid">
        <Custom:OpenForm BtText="添加<#=Table.DisplayName#>" runat="server" ID="lbAdd" DialogWidth="500px" DialogHeight="<#=boxHeight#>px" Url="<#=Table.Name#>Form.aspx" Title="<#=Table.Name#>"></Custom:OpenForm>
        关键字：<asp:TextBox ID="txtKey" runat="server"></asp:TextBox>
        <asp:Button ID="btnSearch" runat="server" class="btn btn-info" Text="查询" />
    </div><#
StringBuilder sbpk=new StringBuilder();
StringBuilder sbpk2=new StringBuilder();
Int32 pki=0;
foreach(IDataColumn Field in Table.Columns){
    if(Field.PrimaryKey) {
        if(sbpk.Length>0)sbpk.Append(",");
        sbpk.Append(Field.Name);

        if(sbpk2.Length>0)sbpk2.Append("&");
        sbpk2.Append(Field.Name+"={"+pki+++"}");
    } 
}
    #><div class="row-fluid">
        <div class="widget-box">
        <div class="widget-content nopadding">
    <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="<#=sbpk#>" DataSourceID="ods" AllowPaging="True" AllowSorting="True" CssClass="table table-bordered table-striped" PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True">
        <Columns>
            <%--<asp:TemplateField>
                <ItemTemplate>
                    <asp:CheckBox ID="cb" runat="server" />
                </ItemTemplate>
                <HeaderStyle Width="20px" />
                <ItemStyle HorizontalAlign="Center" />
            </asp:TemplateField>--%><#
foreach(IDataColumn Field in Table.Columns){
    String pname = Field.Name;

    // 查找关系，如果对方有名为Name的字符串字段，则加一个扩展属性
    IDataRelation dr=XCode.DataAccessLayer.ModelHelper.GetRelation(Table, Field.ColumnName);
    if(dr!=null&&!dr.Unique){
        IDataTable rtable=FindTable(dr.RelationTable);
        if(rtable!=null){
            IDataColumn rname=rtable.GetColumn("Name");
            if(rname!=null&&rname.DataType==typeof(String)){#>
            <%--<asp:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortExpression="<#=pname#>" <# if(Field.PrimaryKey){#>InsertVisible="False" ReadOnly="True" <#}#>>
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" CssClass="key" />
            </asp:BoundField>--%>
            <asp:BoundField DataField="<#=rtable.Name+"Name"#>" HeaderText="<#=Field.DisplayName#>" SortExpression="<#=pname#>">
                <HeaderStyle CssClass="widget-title" Font-Size="Small" />
            </asp:BoundField>
            <#
                continue;
            }
        }
    }

    if(Field.Identity){#>
            <asp:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortExpression="<#=pname#>" <# if(Field.PrimaryKey){#>InsertVisible="False" ReadOnly="True" <#}#>>
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" CssClass="Ikey" />
                <HeaderStyle CssClass="widget-title" Font-Size="Small" />
            </asp:BoundField><#}
    else if(Field.DataType == typeof(DateTime)){#>
            <asp:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortExpression="<#=pname#>" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" <# if(Field.PrimaryKey){#>InsertVisible="False" ReadOnly="True" <#}#>>
                <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="120px" />
                <HeaderStyle CssClass="widget-title" Font-Size="Small" />
            </asp:BoundField><#}
    else if(Field.DataType == typeof(Decimal)){#>
            <asp:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortExpression="<#=pname#>" DataFormatString="{0:c}">
                <ItemStyle HorizontalAlign="Right" Font-Bold="True" ForeColor="Blue" />
                <HeaderStyle CssClass="widget-title" Font-Size="Small" />
            </asp:BoundField><#}
    else if(Type.GetTypeCode(Field.DataType)>=TypeCode.Int16&&Type.GetTypeCode(Field.DataType)<=TypeCode.UInt64){#>
            <asp:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortExpression="<#=pname#>" DataFormatString="{0:n0}">
                <ItemStyle HorizontalAlign="Right" Font-Bold="True" />
                <HeaderStyle CssClass="widget-title" Font-Size="Small" />
            </asp:BoundField><#}
    else if(Field.DataType == typeof(Boolean)){#>
            <asp:TemplateField HeaderText="<#=Field.DisplayName#>" SortExpression="<#=pname#>">
                <ItemTemplate>
                    <asp:Label ID="<#=pname#>1" runat="server" Text="√" Visible='<%# Eval("<#=pname#>") %>' Font-Bold="True" Font-Size="14pt" ForeColor="Green"></asp:Label>
                    <asp:Label ID="<#=pname#>2" runat="server" Text="×" Visible='<%# !(Boolean)Eval("<#=pname#>") %>' Font-Bold="True" Font-Size="16pt" ForeColor="Red"></asp:Label>
                </ItemTemplate>
                <ItemStyle HorizontalAlign="Center" />
                <HeaderStyle CssClass="widget-title" Font-Size="Small" />
            </asp:TemplateField><#}
    // 密码字段和大文本字段不输出
    else if(!pname.Equals("Password", StringComparison.OrdinalIgnoreCase) && 
       !pname.Equals("Pass", StringComparison.OrdinalIgnoreCase) && 
       !pname.Equals("Pwd", StringComparison.OrdinalIgnoreCase) && 
       Field.Length>0 && Field.Length<300){#>
            <asp:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortExpression="<#=pname#>" <# if(Field.PrimaryKey){#>InsertVisible="False" ReadOnly="True" <#}#>>
                <HeaderStyle CssClass="widget-title" Font-Size="Small" />
            </asp:BoundField><#
}}#>
            <asp:TemplateField HeaderText="编辑">
                            <ItemTemplate>
                                <Custom:OpenForm BtText="编辑" runat="server" ID="OpenForm1" DialogWidth="500px" DialogHeight="<#=boxHeight#>px"
                                    Url='<%# "<#=Table.Name#>Form.aspx?ID="+ Eval("ID")%>' Title="<#=Table.Name#>" IsButtonStyle="false">
                                </Custom:OpenForm>
                            </ItemTemplate>
                            <HeaderStyle CssClass="widget-title" Font-Size="Small" Width="30px" />
                        </asp:TemplateField>
            <asp:TemplateField ShowHeader="False" HeaderText="删除">
                <ItemTemplate>
                    <asp:LinkButton ID="btnDelete" runat="server" CausesValidation="False" CommandName="Delete" OnClientClick='return confirm("确定删除吗？")' Text="删除"></asp:LinkButton>
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
    <asp:ObjectDataSource ID="ods" runat="server" EnablePaging="True" SelectCountMethod="SearchCount" SelectMethod="Search" SortParameterName="orderClause" EnableViewState="false">
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