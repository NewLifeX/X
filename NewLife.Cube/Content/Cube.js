$(function () {

    window.infoDialog = parent['infoDialog'] || function (title, msg) { alert(msg); };
    window.confirmDialog = parent['confirmDialog'] || function (msg, func) { if (confirm(msg)) func(); };
    window.tips = parent['tips'] || function (msg, modal, time, jumpUrl) { alert(msg); location.reload(); };

    //$.ajaxSetup({
    //    //url: "/xmlhttp/",
    //    //global: false,
    //    //type: "GET"
    //    X-Requested-With
    //});

    //为所有data-action加上事件
    //$('[data-action]').each(function () {
    //    $(this).data('click', this.onclick);
    //    this.onclick = null;
    //});

    //$(document).on('click', '[data-action]', function (e) {

    //    var that = $(this),
    //        oldClick = that.data('click');
    //    //isAjax = that.data('ajax');
    //    action = that.data('action');
    //    //if (!!isAjax) {
    //    //如果标志为ajax，阻止默认事件，
    //    //this.onclick = null;
    //    //e.stopPropagation();
    //    //e.preventDefault();
    //    //switch (action) {
    //    //    case 'view':
    //    //        break;
    //    //    case 'edit':
    //    //        break;
    //    //    case 'delete':
    //    //        confirmDialog('确认删除？', function () {
    //    //            $.get(that.attr('href'),
    //    //                function (data) {
    //    //                    var url = 'iframe|' + data.url;
    //    //                    if (data.code != 0) {
    //    //                        url = null;
    //    //                    }
    //    //                    tips(data.msg, 0, 1000, url);

    //    //                }).error(function (error) {
    //    //                    infoDialog('提示', error || '删除失败', 1);
    //    //                });
    //    //        });
    //    //        break;
    //    //    case 'add':
    //    //        break;
    //    //}

    //    //return false;
    //    //} else {

    //    //    oldClick && oldClick.call(this);
    //    //}
    //});

    //批量操作
    var $toolbarContext = $(document),
        $table = $('.table');

    //按钮事件
    $toolbarContext.on('click',
        'button[data-action], input[data-action], a[data-action]',
        function (e) {
            var fields = $(this).attr('data-fields');
            //参数
            var parameter = '';
            //if (fields.length == 0 || fields == '' || fields == undefined) {
            if (fields && fields.length > 0) {
                //var tempstr = '';
                var fieldArr = fields.split(',');
                for (var i = 0; i < fieldArr.length; i++) {
                    //tempstr = '';

                    var detailArr = $('[name=' + fieldArr[i] + ']');
                    //不对name容器标签进行限制，直接进行序列化
                    //如果有特殊需求，可以再指定筛选器进行筛选
                    parameter += ((parameter.length > 0 ? '&' : '') + detailArr.serialize());

                    //if (fieldArr[i] == 'q') {
                    //    //tempstr = tempstr + '=' + $('#q').val();
                    //    tempstr = ((tempstr.length > 0 ? '&' : '') + 'q=' + $('#q').val());
                    //} else {
                    //for (var j = 0; j < detailArr.length; j++) {
                    //    if (detailArr[j].tagName == 'INPUT') {
                    //        if (tempstr.indexOf(detailArr[j].val()) < 0) {
                    //            tempstr += detailArr[j].val() + ',';
                    //        }
                    //    }
                    //}
                    //}
                    //parameter += fieldArr[i] + '=' + tempstr.substr(0, tempstr.length - 1) + '&'; 
                }

            }

            //method
            var cmethod = $(this).data('method');
            var method = 'GET';
            if (cmethod && cmethod.length > 0) {
                method = cmethod;
            }

            //url
            var curl = $(this).data('url');

            if (!curl || curl.length <= 0) {
                if ($(this)[0].tagName == 'A') {
                    curl = $(this).attr('href');
                }
            }

            //if (curl && curl.length == 0) {
            //    if ($(this).tagName == 'A') {
            //        url = $(this).href;
            //    }
            //} else {
            //    url = curl;
            //}

            doAction(method, curl, parameter);

            //阻止按钮本身的事件冒泡
            return false;
        });
});

//ajax请求 methodName 指定GET与POST
function doAction(methodName, actionUrl, actionParamter) {
    //if (methodName == '' || methodName == undefined || actionUrl == '' || actionUrl == undefined || actionParamter == '' || actionParamter == undefined) {
    if (!methodName || methodName.length <= 0 || !actionUrl || actionUrl.length <= 0) {
        tips('请求参数异常，请保证请求的地址跟参数正确！', 0, 1000);
        return;
    }

    $.ajax({
        url: actionUrl,
        type: methodName,
        async: false,
        dataType: 'json',
        data: actionParamter,
        error: function (ex) {
            tips('请求异常！', 0, 1000);
            console.log(ex);
        },
        beforeSend: function () {
            tips('正在操作中，请稍候...', 0, 2000);
        },
        success: function (s) {
            console.log(s);
        },
        complete: function (result) {
            //tips(result.data, 0, 1000);
            if (result.responseJSON.data && result.responseJSON.data.length > 0) {
                tips(result.responseJSON.data, 0, 1000);
            }
            if (result.responseJSON.url && result.responseJSON.url.length > 0) {
                if (result.responseJSON.url == '[refresh]') {
                    location.reload();
                } else {
                    window.location.href = result.responseJSON.url;
                }
            }
        }
    });
}

