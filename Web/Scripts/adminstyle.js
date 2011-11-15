var highlightcolor = '#c1ebff';
//此处clickcolor只能用win系统颜色代码才能成功,如果用#xxxxxx的代码就不行,还没搞清楚为什么:(
var clickcolor = '#51b2f6';
function changeto() {
    source = event.srcElement;
    if (source.tagName == "TR" || source.tagName == "TABLE")
        return;
    while (source.tagName != "TD")
        source = source.parentElement;
    source = source.parentElement;
    cs = source.children;
    //alert(cs.length);
    if (cs[1].style.backgroundColor != highlightcolor && source.id != "nc" && cs[1].style.backgroundColor != clickcolor)
        for (i = 0; i < cs.length; i++) {
            cs[i].style.backgroundColor = highlightcolor;
        }
}

function changeback() {
    if (event.fromElement.contains(event.toElement) || source.contains(event.toElement) || source.id == "nc")
        return
    if (event.toElement != source && cs[1].style.backgroundColor != clickcolor)
    //source.style.backgroundColor=originalcolor
        for (i = 0; i < cs.length; i++) {
            cs[i].style.backgroundColor = "";
        }
}

function clickto() {
    source = event.srcElement;
    if (source.tagName == "TR" || source.tagName == "TABLE")
        return;
    while (source.tagName != "TD")
        source = source.parentElement;
    source = source.parentElement;
    cs = source.children;
    //alert(cs.length);
    if (cs[1].style.backgroundColor != clickcolor && source.id != "nc")
        for (i = 0; i < cs.length; i++) {
            cs[i].style.backgroundColor = clickcolor;
        }
    else
        for (i = 0; i < cs.length; i++) {
            cs[i].style.backgroundColor = "";
        }
}

//默认选择Tab
var tID = 0;
function ShowTabs(ID) {
    if (ID != tID) {
        document.getElementById("TabTitle" + tID).className = "tabtitle";
        document.getElementById("TabTitle" + ID).className = "titlemouseover";
        document.getElementById("Tab" + ID).style.display = "";
        document.getElementById("Tab" + tID).style.display = "none";
        tID = ID;
    }
}