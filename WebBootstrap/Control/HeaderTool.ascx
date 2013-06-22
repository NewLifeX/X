<%@ Control Language="C#" AutoEventWireup="true" CodeFile="HeaderTool.ascx.cs" Inherits="Control_HeaderTool" %>
<script type="text/javascript">
    $(function () {
        $('#UserInfo').click(function () {
            $('#maincontent').attr('src', 'AdminInfo.aspx');
        });
    });
</script>
<div id="user-nav" class="navbar navbar-inverse">
    <ul class="nav btn-group">
        <li class="btn btn-inverse"><a id="UserInfo" href="javascript:void(0);"><i class="icon icon-user"></i><span class="text">用户信息</span></a></li>
        <li class="btn btn-inverse dropdown" id="menu-messages"><a href="#" data-toggle="dropdown" data-target="#menu-messages" class="dropdown-toggle"><i class="icon icon-envelope"></i><span class="text">信息</span> <span class="label label-important">5</span><b class="caret"></b></a>
            <ul class="dropdown-menu">
                <li><a class="sAdd" title="" href="#">new message</a></li>
                <li><a class="sInbox" title="" href="#">inbox</a></li>
                <li><a class="sOutbox" title="" href="#">outbox</a></li>
                <li><a class="sTrash" title="" href="#">trash</a></li>
            </ul>
        </li>
        <li class="btn btn-inverse"><a title="" href="#"><i class="icon icon-cog"></i><span class="text">设置</span></a></li>
        <li class="btn btn-inverse"><a title="" href="javascript:void(0);" onclick="location='Default.aspx?act=logout'"><i class="icon icon-share-alt"></i><span class="text">退出</span></a></li>
    </ul>
</div>