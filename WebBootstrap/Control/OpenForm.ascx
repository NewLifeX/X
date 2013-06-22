<%@ Control Language="C#" AutoEventWireup="true" CodeFile="OpenForm.ascx.cs" Inherits="Control_OpenForm" %>
<script type="text/javascript">
    /*调整modal窗口的相关属性（如：窗体大小标题南）*/
    $(function () {
        $('#myModal').on('show', function () {
            var height = $('a#ModalBtn').attr('customdata-height');
            var width = $('a#ModalBtn').attr('customdata-width');

            $('#myModal').find('div.modal-body').css('max-height', height);
            $('#myModal').css('width', width);
        });

        $('myModal').on('hide', function () {
            $('.RealSave').click();
        });
    });

//    $(function () {
//        $('.btnSave').each(function () {
//            $(this).click(function () {
//                $('.RealSave').click();
//            });
//        });

//        $('.btnCopySave').each(function () {
//            $(this).click(function () {
//                $('.CopySave').click();
//            });
//        });
//    });
</script>
<a href="<%= Url %>" role="button" class='<%= IsButtonStyle?"btn":"" %>' data-toggle="modal"
    data-target="#myModal" customdata-width="<%= DialogWidth %>" customdata-height="<%= DialogHeight %>"
    id="ModalBtn">
    <%= BtText%></a>
<div id="myModal" class="modal hide fade" tabindex="-1" role="dialog" aria-labelledby="myModalLabel"
    aria-hidden="true">
    <div class="modal-header">
        <button type="button" class="close" data-dismiss="modal" aria-hidden="true">
            ×</button>
        <h3 id="myModalLabel">
            <%= Title%></h3>
    </div>
    <div class="modal-body">
        <p>
            Loading...</p>
    </div>
   <%-- <div class="modal-footer">
        <button class="btn" data-dismiss="modal" aria-hidden="true">
            关闭</button>
        <a class="btn btn-primary btnSave" data-dismiss="modal" aria-hidden="true">保存</a> <a class="btn btn-info btnCopySave">另存为</a>
    </div>--%>
</div>
