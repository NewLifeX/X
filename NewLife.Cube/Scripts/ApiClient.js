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

        // 打开连接
        Open: function () {
            var ws = this.Socket;
            if (ws && ws.readyState === WebSocket.OPEN) return true;

            console.log('Open ' + this.Uri);

            ws = new WebSocket(this.Uri);
            ws.onopen = function (evt) {
                console.log("已经建立连接");
            };

            // 等到创建完成
            /*var exp = new Date().getTime() + 5000;
            while (ws.readyState !== WebSocket.OPEN) {
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

            return this.Open() ? this.Socket : null;
        },

        // 发送数据
        Send: function (data) {
            var ws = this.getSocket();
            if (!ws) return false;

            ws.send(data);

            return true;
        },

        // 收到数据
        onMessage: function (data) {
            evt.stopPropagation()
            evt.preventDefault()
            console.log(evt.data);
            if (this.OnReceive) this.OnReceive(data);
        }
    }

    ApiClient.fn.init.prototype = ApiClient.fn;

    return ApiClient;
})();