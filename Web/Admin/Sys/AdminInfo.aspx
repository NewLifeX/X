<%@ page language="C#" masterpagefile="~/Admin/ManagerPage.master" autoeventwireup="true"
    codefile="AdminInfo.aspx.cs" inherits="Pages_AdminInfo" title="用户信息" %>

<asp:content id="C" contentplaceholderid="C" runat="server">

    <div class="col-lg-12">
        <h4 class="page-header">用户信息</h4>
    </div>



    <div class="form-group">
        <label class="col-sm-2 control-label">
            名称：
        </label>
        <div class="col-sm-10">
            <asp:textbox id="frmName" class="form-control" runat="server" readonly="true"></asp:textbox>
            </label>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            密码：
        </label>
        <div class="col-sm-10">
            <asp:textbox id="frmPassword" class="form-control" runat="server" textmode="Password"></asp:textbox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            显示名：
        </label>
        <div class="col-sm-10">
            <asp:textbox id="frmDisplayName" class="form-control" runat="server"></asp:textbox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            角色：
        </label>
        <div class="col-sm-10">
            <asp:label id="frmRoleName" runat="server"></asp:label>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            登录次数：
        </label>
        <div class="col-sm-10">
            <asp:label id="frmLogins" runat="server"></asp:label>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            最后登录：
        </label>
        <div class="col-sm-10">
            <asp:label id="frmLastLogin" runat="server"></asp:label>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            最后登陆IP：
        </label>
        <div class="col-sm-10">
            <asp:label id="frmLastLoginIP" runat="server"></asp:label>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            QQ：
        </label>
        <div class="col-sm-10">
            <asp:textbox id="frmQQ" class="form-control"  runat="server"></asp:textbox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            MSN：
        </label>
        <div class="col-sm-10">
            <asp:textbox id="frmMSN" class="form-control" runat="server"></asp:textbox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            邮箱：
        </label>
        <div class="col-sm-10">
            <xcl:mailbox id="frmEmail" class="form-control" runat="server"></xcl:mailbox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            电话：
        </label>
        <div class="col-sm-10">
            <asp:textbox id="frmPhone" class="form-control" runat="server"></asp:textbox>
        </div>
    </div>
    <div class="form-group text-center">
        <asp:button id="btnSave" runat="server" cssclass="btn btn-primary" causesvalidation="True" text='保存' />
    </div>

   <script>
$(document).ready(function(){
$("input[type=text],input[type=password]").removeAttr("style").css("width","200px");
});
   </script>
</asp:content>
