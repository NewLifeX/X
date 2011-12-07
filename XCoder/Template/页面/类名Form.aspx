<%@ Page Title="<#=Table.DisplayName#>管理" Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true" CodeFile="<#=Table.Alias#>Form.aspx.cs" Inherits="<#=Config.EntityConnName+"_"+Table.Alias#>Form"%>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2"><#=Table.DisplayName#></th>
        </tr>
        <# 
        foreach(IDataColumn Field in Table.Columns) { 
            String pname = Field.Alias;
            if(Field.PrimaryKey) continue;
            String frmName = "frm" + pname;
            TypeCode code = Type.GetTypeCode(Field.DataType);
        #><tr>
            <td align="right"><#=Field.DisplayName#>：</td>
            <td><#
                if(code == TypeCode.String){
                    if(pname.Equals("Password", StringComparison.OrdinalIgnoreCase) || pname.Equals("Pass", StringComparison.OrdinalIgnoreCase)){
                #><asp:TextBox ID="<#=frmName#>" runat="server" TextMode="Password"></asp:TextBox><#
                    }else if(Field.Length>300 || Field.Length<0){
                #><asp:TextBox ID="<#=frmName#>" runat="server" TextMode="MultiLine" Width="300px" Height="80px"></asp:TextBox><#
                    }else{
                #><asp:TextBox ID="<#=frmName#>" runat="server" Width="<#=Field.Length+100#>px"></asp:TextBox><#
                    }
                }else if(code == TypeCode.Int32){
                #><XCL:NumberBox ID="<#=frmName#>" runat="server" Width="80px"></XCL:NumberBox><#
                }else if(code == TypeCode.Double){
                #><XCL:RealBox ID="<#=frmName#>" runat="server" Width="80px"></XCL:RealBox><#
                }else if(code == TypeCode.DateTime){
                #><XCL:DateTimePicker ID="<#=frmName#>" runat="server"></XCL:DateTimePicker><#
                }else if(code == TypeCode.Decimal){
                #><XCL:DecimalBox ID="<#=frmName#>" runat="server" Width="80px"></XCL:DecimalBox><#
                }else if(code == TypeCode.Boolean){
                #><asp:CheckBox ID="<#=frmName#>" runat="server" Text="<#=Field.DisplayName#>" /><#}
            #></td>
        </tr>
<#}#>    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' />
                &nbsp;<asp:Button ID="btnReturn" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;" Text="返回" />
            </td>
        </tr>
    </table>
</asp:Content>