using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Caching
{
    /// <summary>Redis客户端</summary>
    public class RedisClient : DisposeBase
    {
        #region 属性
        /// <summary>客户端</summary>
        public TcpClient Client { get; set; }

        /// <summary>内容类型</summary>
        public NetUri Server { get; set; }

        /// <summary>密码</summary>
        public String Password { get; set; }

        /// <summary>是否已登录</summary>
        public Boolean Logined { get; private set; }

        /// <summary>登录时间</summary>
        public DateTime LoginTime { get; private set; }
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            // 销毁时退出
            if (Logined)
            {
                try
                {
                    var tc = Client;
                    if (tc != null && tc.Connected && tc.GetStream() != null) Quit();
                }
                catch { }
            }

            Client.TryDispose();
        }
        #endregion

        #region 核心方法
        /// <summary>异步请求</summary>
        /// <param name="create">新建连接</param>
        /// <returns></returns>
        private async Task<Stream> GetStreamAsync(Boolean create)
        {
            var tc = Client;
            NetworkStream ns = null;

            // 判断连接是否可用
            var active = false;
            try
            {
                ns = tc?.GetStream();
                active = ns != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
            }
            catch { }

            // 如果连接不可用，则重新建立连接
            if (!active)
            {
                Logined = false;

                tc.TryDispose();

                tc = new TcpClient
                {
                    SendTimeout = 5000,
                    ReceiveTimeout = 5000
                };
                await tc.ConnectAsync(Server.Address, Server.Port);

                Client = tc;
                ns = tc.GetStream();
            }

            return ns;
        }

        private static Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n' };
        /// <summary>异步发出请求，并接收响应</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual async Task<Object> SendAsync(String cmd, params Byte[][] args)
        {
            var isQuit = cmd == "QUIT";

            var ns = await GetStreamAsync(isQuit);
            if (ns == null) return null;

            // 验证登录
            if (!Logined && !Password.IsNullOrEmpty() && cmd != "AUTH")
            {
                var ars = await SendAsync("AUTH", Password.GetBytes());
                if (ars as String != "OK") throw new Exception("登录失败！" + ars);

                Logined = true;
                LoginTime = DateTime.Now;
            }

            // *<number of arguments>\r\n$<number of bytes of argument 1>\r\n<argument data>\r\n
            // *1\r\n$4\r\nINFO\r\n

            var log = Log == null && Log == Logger.Null ? null : new StringBuilder();
            log?.Append(cmd);

            // 区分有参数和无参数
            if (args == null || args.Length == 0)
            {
                var str = "*1\r\n${0}\r\n{1}\r\n".F(cmd.Length, cmd);
                ns.Write(str.GetBytes());
            }
            else
            {
                var str = "*{2}\r\n${0}\r\n{1}\r\n".F(cmd.Length, cmd, 1 + args.Length);
                ns.Write(str.GetBytes());

                foreach (var item in args)
                {
                    if (log != null)
                    {
                        if (item.Length <= 32)
                            log.AppendFormat(" {0}", item.ToStr());
                        else
                            log.AppendFormat(" [{0}]", item.Length);
                    }

                    str = "${0}\r\n".F(item.Length);
                    ns.Write(str.GetBytes());
                    ns.Write(item);
                    ns.Write(NewLine);
                }
            }
            if (log != null) WriteLog(log.ToString());

            // 接收
            //var source = new CancellationTokenSource(15000);
            var buf = new Byte[64 * 1024];
            var count = await ns.ReadAsync(buf, 0, buf.Length);
            if (count == 0) return null;

            if (isQuit) Logined = false;

            /*
             * 响应格式
             * 1：简单字符串，非二进制安全字符串，一般是状态回复。  +开头，例：+OK\r\n 
             * 2: 错误信息。-开头， 例：-ERR unknown command 'mush'\r\n
             * 3: 整型数字。:开头， 例：:1\r\n
             * 4：大块回复值，最大512M。  $开头+数据长度。 例：$4\r\mush\r\n
             * 5：多条回复。*开头， 例：*2\r\n$3\r\nfoo\r\n$3\r\nbar\r\n
             */

            // 解析响应
            var rs = new Packet(buf, 0, count);

            var header = rs[0];
            rs = rs.Sub(1);

            log.Clear();

            if (header == '$')
            {
                var p = (Int32)rs.Data.IndexOf(NewLine) - rs.Offset;
                if (p > 0)
                {
                    var len = rs.Sub(0, p).ToStr().ToInt();

                    p += 2;
                    rs = rs.Sub(p, rs.Count - p - 2);

                    if (log != null)
                    {
                        if (rs.Count <= 32)
                            WriteLog("=> {0}", rs.ToStr());
                        else
                            WriteLog("=> [{0}]", rs.Count);
                    }

                    return rs;
                }
            }

            var str2 = rs.ToStr().Trim();
            if (log != null) WriteLog("=> {0}", str2);

            if (header == '+') return str2;
            if (header == '-') throw new Exception(str2);
            //if (header == ':') return str2.ToInt();
            if (header == ':') return str2;

            throw new NotSupportedException();
        }
        #endregion

        #region 缓冲池
        #endregion

        #region 主要方法
        /// <summary>执行命令</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Object Execute(String cmd, params String[] args)
        {
            return Task.Run(() => SendAsync(cmd, args.Select(e => e.GetBytes()).ToArray())).Result;
        }

        /// <summary>心跳</summary>
        /// <returns></returns>
        public Boolean Ping() { return Execute("PING") as String == "PONG"; }

        /// <summary>选择Db</summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public Boolean Select(Int32 db) { return Execute("SELECT", db + "") as String == "OK"; }

        /// <summary>验证密码</summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public Boolean Auth(String password) { return Execute("AUTH", password) as String == "OK"; }

        /// <summary>退出</summary>
        /// <returns></returns>
        public Boolean Quit() { return Execute("QUIT") as String == "OK"; }

        /// <summary>获取信息</summary>
        /// <returns></returns>
        public IDictionary<String, String> GetInfo()
        {
            var rs = Execute("INFO") as Packet;
            if (rs == null || rs.Count == 0) return null;

            var inf = rs.ToStr();
            return inf.SplitAsDictionary(":", "\r\n");
        }
        #endregion

        #region 获取设置
        /// <summary>设置</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="secTimeout">超时时间</param>
        /// <returns></returns>
        public Boolean Set<T>(String key, T value, Int32 secTimeout = 0)
        {
            var val = ToBytes(value);

            Object rs = null;
            if (secTimeout <= 0)
                rs = Task.Run(() => SendAsync("SET", key.GetBytes(), val)).Result;
            else
                rs = Task.Run(() => SendAsync("SETEX", key.GetBytes(), secTimeout.ToString().GetBytes(), val)).Result;

            return rs as String == "OK";
        }

        /// <summary>读取</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(String key)
        {
            //var rs = Execute("GET", key.GetBytes());
            var rs = Task.Run(() => SendAsync("GET", key.GetBytes())).Result;
            var pk = rs as Packet;

            return FromBytes<T>(pk);
        }

        /// <summary>批量设置</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public Boolean SetAll<T>(IDictionary<String, T> values)
        {
            var ps = new List<Byte[]>();
            foreach (var item in values)
            {
                ps.Add(item.Key.GetBytes());
                ps.Add(ToBytes(item.Value));
            }

            var rs = Task.Run(() => SendAsync("MSET", ps.ToArray())).Result;

            return rs as String == "OK";
        }

        /// <summary>批量获取</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public IDictionary<String, T> GetAll<T>(IEnumerable<String> keys)
        {
            var rs = Execute("MGET", keys.ToArray());

            var dic = new Dictionary<String, T>();
            throw new NotSupportedException();

            return dic;
        }
        #endregion

        #region 高级操作
        #endregion

        #region 辅助
        /// <summary>数值转字节数组</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual Byte[] ToBytes<T>(T value)
        {
            Byte[] val = null;

            var type = typeof(T);
            if (type == typeof(Byte[]))
                val = (Byte[])(Object)value;
            else if (type.GetTypeCode() != TypeCode.Object)
                val = "{0}".F(value).GetBytes();
            else
                val = val.ToJson().GetBytes();

            return val;
        }

        /// <summary>字节数组转对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected virtual T FromBytes<T>(Packet pk)
        {

            var type = typeof(T);
            if (type == typeof(Byte[])) return (T)(Object)pk.ToArray();

            var str = pk.ToStr().Trim('\"');
            if (type.GetTypeCode() == TypeCode.String) return (T)(Object)str;
            if (type.GetTypeCode() != TypeCode.Object) return str.ChangeType<T>();

            return str.ToJsonEntity<T>();
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}