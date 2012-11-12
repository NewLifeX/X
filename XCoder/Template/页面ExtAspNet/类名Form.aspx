<%@ Page Language="C#" AutoEventWireup="true" CodeFile="<#=Table.Name#>Form.aspx.cs" Inherits="<#=Config.EntityConnName+"_"+Table.Name#>Form" Title="<#=Table.DisplayName#>管理"%>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <link href="../css/main.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <ext:PageManager ID="PageManager1" runat="server" />
    <ext:SimpleForm ID="SimpleForm1" ShowBorder="false" ShowHeader="false" runat="server"
        BodyPadding="5px" EnableBackgroundColor="true" Title="SimpleForm">
        <Toolbars>
            <ext:Toolbar ID="Toolbar1" runat="server">
                <Items>
                    <ext:Button ID="btnClose" Icon="SystemClose" EnablePostBack="false" runat="server"
                        Text="关闭">
                    </ext:Button>
                    <ext:ToolbarSeparator runat="server" />
                    <%--<ext:Button ID="btnSaveContinue" ValidateForms="SimpleForm1" Icon="SystemSaveNew"
                        OnClick="btnSaveContinue_Click" runat="server" Text="保存并继续">
                    </ext:Button>--%>
                    <ext:Button ID="btnSaveClose" ValidateForms="SimpleForm1" Icon="SystemSaveClose"
                        runat="server" Text="保存后关闭">
                    </ext:Button>
                </Items>
            </ext:Toolbar>
        </Toolbars>
        <Items><# 
        foreach(IDataColumn Field in Table.Columns) { 
            String pname = Field.Name;
            if(Field.PrimaryKey) continue;

            String frmName = "frm" + pname;
            TypeCode code = Type.GetTypeCode(Field.DataType);
        
            if(code == TypeCode.String){
                if(pname.Equals("Password", StringComparison.OrdinalIgnoreCase) || pname.Equals("Pass", StringComparison.OrdinalIgnoreCase)){
            #>
            <ext:TextBox ID="<#=frmName#>" Label="<#=Field.DisplayName#>" runat="server" TextMode="Password" Width="<#=Field.Length+100#>px" /><#
                }else if(Field.Length>300 || Field.Length<0){
            #>
            <ext:TextArea ID="<#=frmName#>" Label="<#=Field.DisplayName#>" runat="server" Width="300px" Height="80px" /><#
                }else{
            #>
            <ext:TextBox ID="<#=frmName#>" Label="<#=Field.DisplayName#>" runat="server" Width="<#=Field.Length+100#>px" /><#
                }
            }else if(code == TypeCode.Int32){
            #>
            <ext:NumberBox ID="<#=frmName#>" Label="<#=Field.DisplayName#>" runat="server" NoDecimal="true" Width="80px" /><#
            }else if(code == TypeCode.Double){
            #>
            <ext:NumberBox ID="<#=frmName#>" Label="<#=Field.DisplayName#>" runat="server" NoDecimal="false" Width="80px" /><#
            }else if(code == TypeCode.DateTime){
            #>
            <ext:DatePicker ID="<#=frmName#>" Label="<#=Field.DisplayName#>" runat="server" Width="150px" /><#
            }else if(code == TypeCode.Decimal){
            #>
            <ext:NumberBox ID="<#=frmName#>" Label="<#=Field.DisplayName#>" runat="server" NoDecimal="false" Width="80px" /><#
            }else if(code == TypeCode.Boolean){
            #>
            <ext:CheckBox ID="<#=frmName#>" Label="<#=Field.DisplayName#>" runat="server" Text="<#=Field.DisplayName#>" /><#}
        }#>
        </Items>
    </ext:SimpleForm>
    </form>
</body>
</html>