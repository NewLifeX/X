<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="Center_Default" %>

<%@ Import Namespace="NewLife.CommonEntity" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>新生命管理平台</title>
    <script type="text/javascript" src="../Scripts/jquery-1.4.1.min.js"></script>
    <script type="text/javascript">
        if (window.top != window) window.top.location.href = window.location;
        $(document).ready(
        function () {
            modefHeight();

            $('#main_table').next('p').remove(); //移除form标签额外输出的p标签
        }
        )

        function modefHeight() {
            $("#leftiframe").height($(window).height() - $("#top").height() - $("#footer").height());
            $("#main").height($(window).height() - $("#top").height() - $("#footer").height());
            $("#main_table").height($(window).height());

        }

        function showleft() {
            var o = $("#td_left");
            if (o.css("display") == "none") {
                //o.fadeIn("slow");
                o.css("display", "")
            }
            else {
                //o.fadeOut("slow");
                o.css("display", "none")
            }
        }
    </script>
    <link href="images/css.css" rel="stylesheet" type="text/css" />
</head>
<body style="margin: 0px; padding: 0px; overflow: hidden" onresize="modefHeight();">
    <form id="form1" runat="server" style="margin: 0;">
    <table id="main_table" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #dfe8f6;">
        <tr>
            <td style="height: 70px; overflow: hidden" colspan="3" id="top" valign="bottom">
                <div style="padding-left: 30px; font-size: 20px; line-height: 35px; font-weight: bold;
                    color: #019401">
                    <%=SysSetting.DisplayName%> v<%=SysSetting.Version %><font style="font-size:12px; color:Blue;">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;——<%=SysSetting.Company%></font>
                </div>
                <div class="toolbar" style="height: 23px;">
                    <div style="float: left; padding-left: 10px;">
                        用户：<%=ManageProvider.Provider.Current.ToString()%>
                        <span style="color: Blue; cursor: pointer;" onclick="location='Default.aspx?act=logout'"
                            title="注销当前登录用户，回到登录界面！">注销</span>
                    </div>
                    <div style="float: right;">
                        <asp:Repeater runat="server" ID="menuItem">
                            <ItemTemplate>
                                <a href="Frame/Left.aspx?ID=<%# Eval("ID") %>" target="left" onclick="document.getElementById('main').src='<%# Eval("Url") %>';">
                                    <b>
                                        <%# Eval("Name") %></b></a>
                            </ItemTemplate>
                        </asp:Repeater>
                        <a href="Sys/AdminInfo.aspx?ID=<%=CommonManageProvider.Provider.Current.ID%>"
                            target="main" title="修改当前用户密码等信息！"><b>用户信息</b></a>
                    </div>
                </div>
            </td>
        </tr>
        <tr>
            <td width="147px" style="border: solid 1px #99bbe8; overflow: hidden;" id="td_left">
                <iframe height="100%" name="left" id="leftiframe" width="100%" frameborder="0" src="Frame/Left.aspx"
                    scrolling="no"></iframe>
            </td>
            <td style="border-top: solid 1px #99bbe8; width: 8px; overflow: hidden; cursor: pointer;"
                onclick="showleft(this)" title="显示或隐藏导航栏">
                &nbsp;
            </td>
            <td style="border: solid 1px #99bbe8" id="td_right">
                <iframe height="100%" width="100%" border="0" frameborder="0" id="main" name="main"
                    src="Main.aspx"></iframe>
            </td>
        </tr>
        <tr>
            <td style="background-color: #dfe8f6; height: 11px; overflow: hidden;" colspan="3"
                id="footer">
            </td>
        </tr>
    </table>
    </form>
</body>
</html>
