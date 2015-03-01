<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Login.aspx.cs" Inherits="Admin_Login" %>

<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <title><%= NewLife.Common.SysConfig.Current.DisplayName %></title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link href="../bootstrap/css/bootstrap.min.css" rel="stylesheet">
    <style type="text/css">
        body {
            padding-top: 40px;
            padding-bottom: 40px;
            background-color: #eee;
        }

        .form-signin {
            max-width: 330px;
            padding: 15px;
            margin: 0 auto;
        }

            .form-signin .form-signin-heading,
            .form-signin .checkbox {
                margin-bottom: 10px;
            }

            .form-signin .checkbox {
                font-weight: normal;
            }

            .form-signin .form-control {
                position: relative;
                height: auto;
                -webkit-box-sizing: border-box;
                -moz-box-sizing: border-box;
                box-sizing: border-box;
                padding: 10px;
                font-size: 16px;
            }

                .form-signin .form-control:focus {
                    z-index: 2;
                }

            .form-signin input[type="email"] {
                margin-bottom: -1px;
                border-bottom-right-radius: 0;
                border-bottom-left-radius: 0;
            }

            .form-signin input[type="password"] {
                margin-bottom: 10px;
                border-top-left-radius: 0;
                border-top-right-radius: 0;
            }
    </style>
</head>
<body>
    <div class="container">
        <form class="form-signin" method="post">
            <h2 class="form-signin-heading"><%= NewLife.Common.SysConfig.Current.DisplayName %></h2>
            <label for="user" class="sr-only">用户名</label>
            <input type="text" name="user" class="form-control" placeholder="用户名" required autofocus>
            <label for="pass" class="sr-only">密码</label>
            <input type="password" name="pass" class="form-control" placeholder="密码" required>
            <div class="checkbox">
                <label>
                    <input type="checkbox" name="remember" value="true">
                    保存密码
                </label>
            </div>
            <button name="login" value="true" class="btn btn-lg btn-primary btn-block" type="submit">登录</button>
        </form>
    </div>
    <!-- /container -->
</body>
</html>
