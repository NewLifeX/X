$(document).ready(function() {
    var liarray = $('#sidebar li').not($('.submenu'));
    liarray.each(function() {
        $(this).find('a').click(function() {
            //检查当前a标签是否包含在子菜单中
            var lisubmenu = $(this).parents('li.submenu');

            if ($(this).parent().hasClass('active') === false) {
                //大于0说明此处查到了指定的li元素，该a标签也是包含在一个子菜单中
                if (lisubmenu.length > 0) {
                    //lisubmenu.find('.submenu').hasClass('active')
                    if (lisubmenu.hasClass('active')) {
                        lisubmenu.find('.active').removeClass('active');
                        $(this).parent('li').addClass('active');
                    } else {
                        moveClass();
                        lisubmenu.addClass('active').addClass('open');
                        $(this).parent('li').addClass('active');
                    }
                } else {
                    moveClass();
                    $(this).parent().addClass('active');
                }
            }
            //设置连接地址
            //			$.get($(this).attr('address'), function(data) {
            //				$('#content').html(data);
            //			});
            $('#maincontent').attr('src', $(this).attr('address'));
        });
    });


    
});

//移除active open 的css

function moveClass() {
    //检查是否有打开的多级菜单
    var lisubmenu = $('.submenu.active');

    if (lisubmenu.length > 0) {
        if (($(window).width() > 768) || ($(window).width() < 479)) {
            lisubmenu.find('ul').slideUp();
        } else {
            lisubmenu.find('ul').fadeOut(250);
        }
        lisubmenu.removeClass('open');
    }
    $('.active').removeClass('active');
}

/*检查登录数据是否为空*/
function LoginCheckComplete() {
    var name = $('#AcccountName');
    var pw = $('#Password');
    var b = true;

    if (!name.val()) {
        name.parents('div.control-group').eq(0).addClass('error');
        b = b && false;
    }
    if (!pw.val()) {
        pw.parents('div.control-group').eq(0).addClass('error');
        b = b && false;
    }
    return b;
}