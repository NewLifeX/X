//var IMGFOLDERPATH = '../images/Dialog/'; //图片路径配置
var CONTEXTPATH = ''; //弹出框内页面路径配置
var isIE = navigator.userAgent.toLowerCase().indexOf("msie") != -1;
var isIE6 = navigator.userAgent.toLowerCase().indexOf("msie 6.0") != -1;
var isGecko = navigator.userAgent.toLowerCase().indexOf("gecko") != -1;
var isQuirks = document.compatMode == "BackCompat";

function GetObjID(ele)
{
    if (typeof (ele) == 'string')
    {
        ele = document.getElementById(ele)
        if (!ele)
        {
            return null;
        }
    }
    if (ele)
    {
        Core.attachMethod(ele);
    }
    return ele;
}
function $T(tagName, ele)
{
    ele = GetObjID(ele);
    ele = ele || document;
    var ts = ele.getElementsByTagName(tagName); //此处返回的不是数组
    var arr = [];
    var len = ts.length;
    for (var i = 0; i < len; i++)
    {
        arr.push(GetObjID(ts[i]));
    }
    return arr;
}
function stopEvent(event)
{//阻止一切事件执行,包括浏览器默认的事件
    event = window.event || event;
    if (!event)
    {
        return;
    }
    if (isGecko)
    {
        event.preventDefault();
        event.stopPropagation();
    }
    event.cancelBubble = true
    event.returnValue = false;
}

Array.prototype.remove = function(s)
{
    for (var i = 0; i < this.length; i++)
    {
        if (s == this[i])
        {
            this.splice(i, 1);
        }
    }
};

if (typeof window.HTMLElement != 'undefined' &&
    typeof HTMLElement.prototype != 'undefined' &&
    typeof HTMLElement.prototype.__defineGetter__ != 'undefined')
{
    //给FF添加IE专有的属性和方法
    var elept = HTMLElement.prototype;
    if(!elept.hasOwnProperty('parentElement'))
    {
        elept.__defineGetter__("parentElement", function()
        {
            if (this.parentNode == this.ownerDocument) return null;
            return this.parentNode;
        });
    }
    if(!elept.hasOwnProperty('outerHTML'))
    {
        elept.__defineSetter__("outerHTML", function(sHTML)
        {
            var r = this.ownerDocument.createRange();
            r.setStartBefore(this);
            var df = r.createContextualFragment(sHTML);
            this.parentNode.replaceChild(df, this);
            return sHTML;
        });
        elept.__defineGetter__("outerHTML", function()
        {
            var attr;
            var attrs = this.attributes;
            var str = "<" + this.tagName;
            for (var i = 0; i < attrs.length; i++)
            {
                attr = attrs[i];
                if (attr.specified)
                    str += " " + attr.name + '="' + attr.value + '"';
            }
            if (!this.canHaveChildren)
                return str + ">";
            return str + ">" + this.innerHTML + "</" + this.tagName + ">";
        });
    }
    if(!elept.hasOwnProperty('innerText'))
    {
        elept.__defineSetter__("innerText", function(sText)
        {
            var parsedText = document.createTextNode(sText);
            this.innerHTML = parsedText;
            return parsedText;
        });
        elept.__defineGetter__("innerText", function()
        {
            var r = this.ownerDocument.createRange();
            r.selectNodeContents(this);
            return r.toString();
        });
    }
}

var $E = {};
$E.$A = function(attr, ele)
{
    ele = ele || this;
    ele = GetObjID(ele);
    return ele.getAttribute ? ele.getAttribute(attr) : null;
};
$E.getTopLevelWindow = function()
{
    var pw = window, lastPW;
    while (pw != pw.parent)
    {
        lastPW = pw;
        pw = pw.parent;
        try
        {
            // 想上层找顶级窗口,如果发现某个顶级窗口不包含弹窗相关的js 则去上一次找到的窗口 可以避免弹窗调用失败的情况
            if(typeof pw.$E !=='object' ||
               typeof pw.$E.$A !== 'function' ||
               typeof pw.$E.getTopLevelWindow !== 'function' ||
               typeof pw.$E.show !== 'function' ||
               typeof pw.$E.hide !== 'function' ||
               typeof pw.$E.visible !== 'function')
            {
                pw = lastPW;
                break;
            }
        }
        catch(e)
        {
            pw = lastPW;
            break;
        }
    }
    return pw;
};
$E.hide = function(ele)
{
    ele = ele || this;
    ele = GetObjID(ele);
    ele.style.display = 'none';
};
$E.show = function(ele)
{
    ele = ele || this;
    ele = GetObjID(ele);
    ele.style.display = '';
};
$E.visible = function(ele)
{
    ele = ele || this;
    ele = GetObjID(ele);
    if (ele.style.display == "none")
    {
        return false;
    }
    return true;
};

var Core = {};
Core.attachMethod = function(ele)
{
    if (!ele || ele["$A"])
    {
        return;
    }
    if (ele.nodeType == 9)
    {
        return;
    }
    var win;
    try
    {
        if (isGecko)
        {
            win = ele.ownerDocument.defaultView;
        } else
        {
            win = ele.ownerDocument.parentWindow;
        }
        for (var prop in $E)
        {
            ele[prop] = win.$E[prop];
        }
    } catch (ex)
    {
        //alert("Core.attachMethod:"+ele)//有些对象不能附加属性，如flash
    }
};

function Dialog(strID)
{
    if (!strID)
    {
        alert("错误的Dialog ID！");
        return;
    }
    this.ID = strID;
    this.isModal = true;
    this.Width = 400;
    this.Height = 300;
    this.Top = 0;
    this.Left = 0;
    this.ParentWindow = null;
    this.onLoad = null;
    this.Window = null;
    this.HideScroll = true; //是否禁止出现滚动条

    this.Title = "";
    this.URL = null;
    this.innerHTML = null
    this.innerElementId = null
    this.DialogArguments = {};
    this.WindowFlag = false;
    this.Message = null;
    this.MessageTitle = null;//弹窗标题
    this.ShowMessageRow = false;//是否显示信息提示栏
    this.ShowButtonRow = true;//是否显示按钮栏
    this.Icon = null;
    this.bgdivID = null;
    this.buttons = null; //按钮组（默认显示确定和取消） [{ displayname: "修改",ID:"",title:"", onpress: Hander }]
    this.showBtOK = true; //是否显示确认按钮
    //this.btCancelText = ""; //关闭按钮默认文字
    //this.btOKText = "";//确定按钮默认文字
    this.BeforeShow = null;
    this.AfterClose = null;
}

Dialog._Array = [];

Dialog.prototype.showWindow = function() {
    if (isIE) {
        alert(typeof (this.ParentWindow));
        for (var m in this.ParentWindow.window) {
           
                alert(m);
           
        }
        this.ParentWindow.showModalessDialog(this.URL, this.DialogArguments, "dialogWidth:" + this.Width + ";dialogHeight:" + this.Height + ";help:no;scroll:no;status:no");
    }
    if (isGecko) {
        var sOption = "location=no,menubar=no,status=no;toolbar=no,dependent=yes,dialog=yes,minimizable=no,modal=yes,alwaysRaised=yes,resizable=no";
        this.Window = this.ParentWindow.open('', this.URL, sOption, true);
        var w = this.Window;
        if (!w) {
            alert("发现弹出窗口被阻止，请更改浏览器设置，以便正常使用本功能!");
            return;
        }
        w.moveTo(this.Left, this.Top);
        w.resizeTo(this.Width, this.Height + 30);
        w.focus();
        w.location.href = this.URL;
        w.Parent = this.ParentWindow;
        w.dialogArguments = this.DialogArguments;
    }
};

Dialog.prototype.show = function() {
    var pw = $E.getTopLevelWindow();
    var doc = pw.document;
    var cw = doc.compatMode == "BackCompat" ? doc.body.clientWidth : doc.documentElement.clientWidth;
    var ch = doc.compatMode == "BackCompat" ? doc.body.clientHeight : doc.documentElement.clientHeight; //必须考虑文本框处于页面边缘处，控件显示不全的问题
    var sl = Math.max(doc.documentElement.scrollLeft, doc.body.scrollLeft);
    var st = Math.max(doc.documentElement.scrollTop, doc.body.scrollTop); //考虑滚动的情况
    var sw = Math.max(doc.documentElement.scrollWidth, doc.body.scrollWidth);
    var sh = Math.max(doc.documentElement.scrollHeight, doc.body.scrollHeight); //考虑滚动的情况
    sw = Math.max(sw, cw);
    sh = Math.max(sh, ch);
    //	alert("\n"+cw+"\n"+ch+"\n"+sw+"\n"+sh)

    if (!this.ParentWindow) {
        this.ParentWindow = window;
    }
    this.DialogArguments._DialogInstance = this;
    this.DialogArguments.ID = this.ID;

    if (!this.Height) {
        this.Height = this.Width / 2;
    }

    if (this.Top == 0) {
        this.Top = (ch - this.Height - 30) / 2 + st - 8;
    }
    if (this.Left == 0) {
        this.Left = (cw - this.Width - 12) / 2 + sl;
    }
    if (this.ShowButtonRow) {//按钮行高36
        this.Top -= 18;
    }
    if (this.WindowFlag) {
        this.showWindow();
        return;
    }
    var arr = [];
    arr.push("<table id='_DialogTable_" + this.ID + "' style='-moz-user-select:none;' oncontextmenu='stopEvent(event);' onselectstart='stopEvent(event);' border='0' cellpadding='0' cellspacing='0' width='" + (this.Width + 26) + "'>");
    arr.push("  <tr style='cursor:move;' id='_draghandle_" + this.ID + "'>");
    arr.push("    <td width='13' height='33' style=\"background-image:url(<%=WebResource("XControl.Box.Dialog.dialog_lt.png") %>) !important;background-image: none;filter:progid:DXImageTransform.Microsoft.AlphaImageLoader(src='<%=WebResource("XControl.Box.Dialog.dialog_lt.png")%>', sizingMethod='crop');\"><div style='width:13px;'></div></td>");
    arr.push("    <td height='33' style=\"background-image:url(<%=WebResource("XControl.Box.Dialog.dialog_ct.png") %>) !important;background-image: none;filter:progid:DXImageTransform.Microsoft.AlphaImageLoader(src='<%=WebResource("XControl.Box.Dialog.dialog_ct.png")%>', sizingMethod='crop');\"><div style=\"float:left;font-weight:bold; color:#FFFFFF; padding:9px 0 0 4px;overflow:hidden;\"><img src=\"<%=WebResource("XControl.Box.Dialog.icon_dialog.gif")%>\" align=\"absmiddle\">&nbsp;" + this.Title + "</div>");
    arr.push("      <div style=\"position: relative;cursor:pointer; float:right; margin:5px 0 0; _margin:4px 0 0;height:17px; width:28px; background-image:url(<%=WebResource("XControl.Box.Dialog.dialog_closebtn.gif")%>)\" title=\"关闭\" onMouseOver=\"this.style.backgroundImage='url(<%=WebResource("XControl.Box.Dialog.dialog_closebtn_over.gif") %>)'\" onMouseOut=\"this.style.backgroundImage='url(<%=WebResource("XControl.Box.Dialog.dialog_closebtn.gif")%>)'\" drag='false' onClick=\"Dialog.getInstance('" + this.ID + "').CancelButton.onclick.apply(Dialog.getInstance('" + this.ID + "').CancelButton,[]);\"></div></td>");
    arr.push("    <td width='13' height='33' style=\"background-image:url(<%=WebResource("XControl.Box.Dialog.dialog_rt.png") %>) !important;background-image: none;filter:progid:DXImageTransform.Microsoft.AlphaImageLoader(src='<%=WebResource("XControl.Box.Dialog.dialog_rt.png") %>', sizingMethod='crop');\"><div style=\"width:13px;\"></div></td>");
    arr.push("  </tr>");
    arr.push("  <tr drag='false'><td width='13' style=\"background-image:url(<%=WebResource("XControl.Box.Dialog.dialog_mlm.png") %>) !important;background-image: none;filter:progid:DXImageTransform.Microsoft.AlphaImageLoader(src='<%=WebResource("XControl.Box.Dialog.dialog_mlm.png") %>', sizingMethod='crop');\"></td>");
    arr.push("    <td align='center' valign='top'><a href='#;' id='_forTab_" + this.ID + "'></a>");
    arr.push("    <table width='100%' border='0' cellpadding='0' cellspacing='0' bgcolor='#FFFFFF'>");
    arr.push("        <tr id='_MessageRow_" + this.ID + "' style='display:none'>");
    arr.push("          <td height='50' valign='top'><table id='_MessageTable_" + this.ID + "' width='100%' border='0' cellspacing='0' cellpadding='8' style=\" background:#EAECE9 url(<%=WebResource("XControl.Box.Dialog.dialog_bg.jpg") %>) no-repeat right top;\">");
    arr.push("              <tr><td width='25' height='50' align='right'><img id='_MessageIcon_" + this.ID + "' src='<%=WebResource("XControl.Box.Dialog.window.gif") %>' width='32' height='32'></td>");
    arr.push("                <td align='left' style='line-height:16px;' valign='top'>");
    arr.push("                <span class='fb' style='font-size:14px;font-weight:bolder;' id='_MessageTitle_" + this.ID + "'>&nbsp;</span>");
    arr.push("                <div style='font-size:12px;' id='_Message_" + this.ID + "'>&nbsp;</div></td>");
    arr.push("              </tr></table></td></tr>");
    arr.push("        <tr><td align='center' valign='top'><div id='_DialogLayout_" + this.ID + "' style='position:relative;width:" + this.Width + "px;height:" + this.Height + "px;'>");
    arr.push("         <div  id='_Covering_" + this.ID + "' style='position:absolute; height:100%; width:100%;display:none;'>&nbsp;</div>");
    if (this.innerHTML) {
        arr.push(this.innerHTML);
    } else if (this.innerElementId) {
    } else if (this.URL) {
        arr.push("          <iframe src='");
        if (this.URL.substr(0, 7) == "http://" || this.URL.substr(0, 1) == "/") {
            arr.push(this.URL);
        } else {
            arr.push(CONTEXTPATH + this.URL);
        }
        arr.push("' id='_DialogFrame_" + this.ID + "' allowTransparency='true'  width='100%' height='100%' frameborder='0' style=\"background-color: #transparent; border:none;\"></iframe>");
    }
    arr.push("        </div></td></tr>");
    arr.push("        <tr drag='false' id='_ButtonRow_" + this.ID + "'");
    if (!this.ShowButtonRow)
        arr.push("style='display:none;'");
    arr.push("><td height='36'>");
    arr.push("            <div id='_DialogButtons_" + this.ID + "' style='text-align:right; border-top:#dadee5 1px solid; padding:8px 20px; background-color:#f6f6f6;");
    arr.push("'>");
    ///自定义按钮组
    if (this.buttons) {
        for (i = 0; i < this.buttons.length; i++) {
            var btn = this.buttons[i];
            if (btn.ID) {
                if (btn.title) {
                    arr.push("         <input id='_Button_my_" + btn.ID + "'  style='cursor:pointer;' title='" + btn.title + "'  type='button' value='" + btn.displayname + "'>");
                }
                else {
                    arr.push("         <input id='_Button_my_" + btn.ID + "'  style='cursor:pointer;'  type='button' value='" + btn.displayname + "'>");
                }
            }
        }
    }
    if (this.showBtOK) {
        if (this.btOKText)
            arr.push("           	<input id='_ButtonOK_" + this.ID + "'  style='cursor:pointer;' type='button' value='" + this.btOKText + "'>");
        else
            arr.push("           	<input id='_ButtonOK_" + this.ID + "'  style='cursor:pointer;' type='button' value='确 定'>");
    }
    if (this.btCancelText)
        arr.push("           	<input id='_ButtonCancel_" + this.ID + "'  style='cursor:pointer;'  type='button' onclick=\"Dialog.getInstance('" + this.ID + "').close();\" value='" + this.btCancelText + "'>");
    else
        arr.push("           	<input id='_ButtonCancel_" + this.ID + "'  style='cursor:pointer;' type='button' onclick=\"Dialog.getInstance('" + this.ID + "').close();\" value='取 消'>");
    arr.push("            </div></td></tr>");
    arr.push("      </table><a href='#;' onfocus='GetObjID(\"_forTab_" + this.ID + "\").focus();'></a></td>");
    arr.push("    <td width='13' style=\"background-image:url(<%=WebResource("XControl.Box.Dialog.dialog_mrm.png") %>) !important;background-image: none;filter:progid:DXImageTransform.Microsoft.AlphaImageLoader(src='<%=WebResource("XControl.Box.Dialog.dialog_mrm.png") %>', sizingMethod='crop');\"></td></tr>");
    arr.push("  <tr><td width='13' height='13' style=\"background-image:url(<%=WebResource("XControl.Box.Dialog.dialog_lb.png") %>) !important;background-image: none;filter:progid:DXImageTransform.Microsoft.AlphaImageLoader(src='<%=WebResource("XControl.Box.Dialog.dialog_lb.png") %>', sizingMethod='crop');\"></td>");
    arr.push("    <td style=\"background-image:url(<%=WebResource("XControl.Box.Dialog.dialog_cb.png") %>) !important;background-image: none;filter:progid:DXImageTransform.Microsoft.AlphaImageLoader(src='<%=WebResource("XControl.Box.Dialog.dialog_cb.png") %>', sizingMethod='crop');\"></td>");
    arr.push("    <td width='13' height='13' style=\"background-image:url(<%=WebResource("XControl.Box.Dialog.dialog_rb.png") %>) !important;background-image: none;filter:progid:DXImageTransform.Microsoft.AlphaImageLoader(src='<%=WebResource("XControl.Box.Dialog.dialog_rb.png") %>', sizingMethod='crop');\"></td>");
    arr.push("  </tr></table>");
    this.TopWindow = pw;

    var bgdiv = pw.GetObjID("_DialogBGDiv");
    if (!bgdiv) {
        bgdiv = pw.document.createElement("div");
        bgdiv.id = "_DialogBGDiv";
        $E.hide(bgdiv);
        pw.$T("body")[0].appendChild(bgdiv);
        if (isIE6) {
            var bgIframeBox = pw.document.createElement('<div style="position:relative;width:100%;height:100%;"></div>');
            var bgIframe = pw.document.createElement('<iframe src="about:blank" style="filter:alpha(opacity=1);" width="100%" height="100%"></iframe>');
            var bgIframeMask = pw.document.createElement('<div src="about:blank" style="position:absolute;background-color:#333;filter:alpha(opacity=1);width:100%;height:100%;"></div>');
            bgIframeBox.appendChild(bgIframeMask);
            bgIframeBox.appendChild(bgIframe);

            bgdiv.appendChild(bgIframeBox);

            var bgIframeDoc = bgIframe.contentWindow.document;
            bgIframeDoc.open();
            bgIframeDoc.write("<body style='background-color:#333' oncontextmenu='return false;'></body>");
            bgIframeDoc.close();
        }
    }

    var div = pw.GetObjID("_DialogDiv_" + this.ID);
    if (!div) {
        div = pw.document.createElement("div");
        $E.hide(div);
        div.id = "_DialogDiv_" + this.ID;
        div.className = "dialog-div";
        //div.setAttribute("dragStart","Dialog.dragStart");
        pw.$T("body")[0].appendChild(div);
    }
    /*div.onmousedown = function(evt){
    var w = $$$E.getTopLevelWindow();
    //w.DragManager.onMouseDown(evt||w.event,this);//拖拽处理
    }*/

    this.DialogDiv = div;
    div.innerHTML = arr.join('\n');
    if (isIE6 && this.URL && this.URL.indexOf("javascript") < 0) {
        var winObj = document.getElementById("_DialogFrame_" + this.ID);
        if (winObj) {
            winObj.contentWindow.window.location.reload();
        }
    }
    if (this.innerElementId) {
        var innerElement = GetObjID(this.innerElementId);
        innerElement.style.position = "";
        innerElement.style.display = "";
        if (isIE) {
            var fragment = pw.document.createElement("div");
            fragment.innerHTML = innerElement.outerHTML;
            innerElement.outerHTML = "";
            pw.GetObjID("_Covering_" + this.ID).parentNode.appendChild(fragment)
        } else {
            pw.GetObjID("_Covering_" + this.ID).parentNode.appendChild(innerElement)
        }
    }
    pw.GetObjID("_DialogDiv_" + this.ID).DialogInstance = this;
    if (this.URL)
        pw.GetObjID("_DialogFrame_" + this.ID).DialogInstance = this;
    pw.Drag.init(pw.GetObjID("_draghandle_" + this.ID), pw.GetObjID("_DialogDiv_" + this.ID)); //注册拖拽方法
    if (!isIE) {
        pw.GetObjID("_DialogDiv_" + this.ID).dialogId = this.ID;
        pw.GetObjID("_DialogDiv_" + this.ID).onDragStart = function() { pw.GetObjID("_Covering_" + this.dialogId).style.display = "" }
        pw.GetObjID("_DialogDiv_" + this.ID).onDragEnd = function() { pw.GetObjID("_Covering_" + this.dialogId).style.display = "none" }
    }
    if (this.showBtOK) {
        this.OKButton = pw.GetObjID("_ButtonOK_" + this.ID);
    }
    this.CancelButton = pw.GetObjID("_ButtonCancel_" + this.ID);

    //显示标题图片
    if (this.ShowMessageRow) {
        $E.show(pw.GetObjID("_MessageRow_" + this.ID));
        if (this.MessageTitle) {
            pw.GetObjID("_MessageTitle_" + this.ID).innerHTML = this.MessageTitle;
        }
        if (this.Message) {
            pw.GetObjID("_Message_" + this.ID).innerHTML = this.Message;
        }
    }

    //显示按钮栏
    //    if (!this.ShowButtonRow) {
    //        //pw.$$$("_ButtonRow_"+this.ID).hide();
    //        $E.hide("_ButtonRow_" + this.ID);
    //    }
    if (this.CancelEvent) {
        this.CancelButton.onclick = this.CancelEvent;
    }
    if (this.OKEvent && this.showBtOK) {
        this.OKButton.onclick = this.OKEvent;
    }
    //绑定自定义按钮组事件
    if (this.buttons) {
        for (i = 0; i < this.buttons.length; i++) {
            var btn = this.buttons[i];
            if (btn.ID && btn.onpress) {
                pw.GetObjID("_Button_my_" + btn.ID).onclick = btn.onpress;
            }
        }
    }
    if (!this.AlertFlag) {
        $E.show(bgdiv);
        this.bgdivID = "_DialogBGDiv";
    } else {
        bgdiv = pw.GetObjID("_AlertBGDiv");
        if (!bgdiv) {
            bgdiv = pw.document.createElement("div");
            bgdiv.id = "_AlertBGDiv";
            $E.hide(bgdiv);
            pw.$T("body")[0].appendChild(bgdiv);
            if (isIE6) {
                var bgIframeBox = pw.document.createElement('<div style="position:relative;width:100%;height:100%;"></div>');
                var bgIframe = pw.document.createElement('<iframe src="about:blank" style="filter:alpha(opacity=1);" width="100%" height="100%"></iframe>');
                var bgIframeMask = pw.document.createElement('<div src="about:blank" style="position:absolute;background-color:#333;filter:alpha(opacity=1);width:100%;height:100%;"></div>');
                bgIframeBox.appendChild(bgIframeMask);
                bgIframeBox.appendChild(bgIframe);
                bgdiv.appendChild(bgIframeBox);
                var bgIframeDoc = bgIframe.contentWindow.document;
                bgIframeDoc.open();
                bgIframeDoc.write("<body style='background-color:#333' oncontextmenu='return false;'></body>");
                bgIframeDoc.close();
            }
            bgdiv.style.cssText = "background-color:#333;position:absolute;left:0px;top:0px;opacity:0.4;filter:alpha(opacity=40);width:100%;height:" + sh + "px;z-index:991";
        }
        $E.show(bgdiv);
        this.bgdivID = "_AlertBGDiv";
    }
    this.DialogDiv.style.cssText = "position:absolute; display:block;z-index:" + (this.AlertFlag ? 992 : 990) + ";left:" + this.Left + "px;top:" + this.Top + "px";

    //判断当前窗口是否是对话框，如果是，则将其置在bgdiv之后
    if (!this.AlertFlag) {
        var win = window;
        var flag = false;
        while (win != win.parent) {//需要考虑父窗口是弹出窗口中的一个iframe的情况
            if (win._DialogInstance) {
                win._DialogInstance.DialogDiv.style.zIndex = 959;
                flag = true;
                break;
            }
            win = win.parent;
        }
        if (!flag) {
            bgdiv.style.cssText = "background-color:#333;position:absolute;left:0px;top:0px;opacity:0.4;filter:alpha(opacity=40);width:100%;height:" + sh + "px;z-index:960";
        }
        //this.ParentWindow.$D = this;
    }
    if (this.ShowButtonRow && this.showBtOK)
        this.OKButton.focus();

    var pwbody = doc.getElementsByTagName(isQuirks ? "BODY" : "HTML")[0];
    /* // @netwjx 不修改父窗口的html标签样式
    if (this.HideScroll)
        pwbody.style.overflow = "hidden";// 禁止出现滚动条
    */

    pw.Dialog._Array.push(this.ID); //放入队列中，以便于ESC时正确关闭
};

Dialog.prototype.addParam = function(paramName, paramValue)
{
    this.DialogArguments[paramName] = paramValue;
};

//添加可调整窗体大小
Dialog.prototype.resize = function(width, height)
{
    this.Width = width;
    this.Height = height;

    try
    {
        var table = GetObjID("_DialogTable_"+this.ID);
        var layout = GetObjID("_DialogLayout_"+this.ID);

        table.width = width+26;
        layout.style.width = width+"px";
        layout.style.height = height+"px";

        this.setPosition();
    }catch(e)
    {
       //alert(e);
    }
};

Dialog.prototype.close = function()
{
    try{
        if (this.innerElementId)
        {
            var innerElement = $E.getTopLevelWindow().GetObjID(this.innerElementId);
            innerElement.style.display = "none";
            if (isIE)
            {
                //ie下不能跨窗口拷贝元素
                var fragment = document.createElement("div");
                fragment.innerHTML = innerElement.outerHTML;
                innerElement.outerHTML = "";
                $T("body")[0].appendChild(fragment)
            } else
            {
                $T("body")[0].appendChild(innerElement)
            }
        }
        if (this.WindowFlag)
        {
            this.ParentWindow.$D = null;
            this.ParentWindow.$DW = null;
            this.Window.opener = null;
            this.Window.close();
            this.Window = null;
        } else
        {
            //如果上级窗口是对话框，则将其置于bgdiv前
            var pw = $E.getTopLevelWindow();
            var doc = pw.document;
            var win = window;
            var flag = false;
            while (win != win.parent)
            {
                if (win._DialogInstance)
                {
                    flag = true;
                    win._DialogInstance.DialogDiv.style.zIndex = 960;
                    break;
                }
                win = win.parent;
            }
            if (this.AlertFlag)
            {
                $E.hide(pw.GetObjID("_AlertBGDiv"));
            }
            if (!flag && !this.AlertFlag)
            {//此处是为处理弹出窗口被关闭后iframe立即被重定向时背景层不消失的问题
                pw.eval('window._OpacityFunc = function(){var w = $E.getTopLevelWindow();$E.hide(w.GetObjID("_DialogBGDiv"));}');
                pw._OpacityFunc();
            }
            this.DialogDiv.outerHTML = "";
            /* // @netwjx 不修改父窗口的html标签样式
            var pwbody = doc.getElementsByTagName(isQuirks ? "BODY" : "HTML")[0];
            pwbody.style.overflow = "auto"; //还原滚动条
            */
            pw.Dialog._Array.remove(this.ID);
        }
    }finally{
        if(typeof this.AfterClose === 'function'){
            this.AfterClose();
        }
    }
};

Dialog.prototype.addButton = function(id, txt, func)
{
    var html = "<input id='_Button_" + this.ID + "_" + id + "' type='button' value='" + txt + "'> ";
    var pw = $E.getTopLevelWindow();
    pw.GetObjID("_DialogButtons_" + this.ID).$T("input")[0].getParent("a").insertAdjacentHTML("beforeBegin", html);
    pw.GetObjID("_Button_" + this.ID + "_" + id).onclick = func;
};

/// 显示窗口
function ShowDialog(options){
    var dialog=new Dialog(options.ID);
     //在显示前修改属性，方便动态修改参数
    for (var o in options) {
        dialog[o]=options[o];
    }
    if(typeof dialog.BeforeShow === 'function'){
        //返回false 终止打开窗口
        if(dialog.BeforeShow()==false) return;
    }

    dialog.show();
    return dialog;
};

Dialog.close = function(evt)
{
    window.Args._DialogInstance.close();
};

Dialog.GetDialogForFrame = function(frameElement)
{
    var r = null;
    try {
        var ele=frameElement;
        while(true){
            ele=ele.parentNode;
            if( ele.tagName === 'DIV' && ele.className.indexOf('dialog-div') >= 0){
                break;
            }else if(ele.tagName==='HTML'){
                ele=null;
                break;
            }
        }
        if(ele){
            r= Dialog.getInstance(/_DialogDiv_(.+)/.exec(ele.id)[1]);
        }
    } catch (e) {
    
    }
    return r;
};

Dialog.Resize = function(frameElement,width,height)
{
    try
    {
       Dialog.GetDialogForFrame(frameElement).resize(width,height);
    }catch(e)
    {
    }
};

Dialog.CloseSelfDialog = function (frameElement) {
    try {
      Dialog.GetDialogForFrame(frameElement).close();
    } catch (e) {
    //    alert(e);
    }
};

Dialog.CloseAndRefresh = function (frameElement) {
    try {
        Dialog.CloseSelfDialog(frameElement);
        //window.frames["main"].location.reload()
        // 大石头 刷新本页面
        if(typeof window.reloadForm == 'function'){
            reloadForm();
        } else if(window.__doPostBack && !window.DisableDoPostBack){ // 如果DisableDoPostBack=true表示禁用了__doPostBack方式的刷新
            __doPostBack();
        } else {
            location.reload();
        }
    } catch (e) {
        //alert(e);
    }
};

Dialog.getInstance = function(id)
{
    var pw = $E.getTopLevelWindow()
    var f = pw.GetObjID("_DialogDiv_" + id);
    if (!f)
    {
        return null;
    }
    return f.DialogInstance;
};

Dialog.AlertNo = 0;
Dialog.alert = function(msg, func, w, h) {
    Dialog.zalert({ msg: msg, func: func, w: w, h: h });
};
Dialog.zalert = function(option) {
    var pw = $E.getTopLevelWindow()
    var diag = new Dialog("_DialogAlert" + Dialog.AlertNo++);
    diag.ParentWindow = pw;
    diag.Width = option.w ? option.w : 300;
    diag.Height = option.h ? option.h : 120;
    diag.Title = "系统提示";
    diag.URL = "javascript:void(0);";
    diag.AlertFlag = true;
    diag.CancelEvent = function() {
        diag.close();
        if (option.func) {
            option.func();
        }
    };
    diag.show();
    pw.GetObjID("_AlertBGDiv").style.display = "";
    $E.hide(pw.GetObjID("_ButtonOK_" + diag.ID));
    var win = pw.GetObjID("_DialogFrame_" + diag.ID).contentWindow;
    var doc = win.document;
    doc.open();
    doc.write("<body oncontextmenu='return false;'></body>");
    var arr = [];
    arr.push("<table height='100%' border='0' align='center' cellpadding='10' cellspacing='0'>");
    arr.push("<tr><td align='right'><img id='Icon' src='<%=WebResource("XControl.Box.Dialog.icon_alert.gif") %>' width='34' height='34' align='absmiddle'></td>");
    arr.push("<td align='left' id='Message' style='font-size:9pt'><div style='width:100%;height:100%;margin:0; padding:0;overflow-y:auto;'>" + option.msg + "</div></td></tr></table>");
    var div = doc.createElement("div");
    div.innerHTML = arr.join('');
    doc.body.appendChild(div);
    doc.close();
    var h = Math.max(doc.documentElement.scrollHeight, doc.body.scrollHeight);
    var w = Math.max(doc.documentElement.scrollWidth, doc.body.scrollWidth);
    if (w > 300) {
        win.frameElement.width = w;
    }
    if (h > 120) {
        win.frameElement.height = h;
    }
    if (option.OKText)
        diag.CancelButton.value = option.OKText;
    else
        diag.CancelButton.value = "确 定";
    diag.CancelButton.focus();
    pw.GetObjID("_DialogButtons_" + diag.ID).style.textAlign = "center";
};

Dialog.confirm = function(msg, func1, func2, w, h)
{
    var pw = $E.getTopLevelWindow()
    var diag = new Dialog("_DialogAlert" + Dialog.AlertNo++);
    diag.Width = w ? w : 300;
    diag.Height = h ? h : 120;
    diag.Title = "信息确认";
    diag.URL = "javascript:void(0);";
    diag.AlertFlag = true;
    diag.CancelEvent = function()
    {
        diag.close();
        if (func2)
        {
            func2();
        }
    };
    diag.OKEvent = function()
    {
        diag.close();
        if (func1)
        {
            func1();
        }
    };
    diag.show();
    pw.GetObjID("_AlertBGDiv").style.dispaly = "";
    var win = pw.GetObjID("_DialogFrame_" + diag.ID).contentWindow;
    var doc = win.document;
    doc.open();
    doc.write("<body oncontextmenu='return false;'></body>");
    var arr = [];
    arr.push("<table height='100%' border='0' align='center' cellpadding='10' cellspacing='0'>");
    arr.push("<tr><td align='right'><img id='Icon' src='<%=WebResource("XControl.Box.Dialog.icon_query.gif") %>' width='34' height='34' align='absmiddle'></td>");
    arr.push("<td align='left' id='Message' style='font-size:9pt'>" + msg + "</td></tr></table>");
    var div = doc.createElement("div");
    div.innerHTML = arr.join('');
    doc.body.appendChild(div);
    doc.close();
    diag.OKButton.focus();
    pw.GetObjID("_DialogButtons_" + diag.ID).style.textAlign = "center";
};

//Dialog.zconfirm = function(option) {
//    var pw = $E.getTopLevelWindow()
//    var diag = new Dialog("_DialogAlert" + Dialog.AlertNo++);
//    diag.Width = option.w ? option.w : 300;
//    diag.Height = option.h ? option.h : 120;
//    diag.Title = option.title ? option.title : "信息确认";
//    diag.URL = "javascript:void(0);";
//    diag.AlertFlag = true;
//    diag.CancelEvent = function() {
//        diag.close();
//        if (option.func2) {
//            option.func2();
//        }
//    };
//    diag.OKEvent = function() {
//        diag.close();
//        if (option.func1) {
//            option.func1();
//        }
//    };
//    diag.show();
//    pw.GetObjID("_AlertBGDiv").style.dispaly = "";
//    var win = pw.GetObjID("_DialogFrame_" + diag.ID).contentWindow;
//    var doc = win.document;
//    doc.open();
//    doc.write("<body oncontextmenu='return false;'></body>");
//    var arr = [];
//    arr.push("<table height='100%' border='0' align='center' cellpadding='10' cellspacing='0'>");
//    arr.push("<tr><td align='right'><img id='Icon' src='" + IMGFOLDERPATH + "icon_query.gif' width='34' height='34' align='absmiddle'></td>");
//    arr.push("<td align='left' id='Message' style='font-size:9pt'>" + option.msg + "</td></tr></table>");
//    var div = doc.createElement("div");
//    div.innerHTML = arr.join('');
//    doc.body.appendChild(div);
//    doc.close();
//    diag.OKButton.focus();
//    pw.GetObjID("_DialogButtons_" + diag.ID).style.textAlign = "center";
//}
var _DialogInstance = null;
try {
    _DialogInstance = window.frameElement.DialogInstance;
}
catch (ex) { 
}
var Page = {};
Page.onDialogLoad = function()
{
    if (_DialogInstance)
    {
        if (_DialogInstance.Title)
        {
            document.title = _DialogInstance.Title;
        }
        window.Args = _DialogInstance.DialogArguments;
        _DialogInstance.Window = window;
        window.Parent = _DialogInstance.ParentWindow;
    }
};

Page.onDialogLoad();

PageOnLoad = function()
{
    var d = _DialogInstance;
    if (d)
    {
        try
        {
            d.ParentWindow.$D = d;
            d.ParentWindow.$DW = d.Window;
            var flag = false;
            if (!this.AlertFlag)
            {
                var win = d.ParentWindow;
                while (win != win.parent)
                {
                    if (win._DialogInstance)
                    {
                        flag = true;
                        break;
                    }
                    win = win.parent;
                }
                if (!flag)
                {
                    $E.getTopLevelWindow().GetObjID("_DialogBGDiv").style.opacity = "0.4";
                    $E.getTopLevelWindow().GetObjID("_DialogBGDiv").style.filter = "alpha(opacity=40)";
                }
            }
            if (d.AlertFlag)
            {
                $E.show($E.getTopLevelWindow().GetObjID("_AlertBGDiv"));
            }
            if (d.ShowButtonRow && $E.visible(d.CancelButton))
            {
                d.CancelButton.focus();
            }
            if (d.onLoad)
            {
                d.onLoad();
            }
        } catch (ex) { 
           //alert("DialogOnLoad:" + ex.message + "\t(" + ex.fileName + " " + ex.lineNumber + ")"); 
        }
    }
};

Dialog.onKeyDown = function(event) {
    var pw = $E.getTopLevelWindow();
    if (pw.Dialog) {
        if (event.shiftKey && event.keyCode == 9) {//shift键
            
            if (pw.Dialog._Array.length > 0) {
                stopEvent(event);
                return false;
            }
        }
        if (event.keyCode == 27) {//ESC键
            if (pw.Dialog._Array.length > 0) {
                //Page.mousedown();
                //Page.click();
                var diag = pw.Dialog.getInstance(pw.Dialog._Array[pw.Dialog._Array.length - 1]);
                diag.CancelButton.onclick.apply(diag.CancelButton, []);
            }
        }
    }
};

Dialog.dragStart = function(evt)
{
    //DragManager.doDrag(evt,this.getParent("div"));//拖拽处理
};
Dialog.setPosition = function()
{
    if (window.parent != window) return;
    var pw = $E.getTopLevelWindow();
    var DialogArr = pw.Dialog._Array;
    if (DialogArr == null || DialogArr.length == 0) return;

    for (i = 0; i < DialogArr.length; i++)
    {
        pw.GetObjID("_DialogDiv_" + DialogArr[i]).DialogInstance.setPosition();
    }
};
Dialog.prototype.setPosition = function()
{
    var pw = $E.getTopLevelWindow();
    var doc = pw.document;
    var cw = doc.compatMode == "BackCompat" ? doc.body.clientWidth : doc.documentElement.clientWidth;
    var ch = doc.compatMode == "BackCompat" ? doc.body.clientHeight : doc.documentElement.clientHeight; //必须考虑文本框处于页面边缘处，控件显示不全的问题
    var sl = Math.max(doc.documentElement.scrollLeft, doc.body.scrollLeft);
    var st = Math.max(doc.documentElement.scrollTop, doc.body.scrollTop); //考虑滚动的情况
    var sw = Math.max(doc.documentElement.scrollWidth, doc.body.scrollWidth);
    var sh = Math.max(doc.documentElement.scrollHeight, doc.body.scrollHeight);
    sw = Math.max(sw, cw);
    sh = Math.max(sh, ch);
    this.Top = (ch - this.Height - 30) / 2 + st - 8; //有8像素的透明背景
    this.Left = (cw - this.Width - 12) / 2 + sl;
    if (this.ShowButtonRow)
    {//按钮行高36
        this.Top -= 18;
    }
    this.DialogDiv.style.top = this.Top + "px";
    this.DialogDiv.style.left = this.Left + "px";
    //pw.$$$(this.bgdivID).style.width= sw + "px";
    pw.GetObjID(this.bgdivID).style.height = sh + "px";
};

var Drag = {
    "obj": null,
    "init": function(handle, dragBody, e)
    {
        if (e == null)
        {
            handle.onmousedown = Drag.start;
        }
        handle.root = dragBody;

        if (isNaN(parseInt(handle.root.style.left))) handle.root.style.left = "0px";
        if (isNaN(parseInt(handle.root.style.top))) handle.root.style.top = "0px";
        handle.root.onDragStart = new Function();
        handle.root.onDragEnd = new Function();
        handle.root.onDrag = new Function();
        if (e != null)
        {
            var handle = Drag.obj = handle;
            e = Drag.fixe(e);
            var top = parseInt(handle.root.style.top);
            var left = parseInt(handle.root.style.left);
            handle.root.onDragStart(left, top, e.pageX, e.pageY);
            handle.lastMouseX = e.pageX;
            handle.lastMouseY = e.pageY;
            document.onmousemove = Drag.drag;
            document.onmouseup = Drag.end;
        }
    },
    "start": function(e)
    {
        var handle = Drag.obj = this;
        e = Drag.fixEvent(e);
        var top = parseInt(handle.root.style.top);
        var left = parseInt(handle.root.style.left);
        //alert(left)
        handle.root.onDragStart(left, top, e.pageX, e.pageY);
        handle.lastMouseX = e.pageX;
        handle.lastMouseY = e.pageY;
        document.onmousemove = Drag.drag;
        document.onmouseup = Drag.end;
        return false;
    },
    "drag": function(e)
    {
        e = Drag.fixEvent(e);

        var handle = Drag.obj;
        var mouseY = e.pageY;
        var mouseX = e.pageX;
        var top = parseInt(handle.root.style.top);
        var left = parseInt(handle.root.style.left);

        if (isIE) { Drag.obj.setCapture(); } else { e.preventDefault(); }; //作用是将所有鼠标事件捕获到handle对象，对于firefox，以用preventDefault来取消事件的默认动作：

        var currentLeft, currentTop;
        currentLeft = left + mouseX - handle.lastMouseX;
        currentTop = top + (mouseY - handle.lastMouseY);
        if (currentLeft < 0) currentLeft = 0;
        if (currentTop < 0) currentTop = 0;
        handle.root.style.left = currentLeft + "px";
        handle.root.style.top = currentTop + "px";
        handle.lastMouseX = mouseX;
        handle.lastMouseY = mouseY;
        handle.root.onDrag(currentLeft, currentTop, e.pageX, e.pageY);
        return false;
    },
    "end": function()
    {
        if (isIE) { Drag.obj.releaseCapture(); }; //取消所有鼠标事件捕获到handle对象
        document.onmousemove = null;
        document.onmouseup = null;
        Drag.obj.root.onDragEnd(parseInt(Drag.obj.root.style.left), parseInt(Drag.obj.root.style.top));
        Drag.obj = null;
    },
    "fixEvent": function(e)
    {//格式化事件参数对象
        var sl = Math.max(document.documentElement.scrollLeft, document.body.scrollLeft);
        var st = Math.max(document.documentElement.scrollTop, document.body.scrollTop);
        if (typeof e == "undefined") e = window.event;
        if (typeof e.layerX == "undefined") e.layerX = e.offsetX;
        if (typeof e.layerY == "undefined") e.layerY = e.offsetY;
        if (typeof e.pageX == "undefined") e.pageX = e.clientX + sl - document.body.clientLeft;
        if (typeof e.pageY == "undefined") e.pageY = e.clientY + st - document.body.clientTop;
        return e;
    }
};

if (isIE)
{
    document.attachEvent("onkeydown", Dialog.onKeyDown);
    window.attachEvent("onload", PageOnLoad);
    window.attachEvent('onresize', Dialog.setPosition);
} else
{
    document.addEventListener("keydown", Dialog.onKeyDown, false);
    window.addEventListener("load", PageOnLoad, false);
    window.addEventListener('resize', Dialog.setPosition, false);
}
//获取URL参数值
function QueryString(fieldName)
{
    var urlString = document.location.search;
    if (urlString != null)
    {
        var typeQu = fieldName + "=";
        var urlEnd = urlString.indexOf(typeQu);
        if (urlEnd != -1)
        {
            var paramsUrl = urlString.substring(urlEnd + typeQu.length);
            var isEnd = paramsUrl.indexOf('&');
            if (isEnd != -1)
            {
                return paramsUrl.substring(0, isEnd);
            }
            else
            {
                return paramsUrl;
            }
        }
        else
        {
            return null;
        }
    }
    else
    {
        return null;
    }
}
String.format = function()
{
    if (arguments.length == 0)
    {
        return null;
    }

    var str = arguments[0];

    for (var i = 1; i < arguments.length; i++)
    {
        var re = new RegExp('\\{' + (i - 1) + '\\}', 'gm');
        str = str.replace(re, arguments[i]);
    }
    return str;
};