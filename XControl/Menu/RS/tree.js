function stopBubble(e) {
    if (e && e.stopPropagation)
        e.stopPropagation()
    else
        window.event.cancelBubble = true
}

function clickOnEntity(entity, e) {

    var e = e || window.event;
    stopBubble(e);

    if (entity.getAttribute("open") == "false") {
        expand(entity, true)
    }
    else {
        collapse(entity)
    }
}

function expand(entity) {
    var oImage

    oImage = document.getElementById(entity.id + "_image");
    oImage.src = entity.getAttribute("imageOpen");

    for (i = 0; i < entity.childNodes.length; i++) {
        if (entity.childNodes[i].tagName == "DIV") {
            entity.childNodes[i].style.display = "block";
        }
    }
    entity.setAttribute("open", "true");
}

function collapse(entity) {
    var oImage;
    var i;

    oImage = document.getElementById(entity.id + "_image");
    oImage.src = entity.getAttribute("image");

    for (i = 0; i < entity.childNodes.length; i++) {
        if (entity.childNodes[i].tagName == "DIV") {
            if (entity.id != "folderTree") entity.childNodes[i].style.display = "none"
            collapse(entity.childNodes[i])
        }
    }
    entity.setAttribute("open", "false");
}

function expandAll(entity) {
    var oImage
    var i

    expand(entity, false)

    for (i = 0; i < entity.childNodes.length; i++) {
        if (entity.childNodes[i].tagName == "DIV") {
            expandAll(entity.childNodes[i])
        }
    }
}
