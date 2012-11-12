<%@ Page Language="C#" AutoEventWireup="true" CodeFile="<#=Table.Name#>.aspx.cs" Inherits="<#=Config.EntityConnName+"_"+Table.Name#>" Title="<#=Table.DisplayName#>管理" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <link href="../css/main.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <ext:PageManager ID="PageManager1" runat="server" />
    <#
// 表单页面，普通行行高
Int32 LineHeight=27;
// 大文本行行高
Int32 LineHeight2=90;

// 表单窗口高度，初始值（Toolbar、表单头、提交按钮、运行时）
Int32 boxHeight = 31+29+29+14;

foreach(IDataColumn Field in Table.Columns){
    if(Field.PrimaryKey) continue;
    if(Field.DataType==typeof(String) && (Field.Length>300 || Field.Length<0))
        boxHeight += LineHeight2;
    else
        boxHeight += LineHeight;
}

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
    #>
    <ext:Panel ID="pnl" runat="server" BodyPadding="5px" EnableLargeHeader="false" EnableBackgroundColor="true"
        ShowBorder="true" ShowHeader="true" Title="<#=Table.DisplayName#>" Layout="Anchor">
        <Items>
            <ext:Grid ID="gv" runat="server" DataSourceID="ods" ShowBorder="true" ShowHeader="false"
                AutoHeight="true" AllowPaging="true" EnableRowNumber="True" EnableCheckBoxSelect="True"
                DataKeyNames="<#=sbpk#>" AllowSorting="true">
                <Toolbars>
                    <ext:Toolbar ID="gvbar" runat="server">
                        <Items>
                            <ext:Button ID="btnNew" Text="新增数据" Icon="Add" EnablePostBack="false" runat="server">
                            </ext:Button>
                            <ext:Button ID="btnDelete" Text="删除选中项" Icon="Delete" OnClick="btnDelete_Click" runat="server"
                                ConfirmText="删除将不可恢复，是否删除？" ConfirmTitle="确认删除">
                            </ext:Button>
                            <ext:ToolbarSeparator runat="server" />
                            <ext:Label runat="server" Text="关键字：" />
                            <ext:TextBox ID="txtKey" runat="server" Label="关键字" EmptyText="关键字">
                            </ext:TextBox>
                            <ext:Button ID="btnSearch" runat="server" Text="查询" />
                        </Items>
                    </ext:Toolbar>
                </Toolbars>
                <Columns>
<#
// 列表页不宜显示过多列
Int32 fieldMaxCount=10;
foreach(IDataColumn Field in Table.Columns){
    String pname = Field.Name;
    if(fieldMaxCount--<=0) break;

    // 查找关系，如果对方有名为Name的字符串字段，则加一个扩展属性
    IDataRelation dr=ModelHelper.GetRelation(Table, Field.ColumnName);
    if(dr!=null&&!dr.Unique){
        IDataTable rtable=FindTable(dr.RelationTable);
        if(rtable!=null){
            IDataColumn rname=rtable.GetColumn("Name");
            if(rname!=null&&rname.DataType==typeof(String)){#>
                    <ext:BoundField DataField="<#=rtable.Name+"Name"#>" HeaderText="<#=Field.DisplayName#>" SortField="<#=pname#>" /><#
                continue;
            }
        }
    }

    if(Field.Identity){#>
                    <ext:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortField="<#=pname#>" Width="40px" /><#}
    else if(Field.DataType == typeof(DateTime)){#>
                    <ext:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortField="<#=pname#>" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" Width="140px" /><#}
    else if(Field.DataType == typeof(Decimal)){#>
                    <ext:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortField="<#=pname#>" DataFormatString="{0:c}" /><#}
    else if(Type.GetTypeCode(Field.DataType)>=TypeCode.Int16&&Type.GetTypeCode(Field.DataType)<=TypeCode.UInt64){#>
                    <ext:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortField="<#=pname#>" DataFormatString="{0:n0}" /><#}
    else if(Field.DataType == typeof(Boolean)){#>
                    <ext:CheckBoxField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortField="<#=pname#>" RenderAsStaticField="true" /><#}
    // 密码字段和大文本字段不输出
    else if(!pname.Equals("Password", StringComparison.OrdinalIgnoreCase) && 
       !pname.Equals("Pass", StringComparison.OrdinalIgnoreCase) && 
       !pname.Equals("Pwd", StringComparison.OrdinalIgnoreCase) && 
       Field.Length>0 && Field.Length<300){#>
                    <ext:BoundField DataField="<#=pname#>" HeaderText="<#=Field.DisplayName#>" SortField="<#=pname#>" /><#
}}#>
                    <ext:WindowField WindowID="win" HeaderText="编辑" Icon="TableEdit" ToolTip="编辑" DataIFrameUrlFields="<#=sbpk#>" DataIFrameUrlFormatString="<#=Table.Name#>Form.aspx?<#=sbpk2#>" Width="40px" />
                    <ext:LinkButtonField CommandName="Delete" HeaderText="删除" Text="删除" ConfirmText="删除将不可恢复，是否删除？"
                        ConfirmTitle="确认删除" Width="40px" />
                </Columns>
            </ext:Grid>
        </Items>
    </ext:Panel>
    <ext:Window ID="win" Title="编辑 - <#=Table.DisplayName#>" Popup="false" EnableIFrame="true" runat="server"
        CloseAction="HidePostBack" EnableConfirmOnClose="true" IFrameUrl="about:blank"
        Target="Top" IsModal="True" Width="750px" Height="450px">
    </ext:Window>
    <asp:ObjectDataSource ID="ods" runat="server" EnablePaging="True" SelectCountMethod="SearchCount" SelectMethod="Search" SortParameterName="orderClause" EnableViewState="false">
        <SelectParameters>
            <asp:ControlParameter ControlID="pnl$gv$gvbar$txtKey" Name="key" PropertyName="Text" Type="String" />
            <asp:Parameter Name="orderClause" Type="String" />
            <asp:Parameter Name="startRowIndex" Type="Int32" />
            <asp:Parameter Name="maximumRows" Type="Int32" />
        </SelectParameters>
    </asp:ObjectDataSource>
    </form>
</body>
</html>