<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SysConfig.aspx.cs" Inherits="Admin_SysConfig"
    Title="系统配置" MasterPageFile="~/Admin/ManagerPage.master" %>

<%@ Import Namespace="NewLife.Reflection" %>

<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <%--<div class="col-lg-12">
        <h4 class="page-header">系统配置</h4>
    </div>--%>
    <% 
        foreach (System.Reflection.PropertyInfo pi in GetProperties())
        {
            String pname = pi.Name;
            String frmName = "frm" + pname;
            TypeCode code = Type.GetTypeCode(pi.PropertyType);
    %>
    <div class="form-group">
        <label class="col-sm-2 control-label" for="<%=frmName%>"><%=GetDisplayName(pi)%>：</label>
        <div class="col-sm-10">
            <%
            if (code == TypeCode.String)
            {
                if (pname.Equals("email", StringComparison.OrdinalIgnoreCase) || pname.Equals("mail", StringComparison.OrdinalIgnoreCase))
                {
            %>
            <input name="<%=frmName%>" class="form-control" type="text" value="<%=pi.GetValue(Config, null) %>" style="width: 250px;" id="<%=frmName%>" onblur="return ValidMail();" /><%
                    }
                    else
                    {
            %><input name="<%=frmName%>" class="form-control" type="text" value="<%=pi.GetValue(Config, null) %>" id="<%=frmName%>" style="width: 250px;" /><%
                                                        }
                }
                else if (code == TypeCode.Int32)
                {
            %><input name="<%=frmName%>" class="form-control" type="text" value="<%=pi.GetValue(Config, null) %>" id="<%=frmName%>" style="width: 150px;" /><%
                                                                                                                                           }
                else if (code == TypeCode.Double)
                {
            %><input name="<%=frmName%>" class="form-control" type="text" value="<%=pi.GetValue(Config, null) %>" id="<%=frmName%>" style="width: 150px;" /><%
                                                                }
                else if (code == TypeCode.DateTime)
                {
            %><input name="<%=frmName%>" class="form-control" type="text" value="<%=((DateTime)pi.GetValue(Config, null)).ToString("yyyy-MM-dd") %>" id="<%=frmName%>" class="Wdate" onfocus="WdatePicker({autoPickDate:true,skin:'default',lang:'auto',readOnly:true})" style="width: 106px;" /><%
                                                                                                                                           }
                else if (code == TypeCode.Decimal)
                {
            %><input name="<%=frmName%>" class="form-control" type="text" value="<%=pi.GetValue(Config, null) %>" id="<%=frmName%>" style="width: 150px;" /><%
                                                                                                                                                                                                                                                                               }
                else if (code == TypeCode.Boolean)
                {
            %><input id="<%=frmName%>" type="checkbox" class="checkbox" name="<%=frmName%>" <%if ((bool)pi.GetValue(Config, null))
                                                                                              { %>
                checked="checked" <%} %> />
            <%}
            %>
            <span class="help-block  text-info"><%=GetDescription(pi)%></span>
        </div>
    </div>
    <%}%>
    <div class="form-group col-sm-12 text-center">

        <asp:Button ID="btnSave" runat="server" CssClass="btn btn-primary" CausesValidation="True" Text='保存'
            OnClick="btnSave_Click" />
    </div>
</asp:Content>
