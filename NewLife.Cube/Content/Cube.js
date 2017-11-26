$(function () {

    window.infoDialog = parent['infoDialog'] || function (title, msg) { alert(msg); };
    window.confirmDialog = parent['confirmDialog'] || function (msg, func) { if(confirm(msg)) func(); };
    window.tips = parent['tips'] || function (msg, modal, time, jumpUrl) { alert(msg); location.reload(); };

    //为所有data-action加上事件
    $('[data-action]').each(function () {
        $(this).data('click', this.onclick);
        this.onclick = null;
    });

    $(document).on('click', '[data-action]', function (e) {
        
        var that = $(this),
            oldClick = that.data('click');
        isAjax = that.data('ajax');
        action = that.data('action');
        if (!!isAjax) {
            //如果标志为ajax，阻止默认事件，
            this.onclick = null;
            e.stopPropagation();
            e.preventDefault();
            switch (action) {
                case 'view':
                    break;
                case 'edit':
                    break;
                case 'delete':
                    confirmDialog('确认删除？', function () {
                        $.get(that.attr('href'),
                          function (data) {
                              var url  = 'iframe|' + data.url;
                              if (data.code != 0) {
                                  url = null;
                              } 
                              tips(data.msg, 0, 1000, url );

                          }).error(function (error) {
                              infoDialog('提示', error || '删除失败', 1);
                          });
                    });

                    break;
                case 'add':
                    break;
            }

            return false;
        } else {

            oldClick && oldClick.call(this);
        }
    });

    //批量操作
    var $toolbarContext = $('.toolbar-batch'),
        $table = $('.table');

    //按钮事件
    $toolbarContext.on('click',
        'button[data-action], input[data-action], a[data-action]',
        function (e) {
            var fields = $(this).attr('data-fields');            
            //参数
            var parameter = '';
            if (fields.length == 0 || fields == '' || fields == undefined) {
                var tempstr = '';
                var fieldArr = fields.split(',');
                for (var i = 0; i < fieldArr.length; i++) {
                    tempstr = '';
                    if (fieldArr[i] == 'q') {
                        tempstr = tempstr + '=' + $('#q').val();
                    } else {
                        var detailArr = $('[name=' + fieldArr[i] + ']');
                        for (var j = 0; j < detailArr.length; j++) {
                            if (detailArr[j].tagName == 'INPUT') {
                                if (tempstr.indexOf(detailArr[j].val()) < 0) {
                                    tempstr += detailArr[j].val() + ',';
                                }
                            } 
                        }
                    }
                    parameter += fieldArr[i] + '=' + tempstr.substr(0, tempstr.length - 1) + '&';
                }
            }
            //method
            var method = 'POST';
            if ($(this).attr('data-method') != undefined && $(this).attr('data-method') == '') {
                method = 'GET';
            }
            //url
            var url = '';
            if ($(this).attr('data-url') == undefined && $(this).attr('data-url') == '') {
                if ($(this).tagName == 'A') {
                    url = $(this).href;
                }
            } else {
                url = $(this).attr('data-url');
            }
            
            doAction(method, url, parameter);

            //阻止按钮本身的事件冒泡
            return false;

        });
});

//ajax请求 methodName 指定GET与POST
function doAction(methodName, actionUrl, actionParamter) {
    if (methodName == '' || methodName == undefined || actionUrl == '' || actionUrl == undefined || actionParamter == '' || actionParamter == undefined) {
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
            tips(result.message, 0, 1000);
            console.log(result);
            if (result.url != undefined && result.url != '') {
                window.location.href = result.url;
            }
        }
    });
}

