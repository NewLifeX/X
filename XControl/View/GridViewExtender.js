;
(function () {
    var w = window, d = document, bindedEvents = [], objectData;

    // 对象字典,可以将html元素和js变量关联起来,而不用在html元素中引用js元素
    function ObjectDict() {
        this.__data = [];
    }
    ObjectDict.prototype.get = function (n) {
        var i, d;
        for (i = 0; i < this.__data.length; i++) {
            d = this.__data[i];
            if (d[0] === n) {
                return d[1];
            }
        }
    };
    ObjectDict.prototype.set = function (n, v) {
        this.__data.push([n, v]);
        return this;
    };

    // 标签数组,提供类似html class属性的容器,使用push remove增删元素,使用last访问到末尾的元素,push会自动避免重复元素被加入
    function TagArray(equal) {
        this.__equal = equal;
    }
    TagArray.prototype = [];
    TagArray.prototype.push = function () {
        var i, j, args = [];
        for (i = 0; i < arguments.length; i++) {
            if (this.has(arguments[i])) {
                continue; // 已有的不添加
            }
            if (args.length > 0) {
                try {
                    for (j = 0; j < args.length; j++) {
                        if (this.__equal(arguments[i], args[j])) {
                            throw ''; // push参数中重复的不添加
                        }
                    }
                    args.push(arguments[i]);
                } catch (e) { }
            } else {
                args.push(arguments[i]);
            }
        }
        return Array.prototype.push.apply(this, args);
    };
    TagArray.prototype.remove = function (obj) {
        var i, s = 0, r = [];
        while (true) {
            i = this.has(obj, s);
            if (!i) {
                break;
            }
            s = i[0];
            r.push(this.splice(i[0], 1)[0]);
        }
        return r;
    };
    TagArray.prototype.has = function (obj, start) {
        var i;
        start = start || 0;
        for (i = start; i < this.length; i++) {
            if (this.__equal(obj, this[i])) {
                return [i, this[i]];
            }
        }
        return false;
    };
    TagArray.prototype.last = function () {
        if (this.length > 0) {
            return this[this.length - 1];
        }
    };

    function jsArray(arylike, s, n) {
        var ary, i;
        if (Object.prototype.toString.call(arylike) === '[object Array]') {
            return arylike;
        }
        if (typeof arylike.length === 'number') {
            if (typeof s === 'undefined') {
                s = 0;
            }
            if (s < 0) {
                s = arylike.length - s;
            }
            if (typeof n === 'undefined') {
                n = arylike.length;
            }
            if (n < 0) {
                n = arylike.length - n;
            }
            try {
                return Array.prototype.slice.call(arylike, s, n);
            } catch (e) {
                ary = new Array(arylike.length);
                for (i = s; i < n; i++) {
                    ary[i] = arylike[i];
                }
                return ary;
            }
        }
        return [];
    }

    function filter(ary, func) {
        var i, n;
        ary = jsArray(ary);
        n = ary.length;
        for (i = 0; i < n; i++) {
            if (func(ary[i], i)) {
                ary.push(ary[i]);
            }
        }
        ary.splice(0, n);
        return ary;
    }

    function bindEvent(ele, type, listener) {
        var bind;
        if (ele.attachEvent) {
            type = ('on' + type).toLowerCase();
            bind = 'attachEvent';
        } else if (ele.addEventListener) {
            bind = 'addEventListener';
        }
        if (bind) {
            ele[bind](type, listener);
            bindedEvents.push([ele, type, listener]);
        }
    }
    function unbindEvent(ele, type, listener) {
        var unbind;
        if (ele.detachEvent) {
            type = ('on' + type).toLowerCase();
            unbind = 'detachEvent';
        } else if (ele.removeEventListener) {
            unbind = 'removeEventListener';
        }
        if (unbind) {
            ele[unbind](type, listener);
        }
    }
    // 将指定函数包装后返回,指定函数在调用时会拥有返回函数执行的上下文,参数,以及会额外附加当前包装携带的参数
    function wrapFunc(func) {
        var params = jsArray(arguments, 1);
        return function () {
            func.apply(this, jsArray(arguments).concat(params));
        };
    }

    function nextColor(cols, c) {
        var i;
        for (i = 0; i < cols.length; i++) {
            if (cols[i].toLowerCase() === c.toLowerCase()) {
                return i + 1 < cols.length ? cols[i + 1] : cols[0];
            }
        }
    }
    function fireClickEvent(e) {
        var evt;
        if (e.click) {
            e.click();
        } else if (d.createEvent && e.dispatchEvent) {
            evt = d.createEvent('MouseEvents');
            evt.initMouseEvent('click', true, true, w, 0, 0, 0, 0, 0, false, false, false, false, 0, null);
            e.dispatchEvent(evt);
        }
    }

    function getOrInitObjectData(ele, name, deffunc) {
        var d = objectData.get(ele);
        if (typeof d === 'undefined') {
            d = {};
        }
        if (typeof d[name] === 'undefined') {
            d[name] = deffunc();
        }
        objectData.set(ele, d);
        return d[name];
    }
    function initBgColorArray() {
        return new TagArray(function (a, b) {
            return typeof a.t !== 'undefined' && a.t === b.t &&
                (a.c === b.c || typeof a.c === 'undefined');
        });
    }
    // 返回指定元素的背景颜色的标签数组,其元素约定为{t:类型,c:'颜色'} 类型可以是任意类型,添加和删除时必须提供类型,(不指定颜色的删除即删除所有这个类型的元素),颜色在添加时应该提供,否则将有可能添加不进去
    function getBgColorArray(ele) {
        return getOrInitObjectData(ele, 'BgColorArray', initBgColorArray);
    }

    objectData = new ObjectDict();
    w.GridViewExtender = {
        // 对指定id的GridView 表格中数据行执行指定的操作
        ExtendDataRow: function (id, opt) {
            var gv = d.getElementById(id), rows, cell, i, r, type;
            if (gv && gv.tagName === 'TABLE') {
                opt.EventMap = opt.EventMap || {};
                opt.HighlightRowMap = opt.HighlightRowMap || {};
                objectData.set(gv, opt);

                rows = gv.tBodies[0].rows;
                for (i = 0; i < rows.length; i++) {
                    cell = rows[i].firstChild;
                    while (cell !== null && cell.nodeType !== 1) {
                        cell = cell.nextSibling;
                    }
                    if (cell === null) {
                        continue;
                    }

                    if (cell.tagName === 'TD' && cell.colSpan === 1) { // 行的第一个标签是td 并且这个td没有单元格合并
                        r = rows[i];
                        for (type in opt.EventMap) {
                            if (opt.EventMap.hasOwnProperty(type)) {
                                bindEvent(r, type, wrapFunc(opt.EventMap[type], r, i));
                            }
                        }
                    }
                }
            }
        },
        IsInExtendDataRow: function (e) {
            var r, d;
            try {
                do {
                    if (e.tagName === 'TD') {
                        e = e.parentNode;
                    }
                    if (e.tagName === 'TR') {
                        r = e;
                        e = e.parentNode;
                    }
                    if (e.tagName === 'TABLE') {
                        d = objectData.get(e);
                        if (d && d.HighlightRowMap && d.EventMap) {
                            return [e, r];
                        }
                    }
                    e = e.parentNode;
                } while (e !== null && e.tagName !== 'BODY');
            } catch (ex) { }
            return null;
        },
        Highlight: function (colors) {
            if (typeof colors === 'string') {
                colors = colors.split(/\s*,\s*/);
            }
            if (colors.length === 1) {
                colors.push('');
            }
            return function (e, row, i) {
                var s = row.style, d = getBgColorArray(row), c = d.has({ t: colors }), addfunc = 'push';
                if (c) {
                    c[1].c = nextColor(colors, c[1].c);
                } else {
                    if (d.length > 0) {
                        addfunc = 'unshift';
                    }
                    d[addfunc]({ t: colors, c: colors[0] });
                }
                c = d.last();
                if (c) {
                    s.backgroundColor = c.c;
                }
            };
        },
        HighlightRow: function (ele, name, toggle) {
            var color, map, row, d, c, gv = w.GridViewExtender.IsInExtendDataRow(ele);
            if (gv && gv[0] && gv[1]) {
                row = gv[1];
                gv = gv[0];
                map = objectData.get(gv).HighlightRowMap;

                if (!name) {
                    name = '';
                }
                if (map[name]) {
                    color = map[name];
                } else {
                    color = name;
                }

                d = getBgColorArray(row);
                c = d.has({ t: 'HighlightRow' }); // TODO 考虑是否有需要提供分组,而不是使用单一的HighlightRow的组

                if (typeof toggle === 'undefined') {
                    toggle = !c || (c[1].c && color && c[1].c.toLowerCase() !== color.toLowerCase());
                }

                if (c) {
                    d.remove({ t: 'HighlightRow' });
                }
                if (toggle && typeof color !== 'undefined' && color !== '') {
                    d.push({ t: 'HighlightRow', c: color });
                }

                setTimeout(function () {
                    c = d.last();
                    row.style.backgroundColor = c ? c.c : '';
                }, 15);

            }
        },
        ClickElement: function (tag, func) {
            return function (e, row, i) {
                var ele, eles = filter(row.getElementsByTagName(tag), func);
                ele = eles[0];
                if (ele) {
                    fireClickEvent(ele);
                }
            };
        }
    };

    bindEvent(w, 'unload', function () {
        var i, args;
        for (i = 0; i < bindedEvents.length; i++) {
            args = bindedEvents[i];
            unbindEvent.apply(this, args);
        }
    });
}());
