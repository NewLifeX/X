<%@ Page Language="C#" MasterPageFile="~/Admin/MasterPage.master" AutoEventWireup="true" CodeFile="<#=ClassName#>Form.aspx.cs" Inherits="<#=ClassName#>Form" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="2"><#=ClassDescription#></th>
        </tr>
        <# 
        String PKName=null; 
        foreach(XField Field in Table.Fields) { 
            String pname = GetPropertyName(Field);
            if(Field.PrimaryKey) { PKName=pname; continue; } 
            String frmName = "frm" + pname;
            TypeCode code = Type.GetTypeCode(Field.DataType);
        #><tr>
            <td width="15%" align="right"><#=GetPropertyDescription(Field)#>：</td>
            <td width="75%"><#
                if(code == TypeCode.String){
                #><asp:TextBox ID="<#=frmName#>" runat="server" Text='<%# Entity.<#=pname#> %>'></asp:TextBox><#
                }else if(code == TypeCode.Int32){
                #><XCL:NumberBox ID="<#=frmName#>" runat="server" Text='<%# Entity.<#=pname#> %>' Width="80px"></XCL:NumberBox><#
                }else if(code == TypeCode.Double){
                #><XCL:RealBox ID="<#=frmName#>" runat="server" Text='<%# Entity.<#=pname#> %>' Width="80px"></XCL:RealBox><#
                }else if(code == TypeCode.DateTime){
                #><XCL:DateTimePicker ID="<#=frmName#>" runat="server" Text='<%# Entity.<#=pname#> %>'></XCL:DateTimePicker><#
                }else if(code == TypeCode.Decimal){
                #><XCL:NumberBox ID="<#=frmName#>" runat="server" Text='<%# Entity.<#=pname#> %>' Width="80px"></XCL:NumberBox><#
                }else if(code == TypeCode.Boolean){
                #><asp:CheckBox ID="<#=frmName#>" runat="server" Text="<#=GetPropertyDescription(Field)#>" Checked='<%# Entity.<#=pname#> %>' /><#}
            #></td>
        </tr>
<#}#>    </table>
    <table border="0" align="Center" width="100%">
        <tr>
            <td align="center">
                <asp:Button ID="UpdateButton" runat="server" CausesValidation="True" Text='<%# EntityID>0?"更新":"新增" %>' OnClick="UpdateButton_Click" />
                &nbsp;<asp:Button ID="Button2" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;" Text="返回" />
            </td>
        </tr>
    </table>
</asp:Content>