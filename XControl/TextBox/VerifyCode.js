function VerifyCodeBox_Refresh(img, input) {
    var rnd = "&rnd=" + (+new Date());
    img.src = img.src.replace(/&rnd=.+?(?=&|$)/gi, rnd);
    var e=document.getElementById(input);
    e.select();
    e.focus();
}