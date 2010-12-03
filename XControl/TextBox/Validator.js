
//验证事件源的值是否符合指定的正则表达式
function ValidInput(reg)
{
    if(!reg) return true;
    var obj = GetEvent();
    if(!obj) return true;
    return reg.test(obj.value);
}

//验证数字
function ValidNumber()
{
    var obj = GetEvent();

    if(!obj) return true;
    //负号
    if (!obj.value && GetkeyCode(obj) == 45) return true;
    //0到9
    if (GetkeyCode(obj) < 48 || GetkeyCode(obj) > 57) return false;
    return true;
}
//失去焦点时，验证最小值
function ValidNumber2(min, max, step)
{

    var obj = GetEvent();

    if(!obj || !obj.value) return true;

    var value=parseInt(obj.value);
    if(isNaN(value)) return true;

    //目前暂时不支持步进
    //是否指定了最大值
    if(max>-1 && value>max)
    {
        alert('输入的数值必须小于或等于 '+max);
        obj.focus();
        obj.select();
        return false;
    }
    if(min>-1 && value<min)
    {
        alert('输入的数值必须大于或等于 '+min);
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

    if(!obj) return true;
    //负号
    if (!obj.value && GetkeyCode(obj) == 45) return true;
    //小数点
    if (GetkeyCode(obj) == 46 && (!obj.value || obj.value.indexOf('.') < 0)) return true;
    //0到9
    if (GetkeyCode(obj) < 48 || GetkeyCode(obj) > 57) return false;
    return true;
}
//失去焦点时
function ValidReal2()
{
    var obj = GetEvent();

    if(!obj || !obj.value) return true;

    var value=parseFloat(obj.value);
    if(!isNaN(value)) return true;
    alert("这里只能输入浮点数！");
    obj.focus();
    obj.select();
    return false;
}

//验证IP地址
function ValidIP()
{
    var obj = GetEvent();

    if(!obj) return true;
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

    if(!obj || !obj.value) return true;

    if(/^((\d)|(([1-9])\d)|(1\d\d)|(2(([0-4]\d)|5([0-5]))))\.((\d)|(([1-9])\d)|(1\d\d)|(2(([0-4]\d)|5([0-5]))))\.((\d)|(([1-9])\d)|(1\d\d)|(2(([0-4]\d)|5([0-5]))))\.((\d)|(([1-9])\d)|(1\d\d)|(2(([0-4]\d)|5([0-5]))))$/.test(obj.value)) return true;
    alert("这里只能输入标准IP地址！");
    obj.focus();
    obj.select();
    return false;
}

//验证Email地址
function ValidMail()
{
    var obj = GetEvent();

    if(!obj || !obj.value) return true;

    if(/^((\d|[a-z]|[A-Z])(((\d|[a-z]|[A-Z]|\_){1,19})))@((((\d|[a-z]|[A-Z]){1,10})\.){1,4})(((\d|[a-z]|[A-Z])){2,10})$/.test(obj.value)) return true;
    alert("这里只能输入标准Email地址！");
    obj.focus();
    obj.select();
    return false;
}
// 返回 event 对象 
function GetEvent() {
    if (document.all) // IE 
    {
        return window.event;
    }

    func = GetEvent.caller; // 返回调用本函数的函数 
    while (func != null) {
        // Firefox 中一个隐含的对象 arguments，第一个参数为 event 对象  
        var arg0 = func.arguments[0];
        //  alert('参数长度：' + func.arguments.length); 
        if (arg0) {
            if ((arg0.constructor == Event || arg0.constructor == MouseEvent)
               || (typeof (arg0) == "object" && arg0.preventDefault && arg0.stopPropagation)) {
                return arg0;
            }
        }
        func = func.caller;
    }
    return null;
}
// 返回 keyCode 对象
function GetkeyCode(e) {
    return keyCode = e.which || e.keyCode;  
}