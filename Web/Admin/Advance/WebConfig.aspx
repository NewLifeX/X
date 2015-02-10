<%@ page language="C#" autoeventwireup="true" codefile="WebConfig.aspx.cs" inherits="Admin_System_WebConfig"
    title="网站配置" masterpagefile="~/Admin/ManagerPage.master" enableeventvalidation="false" validaterequest="false" %>

<asp:content id="Content1" contentplaceholderid="C" runat="server">
    <br />
    <div class="form-group col-sm-12 text-center">
        <asp:button id="Button1" cssclass="btn btn-primary" runat="server" text="保存" onclick="Button1_Click"
            onclientclick="return confirm('直接修改配置文件将可能导致网站出错！确定保存？');" />
    </div>
    <div class="form-group col-sm-12 text-center">
        <asp:textbox id="txtLog" runat="server" height="600px" style="margin-left:20px;" cssclass="form-control glyphicon-text-width" textmode="MultiLine" width="90%"></asp:textbox>
    </div>
    <br />
</asp:content>
