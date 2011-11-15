<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Left.aspx.cs" Inherits="Center_Frame_Left" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
    <link href="<%= ResolveUrl("~/css/reset.css")%>" rel="stylesheet" type="text/css" />
    <link href="../images/css.css" rel="stylesheet" type="text/css" />
    <style type="text/css">
        body
        {
            margin-left: 0px;
            margin-top: 0px;
            margin-right: 0px;
            margin-bottom: 0px;
            font-size: 13px;
        }
        
        a
        {
            text-decoration: none;
        }
        
        #menu
        {
            width: 147px;
            line-height: 25px;
        }
        #menu li
        {
            float: left;
            width: 100%;
            line-height: 25px;
        }
        
        
        #menu h3
        {
            cursor: pointer;
            overflow: hidden;
        }
        
        #menu div
        {
            float: left;
            height: 25px;
        }
        
        #menu h3 .dir-open
        {
            width: 16px;
            padding-right: 5px;
            background: url(../images/dir-open.gif) no-repeat 0px 5px;
        }
        
        #menu h3 .dir-close
        {
            width: 16px;
            padding-right: 5px;
            background: url(../images/dir-close.gif) no-repeat 0px 5px;
        }
        
        #menu h3 .icon-open
        {
            width: 10px;
            margin-left: 10px;
            background: url(../images/icon-open.gif) no-repeat 0px 9px;
        }
        
        #menu h3 .icon-close
        {
            width: 10px;
            margin-left: 10px;
            background: url(../images/icon-close.gif) no-repeat 0px 7px;
        }
        
        #menu .item
        {
            width: 20px;
            margin-left: 20px;
            background: url(../images/icon1.gif) no-repeat 0px 2px;
        }
        
        #menu ul
        {
            width: 100%;
        }
        
        .tdonmouseover
        {
            background-color: #eeeeee;
        }
        
        .tdselect
        {
            background-color: #d9e8fb;
        }
    </style>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/jquery-1.4.1.min.js")%>"></script>
    <script type="text/javascript">

        //var openmenu; //打开菜单
        //var openbox; //打开菜单

        function showcd(o) {
            var h = $(o);

            tdselect(o)

            if (h.children("div").first().attr("class") == "icon-open") {

                h.children("div:eq(1)").attr("class", "dir-close");
                h.children("div:eq(0)").attr("class", "icon-close");
                h.addClass("tdselect");
                h.parent().children("ul").slideUp("fast");
                return;
            }

            h.children("div:eq(1)").attr("class", "dir-open");
            h.children("div:eq(0)").attr("class", "icon-open");
            h.addClass("tdselect");
            h.parent().children("ul").slideDown("fast");
            return;
        }

        function tdselect(o) {
            $(".tdselect").removeClass("tdselect");
            $(o).addClass("tdselect");
        }


        function tdonmouseover(o) {
            $(o).addClass("tdonmouseover");
        }

        function tdonmouseout(o) {
            $(o).removeClass("tdonmouseover");
        }


        $(document).ready(
            function () {
                modefHeight();
            }
        )

        function modefHeight() {
            $("#menu").height($(window).height() - $(".toolbar").height() - 5);
        }
    </script>
</head>
<body onresize="modefHeight();">
    <form id="form1" runat="server">
    <div class="toolbar">
        <asp:Literal ID="Literal1" runat="server"></asp:Literal>
    </div>
    <asp:Repeater runat="server" ID="menu" OnItemDataBound="menu_ItemDataBound">
        <HeaderTemplate>
            <ul id="menu" style="height: 100px; overflow: auto;">
        </HeaderTemplate>
        <ItemTemplate>
            <li>
                <h3 onclick="showcd(this)" onmouseover="tdonmouseover(this)" onmouseout="tdonmouseout(this)">
                    <div class="icon-close">
                    </div>
                    <div class="dir-close">
                    </div>
                    <div class="title">
                        <%# Eval("Name") %></div>
                </h3>
                <ul id='cd_<%# Eval("ID") %>' style="display: none;">
                    <asp:Repeater runat="server" ID="menuItem">
                        <ItemTemplate>
                            <li onmouseover="tdonmouseover(this)" onmouseout="tdonmouseout(this)" onclick="tdselect(this)">
                                <div class="item">
                                </div>
                                <a href='<%# Eval("Url") %>' target="main" style="color: Black;">
                                    <%# Eval("Name") %></a> </li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ul>
            </li>
        </ItemTemplate>
        <FooterTemplate>
            </ul>
        </FooterTemplate>
    </asp:Repeater>
    </form>
</body>
</html>
