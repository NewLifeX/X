function switchLang(lang) {
    document.cookie = "UserLang=" + lang + "; expires=" + (function () {
        var d = +new Date();
        d += 30 * 24 * 60 * 60 * 1000;
        return new Date(d).toGMTString();
    })() + "; path=/;";

    location.reload(true);
}

jQuery(function ($) {
    if (/zh-TW/gi.test(getCookie('UserLang'))) {
        currentEncoding = 2;
        targetEncoding = 1;
        translateBody();
    }

    var list = $('.topbar .language .language-switcher .language-list');
    $('.topbar .language .language-switcher').hover(function () {
        list.stop(true,true).fadeIn();
    }, function () {
        list.fadeOut('fast').queue(function () { $(this).stop(true,true); });
    });
    list.find('li').hover(function () {
        $(this).addClass('language-hover');
    }, function () {
        $(this).removeClass('language-hover');
    });
});