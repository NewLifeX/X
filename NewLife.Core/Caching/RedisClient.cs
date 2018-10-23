using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using NewLife.Collections;
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

                var timeout = 3_000;
                tc = new TcpClient
                {
                    SendTimeout = timeout,
                    ReceiveTimeout = timeout
                };
                //tc.Connect(Server.Address, Server.Port);
                // 采用异步来解决连接超时设置问题
                var ar = tc.BeginConnect(Server.Address, Server.Port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(timeout, false))
                {
                    tc.Close();
                    throw new TimeoutException($"连接[{Server}][{timeout}ms]超时！");
                }

                tc.EndConnect(ar);

                Client = tc;
                ns = tc.GetStream();
            }

            return ns;
        }

        ///// <summary>收发缓冲区。不支持收发超过64k的大包</summary>
        //private Byte[] _Buffer;

        private static Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n' };

        /// <summary>发出请求</summary>
        /// <param name="ms"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual void GetRequest(Stream ms, String cmd, Packet[] args)
        {
            // *<number of arguments>\r\n$<number of bytes of argument 1>\r\n<argument data>\r\n
            // *1\r\n$4\r\nINFO\r\n

            var log = Log == null || Log == Logger.Null ? null : Pool.StringBuilder.Get();
            log?.Append(cmd);

            /*
             * 一颗玲珑心
             * 九天下凡尘
             * 翩翩起菲舞
             * 霜摧砺石开
             */

            // 区分有参数和无参数
            if (args == null || args.Length == 0)
            {
                //var str = "*1\r\n${0}\r\n{1}\r\n".F(cmd.Length, cmd);
                ms.Write(GetHeaderBytes(cmd, 0));
            }
            else
            {
                //var str = "*{2}\r\n${0}\r\n{1}\r\n".F(cmd.Length, cmd, 1 + args.Length);
                ms.Write(GetHeaderBytes(cmd, args.Length));

                foreach (var item in args)
                {
                    var size = item.Total;
                    var sizes = size.ToString().GetBytes();
                    var len = 1 + sizes.Length + NewLine.Length * 2 + size;

                    if (log != null)
                    {
                        if (size <= 32)
                            log.AppendFormat(" {0}", item.ToStr());
                        else
                            log.AppendFormat(" [{0}]", size);
                    }

                    //str = "${0}\r\n".F(item.Length);
                    //ms.Write(str.GetBytes());
                    ms.WriteByte((Byte)'$');
                    ms.Write(sizes);
                    ms.Write(NewLine);
                    //ms.Write(item);
                    item.WriteTo(ms);
                    ms.Write(NewLine);
                }
            }
            if (log != null) WriteLog(log.Put(true));
        }

        /// <summary>接收响应</summary>
        /// <param name="ns"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected virtual IList<Object> GetResponse(Stream ns, Int32 count)
        {
            /*
             * 响应格式
             * 1：简单字符串，非二进制安全字符串，一般是状态回复。  +开头，例：+OK\r\n 
             * 2: 错误信息。-开头， 例：-ERR unknown command 'mush'\r\n
             * 3: 整型数字。:开头， 例：:1\r\n
             * 4：大块回复值，最大512M。  $开头+数据长度。 例：$4\r\mush\r\n
             * 5：多条回复。*开头， 例：*2\r\n$3\r\nfoo\r\n$3\r\nbar\r\n
             */

            var ms = new BufferedStream(ns);

            // 多行响应
            var list = new List<Object>();
            for (var i = 0; i < count; i++)
            {
                // 解析响应
                var header = (Char)ms.ReadByte();
                if (header == '$')
                {
                    list.Add(ReadBlock(ms));
                }
                else if (header == '*')
                {
                    list.Add(ReadBlocks(ms));
                }
                else
                {
                    // 字符串以换行为结束符
                    var str = ReadLine(ms);

                    var log = Log == null || Log == Logger.Null ? null : Pool.StringBuilder.Get();
                    if (log != null) WriteLog("=> {0}", str);

                    if (header == '+' || header == ':')
                        list.Add(str);
                    else if (header == '-')
                        throw new Exception(str);
                    else
                        throw new InvalidDataException("无法解析响应 [{0}]".F(header));
                }
            }

            return list;
        }

        /// <summary>发出请求</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual Object ExecuteCommand(String cmd, Packet[] args)
        {
            var isQuit = cmd == "QUIT";

            var ns = GetStream(!isQuit);
            if (ns == null) return null;

            // 验证登录
            CheckLogin(cmd);

            var ms = Pool.MemoryStream.Get();
            GetRequest(ms, cmd, args);

            if (ms.Length > 0) ms.WriteTo(ns);
            ms.Put();

            var rs = GetResponse(ns, 1);

            if (isQuit) Logined = false;

            return rs.FirstOrDefault();
        }

        private void CheckLogin(String cmd)
        {
            if (!Logined && !Password.IsNullOrEmpty() && cmd != "AUTH")
            {
                var ars = ExecuteCommand("AUTH", new Packet[] { Password.GetBytes() });
                if (ars as String != "OK") throw new Exception("登录失败！" + ars);

                Logined = true;
                LoginTime = DateTime.Now;
            }
        }

        /// <summary>重置。干掉历史残留数据</summary>
        public void Reset()
        {
            var ns = GetStream(false);
            if (ns == null) return;

            // 干掉历史残留数据
            var count = 0;
            if (ns is NetworkStream nss && nss.DataAvailable)
            {
                var buf = new Byte[1024];
                do
                {
                    count = ns.Read(buf, 0, buf.Length);
                } while (count > 0 && nss.DataAvailable);
            }
        }

        private Packet ReadBlock(Stream ms)
        {
            var rs = ReadPacket(ms);

            if (rs != null && Log != null && Log != Logger.Null)
            {
                if (rs.Count <= 32)
                    WriteLog("=> {0}", rs.ToStr());
                else
                    WriteLog("=> [{0}]", rs.Count);
            }

            return rs;
        }

        private Object[] ReadBlocks(Stream ms)
        {
            // 结果集数量
            var n = ReadLine(ms).ToInt(-1);
            if (n < 0) return new Object[0];

            //var ms = reader.BaseStream;
            var arr = new Object[n];
            for (var i = 0; i < n; i++)
            {
                var header = (Char)ms.ReadByte();
                if (header == '$')
                {
                    arr[i] = ReadPacket(ms);
                }
                else if (header == '*')
                {
                    arr[i] = ReadBlocks(ms);
                }
            }

            return arr;
        }

        private Packet ReadPacket(Stream ms)
        {
            var len = ReadLine(ms).ToInt(-1);
            if (len <= 0) return null;

            var buf = new Byte[len + 2];
            var p = 0;
            while (true)
            {
                // 等待，直到读完需要的数据，避免大包丢数据
                var count = ms.Read(buf, p, buf.Length - p);
                if (count <= 0) break;

                p += count;
            }

            return new Packet(buf, 0, p - 2);
        }

        private String ReadLine(Stream ms)
        {
            var sb = Pool.StringBuilder.Get();
            while (true)
            {
                var b = ms.ReadByte();
                if (b < 0) break;

                if (b == '\r')
                {
                    var b2 = ms.ReadByte();
                    if (b2 < 0) break;

                    if (b2 == '\n') break;

                    sb.Append((Char)b);
                    sb.Append((Char)b2);
                }
                else
                    sb.Append((Char)b);
            }

            return sb.Put(true);
        }
        #endregion

        #region 主要方法
        /// <summary>执行命令。返回字符串、Packet、Packet[]</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Object Execute(String cmd, params Object[] args) => ExecuteCommand(cmd, args.Select(e => ToBytes(e)).ToArray());

        /// <summary>执行命令。返回基本类型、对象、对象数组</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual TResult Execute<TResult>(String cmd, params Object[] args)
        {
            // 管道模式
            if (_ps != null)
            {
                _ps.Add(new Command(cmd, args, typeof(TResult)));
                return default(TResult);
            }

            var type = typeof(TResult);
            var rs = Execute(cmd, args);

            if (TryChangeType(rs, typeof(TResult), out var target)) return (TResult)target;

            return default(TResult);
        }

        /// <summary>尝试转换类型</summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual Boolean TryChangeType(Object value, Type type, out Object target)
        {
            if (value is String str)
            {
                try
                {
                    target = value.ChangeType(type);
                    return true;
                }
                catch (Exception ex)
                {
                    //if (type.GetTypeCode() != TypeCode.Object)
                    throw new Exception("不能把字符串[{0}]转为类型[{1}]".F(str, type.FullName), ex);
                }
            }

            if (value is Packet pk)
            {
                target = FromBytes(pk, type);
                return true;
            }

            if (value is Object[] pks)
            {
                if (type == typeof(Object[])) { target = value; return true; }
                if (type == typeof(Packet[])) { target = pks.Cast<Packet>().ToArray(); return true; }

                var elmType = type.GetElementTypeEx();
                var arr = Array.CreateInstance(elmType, pks.Length);
                for (var i = 0; i < pks.Length; i++)
                {
                    arr.SetValue(FromBytes(pks[i] as Packet, elmType), i);
                }
                target = arr;
                return true;
            }

            target = null;
            return false;
        }

        private IList<Command> _ps;
        /// <summary>管道命令个数</summary>
        public Int32 PipelineCommands => _ps == null ? 0 : _ps.Count;

        /// <summary>开始管道模式</summary>
        public virtual void StartPipeline()
        {
            if (_ps == null) _ps = new List<Command>();
        }

        /// <summary>结束管道模式</summary>
        /// <param name="requireResult">要求结果</param>
        public virtual Object[] StopPipeline(Boolean requireResult)
        {
            var ps = _ps;
            if (ps == null) return null;

            _ps = null;

            var ns = GetStream(true);
            if (ns == null) return null;

            // 整体打包所有命令
            var ms = Pool.MemoryStream.Get();
            foreach (var item in ps)
            {
                GetRequest(ms, item.Name, item.Args.Select(e => ToBytes(e)).ToArray());
            }

            // 整体发出
            if (ms.Length > 0) ms.WriteTo(ns);
            ms.Put();

            if (!requireResult) return new Object[ps.Count];

            // 获取响应
            var list = GetResponse(ns, ps.Count);
            for (var i = 0; i < list.Count; i++)
            {
                if (TryChangeType(list[i], ps[i].Type, out var target)) list[i] = target;
            }

            return list.ToArray();
        }

        class Command
        {
            public String Name { get; set; }
            public Object[] Args { get; set; }
            public Type Type { get; set; }

            public Command(String name, Object[] args, Type type)
            {
                Name = name;
                Args = args;
                Type = type;
            }
        }
        #endregion

        #region 基础功能
        /// <summary>心跳</summary>
        /// <returns></returns>
        public Boolean Ping() => Execute<String>("PING") == "PONG";

        /// <summary>选择Db</summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public Boolean Select(Int32 db) => Execute<String>("SELECT", db + "") == "OK";

        /// <summary>验证密码</summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public Boolean Auth(String password) => Execute<String>("AUTH", password) == "OK";

        /// <summary>退出</summary>
        /// <returns></returns>
        public Boolean Quit() => Execute<String>("QUIT") == "OK";

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
        public T Get<T>(String key) => Execute<T>("GET", key);

        /// <summary>批量设置</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public Boolean SetAll<T>(IDictionary<String, T> values)
        {
            //var ps = new List<Packet>();
            var ps = new List<Object>();
            foreach (var item in values)
            {
                //ps.Add(item.Key.GetBytes());
                //ps.Add(ToBytes(item.Value));
                ps.Add(item.Key);
                ps.Add(item.Value);
            }

            //var rs = ExecuteCommand("MSET", ps.ToArray());
            var rs = Execute<String>("MSET", ps.ToArray());

            return rs as String == "OK";
        }

        /// <summary>批量获取</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public IDictionary<String, T> GetAll<T>(IEnumerable<String> keys)
        {
            var ks = keys.ToArray();
            var rs = Execute("MGET", ks) as Object[];

            var dic = new Dictionary<String, T>();
            if (rs == null) return dic;

            for (var i = 0; i < rs.Length; i++)
            {
                dic[ks[i]] = FromBytes<T>(rs[i] as Packet);
            }

            return dic;
        }
        #endregion

        #region 辅助
        /// <summary>数值转字节数组</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual Packet ToBytes(Object value)
        {
            if (value == null) return new Byte[0];

            if (value is Packet pk) return pk;
            if (value is Byte[] buf) return buf;
            if (value is IAccessor acc) return acc.ToPacket();

            var type = value.GetType();
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
            if (pk == null) return null;

            if (type == typeof(Packet)) return pk;
            if (type == typeof(Byte[])) return pk.ToArray();
            if (type.As<IAccessor>()) return type.AccessorRead(pk);

            var str = pk.ToStr().Trim('\"');
            if (type.GetTypeCode() == TypeCode.String) return str;
            if (type.GetTypeCode() != TypeCode.Object) return str.ChangeType(type);

            return str.ToJsonEntity(type);
        }

        /// <summary>字节数组转对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected T FromBytes<T>(Packet pk) => (T)FromBytes(pk, typeof(T));

        private static ConcurrentDictionary<String, Byte[]> _cache0 = new ConcurrentDictionary<String, Byte[]>();
        private static ConcurrentDictionary<String, Byte[]> _cache1 = new ConcurrentDictionary<String, Byte[]>();
        private static ConcurrentDictionary<String, Byte[]> _cache2 = new ConcurrentDictionary<String, Byte[]>();
        private static ConcurrentDictionary<String, Byte[]> _cache3 = new ConcurrentDictionary<String, Byte[]>();
        /// <summary>获取命令对应的字节数组，全局缓存</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Byte[] GetHeaderBytes(String cmd, Int32 args = 0)
        {
            if (args == 0) return _cache0.GetOrAdd(cmd, k => "*1\r\n${0}\r\n{1}\r\n".F(k.Length, k).GetBytes());
            if (args == 1) return _cache1.GetOrAdd(cmd, k => "*2\r\n${0}\r\n{1}\r\n".F(k.Length, k).GetBytes());
            if (args == 2) return _cache2.GetOrAdd(cmd, k => "*3\r\n${0}\r\n{1}\r\n".F(k.Length, k).GetBytes());
            if (args == 3) return _cache3.GetOrAdd(cmd, k => "*4\r\n${0}\r\n{1}\r\n".F(k.Length, k).GetBytes());

            return "*{2}\r\n${0}\r\n{1}\r\n".F(cmd.Length, cmd, 1 + args).GetBytes();
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }
}