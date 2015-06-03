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
                    //console.log('delete');
                    confirmDialog('确认删除？', function () {
                        $.get(that.attr('href'),
                          function (data) {
                              //console.log(data);

                              tips(data.msg, 1, 1000, 'iframe|' + data.url);

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
});
