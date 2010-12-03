<%@ Page Language="C#" AutoEventWireup="true" CodeFile="<#=ClassName#>Form.aspx.cs" Inherits="Pages_<#=ClassName#>Form" ValidateRequest="false" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title><#=ClassDescription#>管理</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:FormView ID="FormView1" runat="server" DataKeyNames="ID" DataSourceID="ObjectDataSource1"
            DefaultMode="Edit" OnItemUpdated="FormView1_ItemUpdated">
            <EditItemTemplate>
                <table><#
                String PKName=null;
                foreach(XField Field in Table.Fields)
                {
            String pname = GetPropertyName(Field);
            if(Field.PrimaryKey) { PKName=pname; continue; } 
            String frmName = "frm" + pname;
            TypeCode code = Type.GetTypeCode(Field.DataType);
                #>
                    <tr>
                        <td width="15%" align="right"><#=GetPropertyDescription(Field)#>：</td>
                        <td width="75%"><#
                            if(code == TypeCode.String){
                            #><asp:TextBox ID="<#=frmName#>" runat="server" Text='<%# Bind("<#=pname#>") %>'></asp:TextBox><#
                            }else if(code == TypeCode.Int32){
                            #><XCL:NumberBox ID="<#=frmName#>" runat="server" Text='<%# Bind("<#=pname#>") %>' Width="80px"></XCL:NumberBox><#
                            }else if(code == TypeCode.Double){
                            #><XCL:RealBox ID="<#=frmName#>" runat="server" Text='<%# Bind("<#=pname#>") %>' Width="80px"></XCL:RealBox><#
                            }else if(code == TypeCode.DateTime){
                            #><XCL:DateTimePicker ID="<#=frmName#>" runat="server" Text='<%# Bind("<#=pname#>") %>'></XCL:DateTimePicker><#
                            }else if(code == TypeCode.Decimal){
                            #><XCL:NumberBox ID="<#=frmName#>" runat="server" Text='<%# Bind("<#=pname#>") %>' Width="80px"></XCL:NumberBox><#
                            }else if(code == TypeCode.Boolean){
                            #><asp:CheckBox ID="<#=frmName#>" runat="server" Text="<#=GetPropertyDescription(Field)#>" Checked='<%# Bind("<#=pname#>") %>' /><#}
                        #></td>
                        <td>
                            &nbsp;
                        </td>
                    </tr>
                  <#}#>
                </table>
                <asp:Button ID="UpdateButton" runat="server" CausesValidation="True" CommandName="Update" Text='<%# EntityID>0?"更新":"新增" %>' />
                &nbsp;<asp:Button ID="UpdateCancelButton" runat="server" CausesValidation="False" CommandName="Cancel" Text="取消" />&nbsp;
                <asp:Button ID="Button2" runat="server" OnClientClick="history.go(-1);return false;" Text="返回" />
            </EditItemTemplate>
        </asp:FormView>
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="<#=Config.NameSpace#>.<#=ClassName#>"
            OldValuesParameterFormatString="original_{0}" SelectMethod="FindByKeyForEdit"
            TypeName="<#=Config.NameSpace#>.<#=ClassName#>" UpdateMethod="Save">
            <SelectParameters>
                <asp:QueryStringParameter Name="__<#=PKName#>" QueryStringField="<#=PKName#>" Type="String" />
            </SelectParameters>
        </asp:ObjectDataSource>
    </div>
    </form>
</body>
</html>

<script runat="server">
    protected void FormView1_ItemUpdated(object sender, FormViewUpdatedEventArgs e)
    {
        Int32 id = (Int32)e.Keys[0];
        if (id > 0)
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('成功！');", true);
        else
        {
            System.Collections.Generic.List<<#=Config.NameSpace#>.<#=ClassName#>> list = <#=Config.NameSpace#>.<#=ClassName#>.FindAll(null, <#=Config.NameSpace#>.<#=ClassName#>._.<#=PKName#> + " Desc", null, 0, 1);
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('成功！');location.href='<#=ClassName#>Form.aspx?ID=" + list[0].<#=PKName#> + "';", true);
        }
    }
</script>