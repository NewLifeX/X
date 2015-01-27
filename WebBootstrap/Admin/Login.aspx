<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Login.aspx.cs" Inherits="Admin_Login" %>

<!DOCTYPE html >
<html lang="en">
<head runat="server">
    <title>NewLife</title>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link rel="stylesheet" href="<%= ResolveUrl("~/UI/css/bootstrap.min.css")%>" type="text/css" />
    <link rel="stylesheet" href="<%= ResolveUrl("~/UI/css/bootstrap-responsive.min.css")%>"
        type="text/css" />
    <link rel="stylesheet" href="<%= ResolveUrl("~/UI/css/unicorn.login.css") %>" type="text/css" />
    <script src="../js/jquery.min.js" type="text/javascript"></script>
    <script src="../Scripts/Common.js" type="text/javascript"></script>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
</head>
<body>
    <div id="logo">
        <img src="../UI/img/logo.png" alt="" />
    </div>
    <div id="loginbox">
        <form id="loginform" class="form-vertical" runat="server">
            <p>
                请输入您的帐号及密码
            </p>
            <div class="control-group">
                <div class="controls">
                    <div class="input-prepend">
                        <span class="add-on"><i class="icon-user"></i></span>
                        <asp:TextBox ID="AcccountName" runat="server" placeholder="帐号"></asp:TextBox>
                    </div>
                </div>
            </div>
            <div class="control-group">
                <div class="controls">
                    <div class="input-prepend">
                        <span class="add-on"><i class="icon-lock"></i></span>
                        <asp:TextBox ID="Password" runat="server" placeholder="密码" TextMode="Password"></asp:TextBox>
                    </div>
                </div>
            </div>
            <div class="form-actions">
                <span class="pull-left"><a href="#" class="flip-link">忘记密码?</a></span> <span class="pull-center">
                    <asp:Label ID="errorMessage" runat="server" ForeColor="Red"></asp:Label></span>
                <span class="pull-right">
                    <asp:Button CssClass="btn btn-inverse" runat="server" Text="登录" ID="LoginBt" OnClientClick="return LoginCheckComplete();"
                        OnClick="LoginBt_Click" /></span>
            </div>
        </form>
    </div>
</body>
</html>
