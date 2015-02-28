<%@ Page Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true"
    CodeFile="AdminInfo.aspx.cs" Inherits="Pages_AdminInfo" Title="用户信息" %>

<asp:Content ID="C" ContentPlaceHolderID="C" runat="server">
    <%--<div class="col-lg-12">
        <h4 class="page-header">用户信息</h4>
    </div>--%>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            名称：
        </label>
        <div class="col-sm-10">
            <asp:TextBox ID="frmName" class="form-control" runat="server" ReadOnly="true"></asp:TextBox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            密码：
        </label>
        <div class="col-sm-10">
            <asp:TextBox ID="frmPassword_" class="form-control" runat="server" TextMode="Password"></asp:TextBox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            显示名：
        </label>
        <div class="col-sm-10">
            <asp:TextBox ID="frmDisplayName" class="form-control" runat="server"></asp:TextBox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            角色：
        </label>
        <div class="col-sm-10">
            <asp:Label ID="frmRoleName" runat="server"></asp:Label>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            QQ：
        </label>
        <div class="col-sm-10">
            <asp:TextBox ID="frmCode" class="form-control" runat="server"></asp:TextBox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            邮箱：
        </label>
        <div class="col-sm-10 input-group">
            <span class="input-group-addon">@</span>
            <XCL:MailBox ID="frmMail" class="form-control" runat="server"></XCL:MailBox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            电话：
        </label>
        <div class="col-sm-10">
            <asp:TextBox ID="frmPhone" class="form-control" runat="server"></asp:TextBox>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            登录次数：
        </label>
        <div class="col-sm-10">
            <asp:Label ID="frmLogins" runat="server"></asp:Label>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            最后登录：
        </label>
        <div class="col-sm-10">
            <asp:Label ID="frmLastLogin" runat="server"></asp:Label>
        </div>
    </div>
    <div class="form-group">
        <label class="col-sm-2 control-label">
            最后登陆IP：
        </label>
        <div class="col-sm-10">
            <asp:Label ID="frmLastLoginIP" runat="server"></asp:Label>
        </div>
    </div>
    <div class="form-group text-center">
        <asp:Button ID="btnSave" runat="server" CssClass="btn btn-primary" CausesValidation="True" Text='保存' />
    </div>

    <script>
        $(document).ready(function () {
            $("input[type=text],input[type=password]").removeAttr("style").css("width", "200px");
        });
    </script>
</asp:Content>
