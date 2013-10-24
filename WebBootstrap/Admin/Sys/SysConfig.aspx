<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SysConfig.aspx.cs" Inherits="Admin_SysConfig"
    Title="系统配置" MasterPageFile="~/MasterPage.master" %>

<%@ Import Namespace="NewLife.Reflection" %>
<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <div class="row-fluid">
        <div class="widget-box">
            <div class="widget-title">
                <span class="icon"><i class="icon-th"></i></span>
                <h5>
                    系统配置</h5>
            </div>
            <div class="widget-content nopadding">
                <table class="table table-bordered table-striped">
                    <tbody>
                        <% 
                            foreach (System.Reflection.PropertyInfo pi in GetProperties())
                            {
                                String pname = pi.Name;
                                String frmName = "frm" + pname;
                                TypeCode code = Type.GetTypeCode(pi.PropertyType);
                        %><tr>
                            <td align="right">
                                <%=GetDisplayName(pi)%>：
                            </td>
                            <td style="width: 200px;">
                                <%
                                    if (code == TypeCode.String)
                                    {
                                        if (pname.Equals("email", StringComparison.OrdinalIgnoreCase) || pname.Equals("mail", StringComparison.OrdinalIgnoreCase))
                                        {
                                %><input name="<%=frmName%>" type="text" value="<%=pi.GetValue(Config) %>" id="<%=frmName%>"
                                    onblur="return ValidMail();" style="border-color: Black; border-width: 0px; border-style: Solid;
                                    font-size: 10pt; width: 120px; border-bottom-width: 1px;" /><%
}
                                        else
                                        {
                                    %><input name="<%=frmName%>" type="text" value="<%=pi.GetValue(Config) %>" id="<%=frmName%>"
                                        style="width: 250px;" /><%
                                                                    }
                                    }
                                    else if (code == TypeCode.Int32)
                                    {
                                        %><input name="<%=frmName%>" type="text" value="<%=pi.GetValue(Config) %>" id="<%=frmName%>"
                                            style="width: 150px;" /><%
                                                                        }
                                    else if (code == TypeCode.Double)
                                    {
                                            %><input name="<%=frmName%>" type="text" value="<%=pi.GetValue(Config) %>" id="<%=frmName%>"
                                                style="width: 150px;" /><%
                                                                            }
                                    else if (code == TypeCode.DateTime)
                                    {
                                                %><input name="<%=frmName%>" type="text" value="<%=((DateTime)pi.GetValue(Config)).ToString("yyyy-MM-dd") %>"
                                                    id="<%=frmName%>" class="Wdate" onfocus="WdatePicker({autoPickDate:true,skin:'default',lang:'auto',readOnly:true})"
                                                    style="width: 86px;" /><%
                                                                               }
                                    else if (code == TypeCode.Decimal)
                                    {
                                                    %><input name="<%=frmName%>" type="text" value="<%=pi.GetValue(Config) %>" id="<%=frmName%>"
                                                        style="width: 150px;" /><%
                                                                                    }
                                    else if (code == TypeCode.Boolean)
                                    {
                                                        %><input id="<%=frmName%>" type="checkbox" name="<%=frmName%>" <%if((bool)pi.GetValue(Config)){ %>
                                                            checked="checked" <%} %> /><label for="<%=frmName%>"><%=pi.DisplayName%></label><%}
                                                            %>
                            </td>
                            <td>
                                <%=GetDescription(pi)%>
                            </td>
                        </tr>
                        <%}%>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
    <div class="row-fluid">
        <div class="widget-content nopadding">
            <div style="text-align: center">
                <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' CssClass="btn btn-info" />
            </div>
        </div>
    </div>
</asp:Content>
