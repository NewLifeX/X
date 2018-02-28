// 以下时间用于魔方判断是否需要更新脚本
// 2017-12-07 00:00:00

$(function () {

    window.infoDialog = parent['infoDialog'] || function (title, msg) { alert(msg); };
    window.confirmDialog = parent['confirmDialog'] || function (msg, func) { if (confirm(msg)) func(); };
    window.tips = parent['tips'] || function (msg, modal, time, jumpUrl) { alert(msg); location.reload(); };

    //根据data-action的值确定操作类型 action为请求后端执行业务操作，url为直接跳转指定url地址
    //按钮请求action
    $(document).on('click',
        'button[data-action="action"], input[data-action="action"], a[data-action="action"]',
        function (e) {
            $this = $(this);
            //动态设置标签参数
            var url = $this.attr('href');
            if (url && url.length > 0) {
                $this.data('url', url);
                $this.attr('href', 'javascript:void(0);');
            }

            var cf = $this.data('confirm');

            if (cf && cf.length > 0) {
                confirmDialog(cf, () => doClickAction($this));
                return false;
            }

            doClickAction($this);
            //阻止按钮本身的事件冒泡
            return false;
        });
    //直接执行Url地址
    $(document).on('click'
        , 'button[data-action="url"],input[data-action="url"],a[data-action="url"]'
        , function (data) {
            $this = $(this);
            var url = $this.attr('href');
            if (url && url.length > 0) {
                $this.data('url', url);
            }
            location = url;
        });
});

function doClickAction($this) {
    var fields = $this.data('fields');
    //参数
    var parameter = '';
    if (fields && fields.length > 0) {
        var fieldArr = fields.split(',');
        for (var i = 0; i < fieldArr.length; i++) {
            var detailArr = $('[name=' + fieldArr[i] + ']');
            //不对name容器标签进行限制，直接进行序列化
            //如果有特殊需求，可以再指定筛选器进行筛选
            parameter += ((parameter.length > 0 ? '&' : '') + detailArr.serialize());
        }
    }

    //method
    var cmethod = $this.data('method');
    var method = 'GET';
    if (cmethod && cmethod.length > 0) {
        method = cmethod;
    }

    //url
    var curl = $this.data('url');
    if (!curl || curl.length <= 0) {
        if ($this[0].tagName == 'A') {
            curl = $this.attr('href');
        }
    }
    doAction(method, curl, parameter);
}

//ajax请求 methodName 指定GET与POST
function doAction(methodName, actionUrl, actionParamter) {
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
            //console.log(ex);
        },
        beforeSend: function () {
            tips('正在操作中，请稍候...', 0, 2000);
        },
        success: function (s) {
            //console.log(s);
        },
        complete: function (result) {
            var rs = result.responseJSON;
            if (rs.data && rs.data.length > 0) {
                tips(rs.data, 0, 1000);
            }
            if (rs.url && rs.url.length > 0) {
                if (rs.url == '[refresh]') {
                    //刷新页面但不重新加载页面的所有静态资源
                    location.reload(false);
                } else {
                    window.location.href = rs.url;
                }
            }
        }
    });
}

