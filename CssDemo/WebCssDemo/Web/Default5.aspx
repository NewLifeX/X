<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default5.aspx.cs" Inherits="Default5" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <title></title>
    <script src="js/jquery-1.9.1.min.js" type="text/javascript"></script>
    <script src="js/bootstrap.min.js" type="text/javascript"></script>
<%--    <script src="js/bootstrap.js" type="text/javascript"></script>--%>
    <link href="css/bootstrap.min.css" rel="stylesheet" type="text/css" />
    <link href="UI/css/uniform.css" rel="stylesheet" type="text/css" />
    <link href="UI/css/unicorn.main.content.css" rel="stylesheet" type="text/css" />
    <link rel="stylesheet" href="UI/css/unicorn.main.css" />
    <link href="UI/css/select2.css" rel="stylesheet" type="text/css" />
    <script src="UI/js/unicorn.js" type="text/javascript"></script>
    <script src="UI/js/unicorn.form_common.js" type="text/javascript"></script>
    <%--<link href="css/bootstrap.css" rel="stylesheet" type="text/css" />--%>

    <script type="text/javascript">
        //        $(function () {
        //            $('.btn').click(function () {
        //                $('#modaltest').modal({
        //                    backdrop: false,
        //                    keyboard: false,
        //                    show: true,
        //                    remote: "Default3.aspx"
        //                });
        //            });
        //        });

        $(function () {
            $('modaltest').on("hide", function () {

            })
        })
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <a role="button" class="btn" data-toggle="modal" href="Default3.aspx" data-target="#modaltest">
        查看演示案例</a>
    <div class="modal hide fade" id="modaltest" _aria-labelledby="myModalLabel" aria-hidden="true">
        <div class="modal-header">
            <h3>
                测试标题</h3>
        </div>
        <div class="modal-body">
            <input type="text" name="tt" />
        </div>
        <div class="modal-footer">
            <button class="btn" _aria-hidden="true" id="closebtn" _data-dismiss="modal">
                关闭</button>
            <button class="btn btn-primary">
                Save changes</button>
        </div>
    </div>
    <div id="uniform-undefined" class="checker">
        <span>
            <input type="checkbox" style="opacity: 0;" />
        </span>
    </div>
    </form>
</body>
</html>
