using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Caching
{
    /// <summary>Redis客户端</summary>
    /// <remarks>
    /// 以极简原则进行设计，每个客户端不支持并行命令处理，可通过多客户端多线程解决。
    /// 收发共用64k缓冲区，所以命令请求和响应不能超过64k。
    /// </remarks>
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

        /// <summary>是否正在处理命令</summary>
        public Boolean Busy { get; private set; }
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
        private Stream GetStream(Boolean create)
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

                Client = null;
                tc.TryDispose();
                if (!create) return null;

                tc = new TcpClient
                {
                    SendTimeout = 5000,
                    ReceiveTimeout = 5000
                };
                tc.Connect(Server.Address, Server.Port);

                Client = tc;
                ns = tc.GetStream();
            }

            return ns;
        }

        /// <summary>收发缓冲区。不支持收发超过64k的大包</summary>
        private Byte[] _Buffer;

        private static Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n' };

        /// <summary>异步发出请求，并接收响应</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual Object SendCommand(String cmd, params Byte[][] args)
        {
            var isQuit = cmd == "QUIT";

            var ns = GetStream(!isQuit);
            if (ns == null) return null;

            // 验证登录
            if (!Logined && !Password.IsNullOrEmpty() && cmd != "AUTH")
            {
                var ars = SendCommand("AUTH", Password.GetBytes());
                if (ars as String != "OK") throw new Exception("登录失败！" + ars);

                Logined = true;
                LoginTime = DateTime.Now;
            }

            // 收发共用的缓冲区
            var buf = _Buffer;
            if (buf == null) _Buffer = buf = new Byte[64 * 1024];

            // *<number of arguments>\r\n$<number of bytes of argument 1>\r\n<argument data>\r\n
            // *1\r\n$4\r\nINFO\r\n

            var log = Log == null || Log == Logger.Null ? null : new StringBuilder();
            log?.Append(cmd);

            // 区分有参数和无参数
            if (args == null || args.Length == 0)
            {
                var str = "*1\r\n${0}\r\n{1}\r\n".F(cmd.Length, cmd);
                ns.Write(str.GetBytes());
            }
            else
            {
                var ms = new MemoryStream(buf);
                ms.SetLength(0);
                ms.Position = 0;

                var str = "*{2}\r\n${0}\r\n{1}\r\n".F(cmd.Length, cmd, 1 + args.Length);
                ms.Write(str.GetBytes());

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
                    ms.Write(str.GetBytes());
                    ms.Write(item);
                    ms.Write(NewLine);

                    if (ms.Length > 1400)
                    {
                        ms.WriteTo(ns);

                        // 从头开始
                        ms.SetLength(0);
                        ms.Position = 0;
                    }
                }
                if (ms.Length > 0) ms.WriteTo(ns);
            }
            if (log != null) WriteLog(log.ToString());

            // 接收
            var count = ns.Read(buf, 0, buf.Length);
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

            var header = (Char)rs[0];

            if (header == '$') return ReadBlock(rs);
            if (header == '*') return ReadBlocks(rs);

            rs = rs.Sub(1);

            var str2 = rs.ToStr().Trim();
            if (log != null) WriteLog("=> {0}", str2);

            if (header == '+') return str2;
            if (header == '-') throw new Exception(str2);
            if (header == ':') return str2;

            throw new NotSupportedException();
        }

        private Packet ReadBlock(Packet pk)
        {
            var rs = ReadPacket(pk);

            if (Log != null && Log != Logger.Null)
            {
                if (rs.Count <= 32)
                    WriteLog("=> {0}", rs.ToStr());
                else
                    WriteLog("=> [{0}]", rs.Count);
            }

            return rs;
        }

        private Packet[] ReadBlocks(Packet pk)
        {
            var header = (Char)pk[0];

            // 结果集数量
            var p = pk.IndexOf(NewLine);
            if (p <= 0) throw new Exception("无法解析响应 {0} [{1}]".F(header, pk.Count));

            var n = pk.Sub(1, p - 1).ToStr().ToInt();

            pk = pk.Sub(p + 2);
            if (Log != null && Log != Logger.Null) WriteLog("=> *{0} [{1}] {2}", n, pk.Count, pk.Sub(0, 32).ToStr().Replace(Environment.NewLine, "\\r\\n"));

            var arr = new Packet[n];
            for (var i = 0; i < n; i++)
            {
                var rs = ReadPacket(pk);
                arr[i] = rs;

                // 下一块，在前一块末尾加 \r\n
                pk = pk.Sub(rs.Offset + rs.Count + 2 - pk.Offset);
            }

            return arr;
        }

        private Packet ReadPacket(Packet pk)
        {
            var header = (Char)pk[0];

            var p = pk.IndexOf(NewLine);
            if (p <= 0) throw new Exception("无法解析响应 {0} [{1}]".F(header, pk.Count));

            // 解析长度
            var len = pk.Sub(1, p - 1).ToStr().ToInt();

            // 出错或没有内容
            if (len <= 0) return pk.Sub(p, 0);

            // 解析内容，跳过长度后的\r\n
            pk = pk.Sub(p + 2, len);

            return pk;
        }
        #endregion

        #region 主要方法
        /// <summary>执行命令。返回字符串、Packet、Packet[]</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Object Execute(String cmd, params Object[] args)
        {
            return SendCommand(cmd, args.Select(e => ToBytes(e)).ToArray());
        }

        /// <summary>执行命令。返回基本类型、对象、对象数组</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual TResult Execute<TResult>(String cmd, params Object[] args)
        {
            var rs = SendCommand(cmd, args.Select(e => ToBytes(e)).ToArray());
            if (rs is String str) return str.ChangeType<TResult>();

            if (rs is Packet pk) return FromBytes<TResult>(pk);

            if (rs is Packet[] pks)
            {
                var elmType = typeof(TResult).GetElementTypeEx();
                var arr = Array.CreateInstance(elmType, pks.Length);
                for (var i = 0; i < pks.Length; i++)
                {
                    arr.SetValue(FromBytes(pks[i], elmType), i);
                }
                return (TResult)(Object)arr;
            }

            return default;
        }

        /// <summary>心跳</summary>
        /// <returns></returns>
        public Boolean Ping() { return Execute<String>("PING") == "PONG"; }

        /// <summary>选择Db</summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public Boolean Select(Int32 db) { return Execute<String>("SELECT", db + "") == "OK"; }

        /// <summary>验证密码</summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public Boolean Auth(String password) { return Execute<String>("AUTH", password) == "OK"; }

        /// <summary>退出</summary>
        /// <returns></returns>
        public Boolean Quit() { return Execute<String>("QUIT") == "OK"; }

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
            if (secTimeout <= 0)
                return Execute<String>("SET", key, value) == "OK";
            else
                return Execute<String>("SETEX", key, secTimeout, value) == "OK";
        }

        /// <summary>读取</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(String key)
        {
            return Execute<T>("GET", key);
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

            var rs = SendCommand("MSET", ps.ToArray());

            return rs as String == "OK";
        }

        /// <summary>批量获取</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public IDictionary<String, T> GetAll<T>(IEnumerable<String> keys)
        {
            var ks = keys.ToArray();
            var rs = Execute("MGET", ks) as Packet[];

            var dic = new Dictionary<String, T>();
            for (var i = 0; i < rs.Length; i++)
            {
                dic[ks[i]] = FromBytes<T>(rs[i]);
            }

            return dic;
        }
        #endregion

        #region 辅助
        /// <summary>数值转字节数组</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual Byte[] ToBytes(Object value)
        {
            if (value == null) return new Byte[0];

            var type = value.GetType();
            if (type == typeof(Byte[])) return (Byte[])value;

            switch (type.GetTypeCode())
            {
                case TypeCode.Object: return value.ToJson().GetBytes();
                case TypeCode.String: return (value as String).GetBytes();
                default: return "{0}".F(value).GetBytes();
            }
        }

        /// <summary>字节数组转对象</summary>
        /// <param name="pk"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual Object FromBytes(Packet pk, Type type)
        {
            if (type == typeof(Byte[])) return pk.ToArray();

            var str = pk.ToStr().Trim('\"');
            if (type.GetTypeCode() == TypeCode.String) return str;
            if (type.GetTypeCode() != TypeCode.Object) return str.ChangeType(type);

            return str.ToJsonEntity(type);
        }

        /// <summary>字节数组转对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected T FromBytes<T>(Packet pk) { return (T)FromBytes(pk, typeof(T)); }
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