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
            var self = this;

            var ws = self.Socket;
            // 如果已打开，直接返回
            if (ws && ws.readyState === WebSocket.OPEN) return true;

            console.log('Open ' + self.Uri);

            // 建立连接完成后，设置其它参数
            ws = new WebSocket(self.Uri);
            ws.onopen = function (evt) {
                console.log("已经建立连接");

                ws.onclose = function (evt) {
                    console.log("已经关闭连接");
                };
                ws.onmessage = function (evt) {
                    self.onMessage(evt);
                };
                ws.onerror = function (evt) {
                    console.log(evt.message);

                    self.Socket = null;
                };

                self.Socket = ws;

                if (callback) callback();

                // 异步登录。注意this作用域
                if (self.UserName) setTimeout(function () { self.Login(); }, 100);
            };

            return true;
        },

        // 关闭
        Close: function () {
            var self = this;

            if (self._timer > 0) clearInterval(self._timer);

            self.Socket = null;
        },

        // 获取Socket
        getSocket: function () {
            var ws = this.Socket;
            if (ws && ws.readyState === WebSocket.OPEN) return ws;

            //return this.Open() ? this.Socket : null;
            return null;
        },

        // 注册动作到指定函数
        requestCallbacks: {},
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
                    this.Process(msg);
                } else if (msg.code) {
                    if (this.responseCallback) this.responseCallback(msg);
                }
                return;
            }
            if (this.OnReceive) this.OnReceive(evt.data);
        },

        // 处理收到的请求
        Process: function (msg) {
            if (this.requestCallbacks) {
                var callback = this.requestCallbacks[msg.action];
                if (callback) {
                    var code = 0;
                    var result = null;
                    try {
                        result = callback(msg.args);
                    } catch (e) {
                        code = -1;
                        result = e + '';
                    }

                    // 发出响应
                    if (code != 0 || result != null) {
                        var msg = { code: code, result: result };

                        // 发出json字符串
                        this.Send(JSON.stringify(msg));
                    }
                }
            }
        },

        // 发送数据
        Send: function (data) {
            var self = this;

            console.log('Send ' + data);

            var ws = self.getSocket();
            if (ws) return ws.send(data);

            self.Open(function () {
                var ws = self.getSocket();
                if (ws) return ws.send(data);
            });

            return true;
        },

        // 调用动作，并在收到响应时调用回调函数
        Invoke: function (action, args, callback) {
            var msg = { action: action, args: args };

            // 设定响应拦截委托
            this.responseCallback = callback;

            // 发出json字符串
            this.Send(JSON.stringify(msg));
        },

        // 登录
        _timer: 0,
        Login: function (callback) {
            var self = this;

            // 构造调用参数
            var args = { user: self.UserName, pass: self.PassWord };
            //console.log('Login ' + args);

            // 发送请求
            self.Invoke('Login', args, function (msg) {
                if (callback) callback(msg);

                // 定时心跳
                if (msg.Code == 0 && !self._timer) self._timer = setInterval(function () { self.Ping(); }, 30000);
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