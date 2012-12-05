<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SysConfig.aspx.cs" Inherits="Admin_SysConfig"
    Title="系统配置" %>
<%@ Import Namespace="NewLife.Reflection" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link href="../images/css.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
        <tr>
            <th colspan="3">系统配置</th>
        </tr>
        <% 
        foreach(PropertyInfoX pi in TypeX.Create(Config.GetType()).Properties) { 
            String pname = pi.Name;
            String frmName = "frm" + pname;
            TypeCode code = Type.GetTypeCode(pi.Type);
        %><tr>
            <td align="right"><%=pi.DisplayName%>：</td>
            <td style="width:200px;"><%
                if(code == TypeCode.String){
                    if(pname.Equals("email", StringComparison.OrdinalIgnoreCase) || pname.Equals("mail", StringComparison.OrdinalIgnoreCase)){
                %><input name="<%=frmName%>" type="text" value="<%=pi.GetValue(Config) %>" id="<%=frmName%>" onblur="return ValidMail();" style="border-color:Black;border-width:0px;border-style:Solid;font-size:10pt;width:120px;border-bottom-width:1px;" /><%
                    }else{
                %><input name="<%=frmName%>" type="text" value="<%=pi.GetValue(Config) %>" id="<%=frmName%>" style="width:150px;" /><%
                    }
                }else if(code == TypeCode.Int32){
                %><input name="<%=frmName%>" type="text" value="<%=pi.GetValue(Config) %>" id="<%=frmName%>" style="width:150px;" /><%
                }else if(code == TypeCode.Double){
                %><input name="<%=frmName%>" type="text" value="<%=pi.GetValue(Config) %>" id="<%=frmName%>" style="width:150px;" /><%
                }else if(code == TypeCode.DateTime){
                %><input name="<%=frmName%>" type="text" value="<%=((DateTime)pi.GetValue(Config)).ToString("yyyy-MM-dd") %>" id="<%=frmName%>" class="Wdate" onFocus="WdatePicker({autoPickDate:true,skin:'default',lang:'auto',readOnly:true})" style="width:86px;" /><%
                }else if(code == TypeCode.Decimal){
                %><input name="<%=frmName%>" type="text" value="<%=pi.GetValue(Config) %>" id="<%=frmName%>" style="width:150px;" /><%
                }else if(code == TypeCode.Boolean){
                %><input id="<%=frmName%>" type="checkbox" name="<%=frmName%>"<%if((bool)pi.GetValue(Config)){ %> checked="checked"<%} %> /><label for="<%=frmName%>"><%=pi.DisplayName%></label><%}
            %></td>
            <td><%=pi.Description%></td>
        </tr>
<%}%>    </table>
        <table border="0" align="Center" width="100%">
            <tr>
                <td align="center">
                    <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' 
                        onclick="btnSave_Click" />
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
