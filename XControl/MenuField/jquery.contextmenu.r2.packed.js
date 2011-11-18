/*
* 修改自：http://www.trendskitchens.co.nz/jquery/contextmenu/
*
*/
(function ($) {
    var menu, shadow, trigger, content, hash, currentTarget, bgIframe;
    var defaults = {
        menuStyle: {
            listStyle: 'none',
            padding: '1px',
            margin: '0px',
            backgroundColor: '#fff',
            border: '1px solid #999',
            width: '100px'
        },
        itemStyle: {
            margin: '0px',
            color: '#000',
            display: 'block',
            cursor: 'default',
            padding: '3px',
            border: '1px solid #fff',
            backgroundColor: 'transparent'
        },
        itemHoverStyle: {
            border: '1px solid #0a246a',
            backgroundColor: '#b6bdd2'
        },
        itemHoverCssClass: 'menuitemhover',
        eventPosX: 'pageX',
        eventPosY: 'pageY',
        shadow: true,
        onContextMenu: null,
        onShowMenu: null,
        onBinding: null,
        triggerEvent: "contextmenu", //"click","onmouseover"...
        menuOffset: "mouse", //"under"
        autoFill: false
    };
    $.fn.contextMenu = function (target, options) {
        if (!menu) { // Create singleton menu
            menu = $('<div id="jqContextMenu"></div>')
.hide()
.css({ position: 'absolute', zIndex: '500' })
.appendTo('body')
.bind('click', function (e) {
    e.stopPropagation();
});
        }
        if (!shadow) {
            shadow = $('<div></div>')
.css({ backgroundColor: '#000', position: 'absolute', opacity: 0.2, zIndex: 499 })
.appendTo('body')
.hide();
        }
        if (!bgIframe) {
            bgIframe = $('<iframe id="iframe_contexmenuBg" width="100%" frameborder="0" style="position:absolute; top:0px; z-index:-1; border-style:none;"></iframe>');
        }
        hash = hash || [];
        options = options || {};
        hash.push({
            id: target,
            menuStyle: $.extend({}, defaults.menuStyle, options.menuStyle || {}),
            itemStyle: $.extend({}, defaults.itemStyle, options.itemStyle || {}),
            itemHoverStyle: $.extend({}, defaults.itemHoverStyle, options.itemHoverStyle || {}),
            bindings: options.bindings || {},
            shadow: options.shadow || options.shadow === false ? options.shadow : defaults.shadow,
            onContextMenu: options.onContextMenu || defaults.onContextMenu,
            onShowMenu: options.onShowMenu || defaults.onShowMenu,
            onBinding: options.onBinding || defaults.onBinding,
            eventPosX: options.eventPosX || defaults.eventPosX,
            eventPosY: options.eventPosY || defaults.eventPosY,
            triggerEvent: options.triggerEvent || defaults.triggerEvent,
            menuOffset: options.menuOffset || defaults.menuOffset,
            itemHoverCssClass: options.itemHoverCssClass || defaults.itemHoverCssClass
        });
        var index = hash.length - 1;
        return $(this).each(function () {
            if (hash[index].triggerEvent.toLowerCase() == "click") {
                $(this).click(function (e) {
                    if (menu.TriggerElement) { menu.TriggerElement.removeClass("contextMenuEleOn"); }
                    menu.TriggerElement = $(this);
                    menu.TriggerElement.addClass("contextMenuEleOn");
                    if (!hash[index].isShow) {
                        var bShowContext = (!!hash[index].onContextMenu) ? hash[index].onContextMenu(e) : true;
                        if (bShowContext) {
                            display(index, this, e, options);
                            for (var i = 0; i < hash.length; i++) { hash[i].isShow = false; }
                            hash[index].isShow = true;
                        }
                    } else { hide(); hash[index].isShow = false; }
                    return false;
                });
            } else {
                $(this).bind(hash[index].triggerEvent, function (e) {
                    // Check if onContextMenu() defined
                    var bShowContext = (!!hash[index].onContextMenu) ? hash[index].onContextMenu(e) : true;
                    if (bShowContext) display(index, this, e, options);
                    return false;
                });
            }
            if (hash[index].onBinding) { hash[index].onBinding(this); }
        });
    };
    function display(index, trigger, e, options) {
        var cur = hash[index];
        //content = $('#'+cur.id).find('ul:first').clone(true);
        content = $(cur.id).find('ul:first').clone(true); //修改为使用选择器而不只是ID
        content.find("ul.submenu").css(cur.menuStyle).addClass(content.attr("class"));
        content.css(cur.menuStyle).find('li:not(:empty)').css(cur.itemStyle).addClass(cur.itemCssClass).hover(
function () {
    $(this).css(cur.itemHoverStyle).addClass(cur.itemHoverCssClass);
    showSubmenu(this);
},
function () {
    $(this).css(cur.itemStyle).removeClass(cur.itemHoverCssClass);
    $(this).children("ul.submenu").hide();
}
).find('img').css({ verticalAlign: 'middle', paddingRight: '2px' });
        // Send the content to the menu
        menu.html(content);
        menu.append(bgIframe.height(menu.height()));
        // if there's an onShowMenu, run it now -- must run after content has been added
        // if you try to alter the content variable before the menu.html(), IE6 has issues
        // updating the content
        if (!!cur.onShowMenu) menu = cur.onShowMenu(e, menu);
        $.each(cur.bindings, function (id, func) {
            $(id, menu).bind('click', function (e) {
                hide();
                func(trigger, currentTarget);
            });
        });
        var left, top;
        switch (hash[index].menuOffset) {
            case "mouse": //根据鼠标位置定位
                left = e[cur.eventPosX];
                top = e[cur.eventPosY];
                break;
            case "under": //显示在触发元素的下方
                left = $(trigger).offset().left;
                top = $(trigger).offset().top + $(trigger).height() + Number($(trigger).css("padding-bottom").replace("px", "")) * 2 + 2;
                break;
            default:
                left = e[cur.eventPosX];
                top = e[cur.eventPosY];
        }
        menu.css({ 'left': left, 'top': top }).show();
        menu.width(menu.find("ul:first").width());
        if (cur.shadow) shadow.css({ width: menu.width(), height: menu.height(), left: menu.offset().left + 2, top: menu.offset().top + 2 }).show();
        $(document).one('click', hide);
    }
    function showSubmenu(parent) {
        var p = $(parent);
        submenu = p.children("ul.submenu");
        if (submenu.length > 0) {
            submenu.show();
            //offset=p.offset();
            submenu.css({ top: parent.offsetTop, left: p.width() });
        }
    }
    function hide() {
        menu.hide();
        shadow.hide();
        menu.TriggerElement.removeClass("contextMenuEleOn");
        for (var i = 0; i < hash.length; i++) { hash[i].isShow = false; }
    }
    // Apply defaults
    $.contextMenu = {
        defaults: function (userDefaults) {
            $.each(userDefaults, function (i, val) {
                if (typeof val == 'object' && defaults[i]) {
                    $.extend(defaults[i], val);
                }
                else defaults[i] = val;
            });
        }
    };
})(jQuery);
jQuery(function () {
    jQuery('div.contextMenu').hide();
});
