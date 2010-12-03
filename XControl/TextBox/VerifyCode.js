function VerifyCodeBox_Refresh(img) {
    var rnd = "&rnd=" + (+new Date());
    img.src = img.src.replace(/&rnd=.+?(?=&|$)/gi, rnd);
}