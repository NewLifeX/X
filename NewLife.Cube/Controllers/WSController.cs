using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;
using NewLife.Log;

namespace NewLife.Cube.Controllers
{
    /// <summary>WebSocket控制器</summary>
    public class WSController : ApiController
    {
        /// <summary>获取</summary>
        /// <returns></returns>
        public HttpResponseMessage Get()
        {
            if (HttpContext.Current.IsWebSocketRequest)
                HttpContext.Current.AcceptWebSocketRequest(Process);

            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }

        private async Task Process(AspNetWebSocketContext arg)
        {
            var socket = arg.WebSocket;
            while (true)
            {
                var buffer = new ArraySegment<Byte>(new Byte[1024]);
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (socket.State != WebSocketState.Open) break;

                var msg = buffer.Array.ToStr(null, 0, result.Count);
                XTrace.WriteLine("WebSocket [{0}] 收到：{1}", arg.UserHostName, msg);

                var str = "收到：" + msg + " @" + DateTime.Now.ToFullString();
                buffer = new ArraySegment<Byte>(str.GetBytes());
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}