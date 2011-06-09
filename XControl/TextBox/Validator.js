// 按键事件处理工具,直接调用将返回一个工具实例,e参数是event参数,对于ie是window.event,对于ff是callback的第一个参数
// 需要注意ie下keypress不会因Backspace Delete Home End 方向键而触发,但是会因Esc Enter键而触发keypress事件
// ff下始终会触发,这里charCode和keyChar也因此而起
function keyPressUtil(e)
{
    if (this instanceof arguments.callee)
    {
        this.__event = e;
    } else
    {
        return new arguments.callee(e);
    }
}
// 指定的第一个参数是否在后续提供的集合之中,集合元素可以是function,将会把第一参数传入,需要返回true或false,比较将使用===的比较,需要提供理想的类型
keyPressUtil.codeIn = function ()
{
    var c = arguments[0];
    var codes = Array.prototype.slice.call(arguments, 1);
    if (c === undefined) return false;
    if (codes.length === 0) return false;

    for (var i = 0; i < codes.length; i++)
    {
        var v = codes[i];
        if (typeof v === 'function')
        {
            if (v.call(this, c))
            {
                return true;
            }
        } else if (v === c)
        {
            return true;
        }
    }
    return false;
};
// 返回第一个参数是否是控制键,实际键盘上>32的也有可能是控制键,比如方向键 Home End Delete
keyPressUtil.isControlKey = function (c)
{
    if (c < 32) return true;
    switch (c)
    {
        case 27: //Esc
        case 46: //Delete
        case 35: case 36: //Home End
        case 37: case 38: case 39: case 40: //方向键
            return true;
        default:
            return false
    }
};
// 当前按键事件的按键是否在指定的字符代码范围内,只有非控制按键才生效
keyPressUtil.prototype.charCodeIn = function ()
{
    var e = this.__event;
    var c;
    if (typeof e.charCode === 'undefined')
    {//ie没有charCode ff有charCode但是控制按键是charCode是0
        c = e.keyCode;
        if (c < 32) return false; //因为ie下部分控制按键会触发keypress
    } else
    {
        c = e.charCode;
    }
    if (c === 0) return false;
    return keyPressUtil.codeIn.apply(this, [c].concat(Array.prototype.slice.call(arguments, 0)));
};
// 当前按键事件的按键是否在指定的按键代码范围内,只有控制按键才生效
keyPressUtil.prototype.keyCodeIn = function ()
{
    var e = this.__event;
    var c;
    if (typeof e.charCode === 'undefined')
    {
        c = e.keyCode;
        if (c >= 32) return false;
    } else
    {
        c = e.keyCode;
    }
    if (c === 0) return false;
    return keyPressUtil.codeIn.apply(this, [c].concat(Array.prototype.slice.call(arguments, 0)));
};
//返回指定文本框当前的选中区域信息,并可用于设置新的选中区域
function getSelection(ele)
{
    if (this instanceof arguments.callee)
    {
        if (typeof document.selection !== 'undefined')
        {
            var dRng = document.selection.createRange();
            var eRng = ele.createTextRange();
            if (eRng.inRange(dRng))
            {
                eRng.collapse(true);
                eRng.setEndPoint('EndToStart', dRng);
                this.start = eRng.text.length;
                this.end = this.start + dRng.text.length;
            } else
            {
                this.start = this.end = ele.value.length; //如果当前没有选中区域,则默认为末尾
            }
        } else if (typeof ele.selectionStart !== 'undefined' && typeof ele.selectionEnd !== 'undefined')
        {
            this.start = ele.selectionStart;
            this.end = ele.selectionEnd;
        } else
        {
            throw 'Nonsupport getSelection';
        }
        this.__ele = ele;
    } else
    {
        try
        {
            return new arguments.callee(ele);
        } catch (ex)
        {
            if (ex === 'Nonsupport getSelection')
            {
                return null;
            } else
            {
                throw ex;
            }
        }
    }
}
// 将当前表示的文本内容选择区域设置为指定范围
getSelection.prototype.selectRange = function (start, end)
{
    var ele = this.__ele;
    if (typeof document.selection !== 'undefined')
    {
        var rng = ele.createTextRange();
        rng.collapse(true);
        rng.moveStart('character', start);
        rng.moveEnd('character', end - start);
        rng.select();
    } else if (typeof ele.setSelectionRange !== 'undefined')
    {
        ele.setSelectionRange(start, end);
    }
};
//验证数字时对减号的额外处理 这里是具体的处理过程,是否按下了减号键需要由调用方处理
function ValidNumberSubtract(ele)
{
    if (ele.value.indexOf("-") === -1)
    {
        var sel = getSelection(ele);
        ele.value = "-" + ele.value;
        sel.selectRange(sel.start + 1, sel.end + 1);
    }
}
//验证事件源的值是否符合指定的正则表达式
function ValidInput(reg)
{
    if (!reg) return true;
    var obj = GetEvent();
    if (!obj) return true;
    return reg.test(obj.value);
}

//验证数字
function ValidNumber()
{
    var obj = GetEvent();

    if (!obj) return true;
    var kutil = keyPressUtil(obj);
    var ret = kutil.charCodeIn(
            function (c) // 减号
            {
                if (c === 45) ValidNumberSubtract(obj.srcElement || obj.currentTarget);
                return false;
            },
            function (c) { return c >= 48 && c <= 57; } //数字
        ) ||
        kutil.keyCodeIn(keyPressUtil.isControlKey);
    return ret;
}
//失去焦点时，验证最小值
function ValidNumber2(min, max, step)
{

    var obj = GetEvent();

    if (!obj || !obj.value) return true;

    var value = parseInt(obj.value, 10);
    if (isNaN(value)) return true;

    //目前暂时不支持步进
    //是否指定了最大值
    if (max > -1 && value > max)
    {
        alert('输入的数值必须小于或等于 ' + max);
        obj.focus();
        obj.select();
        return false;
    }
    if (min > -1 && value < min)
    {
        alert('输入的数值必须大于或等于 ' + min);
        obj.focus();
        obj.select();
        return false;
    }
    return true;
}

//验证浮点数
function ValidReal()
{
    var obj = GetEvent();

    if (!obj) return true;

    var kutil = keyPressUtil(obj);
    return kutil.charCodeIn(
            function (c)
            {
                if (c === 46) // 小数点
                {
                    var ele = obj.srcElement || obj.currentTarget;
                    return ele.value.indexOf('.') === -1;
                }
            },
            function (c) // 减号
            {
                if (c === 45) ValidNumberSubtract(obj.srcElement || obj.currentTarget);
                return false;
            },
            function (c) { return c >= 48 && c <= 57; } //数字
        ) ||
        kutil.keyCodeIn(keyPressUtil.isControlKey);
}
//失去焦点时
function ValidReal2()
{
    var obj = GetEvent();

    if (!obj || !obj.value) return true;

    var value = parseFloat(obj.value, 10);
    if (!isNaN(value)) return true;
    alert("这里只能输入浮点数！");
    obj.focus();
    obj.select();
    return false;
}

//验证IP地址
function ValidIP()
{
    var obj = GetEvent();

    if (!obj) return true;
    //圆点
    if (GetkeyCode(obj) == 46) return true;
    //0到9
    if (GetkeyCode(obj) < 48 || GetkeyCode(obj) > 57) return false;
    return true;
}
//失去焦点时
function ValidIP2()
{
    var obj = GetEvent();

    if (!obj || !obj.value) return true;

    if (/^((\d)|(([1-9])\d)|(1\d\d)|(2(([0-4]\d)|5([0-5]))))\.((\d)|(([1-9])\d)|(1\d\d)|(2(([0-4]\d)|5([0-5]))))\.((\d)|(([1-9])\d)|(1\d\d)|(2(([0-4]\d)|5([0-5]))))\.((\d)|(([1-9])\d)|(1\d\d)|(2(([0-4]\d)|5([0-5]))))$/.test(obj.value)) return true;
    alert("这里只能输入标准IP地址！");
    obj.focus();
    obj.select();
    return false;
}

//验证Email地址
function ValidMail()
{
    var obj = GetEvent();

    if (!obj || !obj.value) return true;

    if (/^((\d|[a-z]|[A-Z])(((\d|[a-z]|[A-Z]|\_){1,19})))@((((\d|[a-z]|[A-Z]){1,10})\.){1,4})(((\d|[a-z]|[A-Z])){2,10})$/.test(obj.value)) return true;
    alert("这里只能输入标准Email地址！");
    obj.focus();
    obj.select();
    return false;
}
// 返回 event 对象 
function GetEvent()
{
    if (document.all) // IE 
    {
        return window.event;
    }

    var func = GetEvent.caller; // 返回调用本函数的函数 
    while (func != null)
    {
        // Firefox 中一个隐含的对象 arguments，第一个参数为 event 对象  
        var arg0 = func.arguments[0];
        //  alert('参数长度：' + func.arguments.length); 
        if (arg0)
        {
            if ((arg0.constructor == Event || arg0.constructor == MouseEvent)
               || (typeof (arg0) == "object" && arg0.preventDefault && arg0.stopPropagation))
            {
                return arg0;
            }
        }
        func = func.caller;
    }
    return null;
}
// 返回 keyCode 对象
function GetkeyCode(e)
{
    return e.which || e.keyCode;
}