<%@ page language="C#" autoeventwireup="true" codefile="SysConfig.aspx.cs" inherits="Admin_SysConfig"
    title="系统配置" masterpagefile="~/Admin/ManagerPage.master" %>

<%@ import namespace="NewLife.Reflection" %>

<asp:content id="Content1" contentplaceholderid="C" runat="server">
    <%--<link href="../../bootstrap/css/bootstrap-theme.min.css" rel="stylesheet" />
    <link href="../../bootstrap/css/bootstrap.min.css" rel="stylesheet" />--%>

    <%--    <link href="../../AmazeUI-2.2.1/assets/css/amazeui.css" rel="stylesheet" />
    <link href="../../AmazeUI-2.2.1/assets/css/admin.css" rel="stylesheet" />--%>
    <%-- <link href="../../bootstrap-3.3.2/css/bootstrap-theme.css" rel="stylesheet" />
    <link href="../../bootstrap-3.3.2/css/bootstrap.min.css" rel="stylesheet" />--%>
    <script src="../../bootstrap-3.3.2/js/bootstrap.js"></script>



    <div class="col-lg-12">
        <h4 class="page-header">系统配置</h4>
    </div>


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

        <asp:button id="btnSave" runat="server" cssclass="btn btn-primary" causesvalidation="True" text='保存'
            onclick="btnSave_Click" />
    </div>



</asp:content>
