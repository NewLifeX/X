<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Login.aspx.cs" Inherits="Login" %>
<%@ Register assembly="NewLife.Bootstrap" namespace="NewLife.Bootstrap.Controls" tagprefix="nbc" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <script src="content/js/jquery-2.1.3.min.js"></script>
    <script src="content/js/bootstrap.min.js"></script>
     <!-- Bootstrap -->
    <link href="content/css/bootstrap.min.css" rel="stylesheet" media="screen" /> 
    <style type="text/css">
        body {
            padding-top: 40px;
        }
    </style>   
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">      
            <nbc:Alert ID="Alert1" runat="server" AlertType="Error" Visible="false">
                <Content>
                    <strong>Erro!</strong> Verifique se introduziu correctamente o username e a password.
                </Content>
            </nbc:Alert>  
            <nbc:FieldSet ID="fieldSet1" runat="server" Legend="Introduza as suas credenciais">
                <Content>
                    <nbc:TextBox ID="txtUsername" runat="server" PlaceHolder="Username" />
                    <nbc:TextBox ID="txtPassword" runat="server" TextMode="Password" PlaceHolder="Password" />
                    <br />
                    <nbc:Button ID="btnOK" runat="server" Toogle="true" Text="Entrar" ButtonSize="Default" ButtonType="Primary" OnClick="btnOK_Click" />              
                </Content>
            </nbc:FieldSet>           
        </div>
    </form>
</body>
</html>
