/*
使用流程：
1，实例化
2，设置用户名密码
3，打开链接
4，自动登录
5，定时心跳
6，调用业务动作
*/

var ApiClient = (function (uri) {
    var ApiClient = function (uri) {
        return new ApiClient.fn.init(uri);
    }

    ApiClient.fn = ApiClient.prototype = {
        constructor: ApiClient,
        init: function (uri) {
            this.Uri = uri;
            this.Socket = null;
            this.OnReceive = null;
        },

        // 用户名密码
        UserName: null,
        PassWord: null,

        // 打开连接
        Open: function (callback) {
            var ws = this.Socket;
            if (ws && ws.readyState === WebSocket.OPEN) return true;

            console.log('Open ' + this.Uri);

            ws = new WebSocket(this.Uri);
            ws.onopen = function (evt) {
                console.log("已经建立连接");

                if (callback) callback();

                // 异步登录
                if (this.UserName) setTimeout(this.Login, 100);
            };

            // 等到创建完成
            /*var exp = new Date().getTime() + 5000;
            while (ws.readyState != WebSocket.OPEN) {
                if (exp <= new Date().getTime()) return false;
            }*/
            ws.onclose = function (evt) {
                console.log("已经关闭连接");
            };
            ws.onmessage = this.onMessage;
            ws.onerror = function (evt) {
                console.log(evt.message);
            };

            this.Socket = ws;

            return true;
        },

        // 获取Socket
        getSocket: function () {
            var ws = this.Socket;
            if (ws && ws.readyState === WebSocket.OPEN) return ws;

            //return this.Open() ? this.Socket : null;
            return null;
        },

        // 注册动作到指定函数
        requestCallbacks: new Array(),
        Register: function (action, callback) {
            this.requestCallbacks[action] = callback;
        },

        // 处理请求和响应的委托
        responseCallback: null,
        // 收到数据
        onMessage: function (evt) {
            evt.stopPropagation()
            evt.preventDefault()
            console.log(evt.data);

            // 响应
            var msg = JSON.parse(evt.data);
            if (msg) {
                if (msg.action) {
                    if (this.requestCallbacks) {
                        var callback = this.requestCallbacks[msg.action];
                        if (callback) callback(msg.args);
                    }
                } else if (msg.code) {
                    if (this.responseCallback) this.responseCallback(msg);
                }
                return;
            }
            if (this.OnReceive) this.OnReceive(evt.data);
        },

        // 发送数据
        Send: function (data) {
            console.log('Send ' + data);

            var ws = this.getSocket();
            if (ws) return ws.send(data);

            this.Open(function () {
                var ws = this.getSocket();
                if (ws) return ws.send(data);
            });

            return true;
        },

        // 调用动作，并在收到响应时调用回调函数
        Invoke: function (action, args, callback) {
            var msg = { action: action, args: args };
            //console.log('Invoke ' + msg);

            this.responseCallback = callback;
            this.Send(JSON.stringify(msg));
        },

        // 登录
        _timer: 0,
        Login: function (callback) {
            var user = this.UserName;
            var pass = this.PassWord;
            var args = { user: this.UserName, pass: this.PassWord };
            console.log('Login ' + args);

            //this.Invoke('Login');
            this.Invoke('Login', args, function (msg) {
                if (callback) callback(msg);

                // 定时心跳
                if (!this._timer) this._timer = setInterval(this.Ping, 30000);
            });
        },

        // 心跳
        Ping: function () {
            this.Invoke('Ping');
        },
    }

    ApiClient.fn.init.prototype = ApiClient.fn;

    return ApiClient;
})();