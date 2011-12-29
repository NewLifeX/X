(function () {
    function extend(obj) {
        for (var i = 1; i < arguments.length; i++) {
            var o = arguments[i];
            if (o) {
                for (var p in o) {
                    obj[p] = o[p];
                }
            }
        }
        return obj;
    }
    function replaceString(s, val) {
        /// <summary>替换指定s中包含{foo}的字符串为val['foo']的值</summary>
        for (var i in val) {
            s = s.replace(new RegExp('\\{' + i + '\\}', 'g'), encodeURIComponent(val[i]));
        }
        return s;
    }
    function setAttributes(ele, attrs) {
        /// <summary>给指定元素设置多个属性</summary>
        /// <param name="ele">指定元素</param>
        /// <param name="attrs">object类型,属性名和值</param>
        /// <returns>DOM Element</returns>
        for (var i in attrs) {
            ele.setAttribute(i, attrs[i]);
        }
        return ele;
    }
    function showModalDialogParams(p) {
        var r = '';
        for (var i in p) {
            r += '; ' + i + ':' + p[i];
        }

        return r == '' ? r : r.substring(2);
    }

    function randomUrl(u) {
        /// <summary>在指定url中复杂一个随机参数,用于始终不缓存,在showModalDialog中有用</summary>
        /// <param name="u">原始url</param>
        /// <returns>String</returns>
        var r = 'randomflag=' + (+new Date() + Math.random());
        u += u.indexOf('?') ? '&' : '?';
        u += r;
        return u;
    }

    function Choose(self, url, modaldialogoptions, extoptions) {
        /// <summary>打开指定地址的选择窗口</summary>
        /// <param name="self">来源元素,需要拥有value(显示名称)和val(值)属性</param>
        /// <param name="url">选择窗口的url,url中可以使用{value}和{text},分别代表当前选择的值和显示名称</param>
        /// <param name="options">object类型,窗口选项,可忽略</param>
        try {
            var valEle = document.getElementById(self.getAttribute('val'));
            var opts = {
                Element: self,
                Url: url,
                UrlParams: {
                    value: valEle.value,
                    text: self.value
                },
                ModalDialogOptions: extend({
                    dialogWidth: '800px',
                    dialogHeight: '600px',
                    center: 1,
                    help: 0,
                    status: 0
                }, modaldialogoptions),
                ExtraClientOptions: extend({
                    before: "", //执行前的回调,包含参数options,即当前的opts,回调返回false表示取消请求,如果返回true表示继续请求,并且不用再额外处理Url,返回undefined表示按照正常情况处理Url
                    after: ""
                }, extoptions)
            };

            if (opts.ExtraClientOptions.before) {
                var func = opts.ExtraClientOptions.before;
                if (typeof func == 'string') {
                    func = new Function('options', opts.ExtraClientOptions.before);
                }
                opts.ExtraClientOptions.beforeResult = func.call(opts.Element, opts);
            }

            if (opts.ExtraClientOptions.beforeResult == false) {
                return false;
            } else if (opts.ExtraClientOptions.beforeResult == undefined) {
                opts.UrlProcessed = replaceString(url, opts.UrlParams);
            } else {
                opts.UrlProcessed = opts.Url;
            }

            opts.UrlProcessed = randomUrl(opts.UrlProcessed);

            var result = showModalDialog(opts.UrlProcessed, {opener:window}, showModalDialogParams(opts.ModalDialogOptions));

            var resultArray;
            if (result != undefined) {
                resultArray = result && typeof (result) == 'string' ? result.split('|||') : [];
            }
            var defaultProcess = function () {
                try {
                    opts.Element.value = resultArray[1];
                    valEle.value = resultArray[0];
                } catch (e) { }
            };
            defaultProcess();

            if (opts.ExtraClientOptions.after) {
                var func = opts.ExtraClientOptions.after;
                if (typeof func == 'string') {
                    func = new Function("options", "result", func);
                }
                opts.ExtraClientOptions.afterResult = func.call(opts.Element, opts, { result: result, resultArray: resultArray, defaultProcess: defaultProcess });
            }
            if (opts.ExtraClientOptions.afterResult == undefined || opts.ExtraClientOptions.afterResult) {
                defaultProcess();
            }


        } catch (e) {
            alert(e);
        }
    }
    window.Choose = Choose;
})();
