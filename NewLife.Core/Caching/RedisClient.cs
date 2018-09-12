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
                catch (ObjectDisposedException) { }
                catch (Exception ex) { XTrace.WriteException(ex); }
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
            catch (ObjectDisposedException) { }
            catch (Exception ex) { XTrace.WriteException(ex); }

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
        /// <returns></returns>
        protected virtual Object GetResponse(Stream ns)
        {
            var buf = new Byte[64 * 1024];

            // 接收
            var count = ns.Read(buf, 0, buf.Length);
            if (count == 0) return null;

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

            if (header == '$') return ReadBlock(rs, ns);
            if (header == '*') return ReadBlocks(rs, ns);

            var pk = rs.Slice(1);

            var str2 = pk.ToStr().Trim();

            var log = Log == null || Log == Logger.Null ? null : Pool.StringBuilder.Get();
            if (log != null) WriteLog("=> {0}", str2);

            if (header == '+') return str2;
            if (header == '-') throw new Exception(str2);
            if (header == ':') return str2;

            throw new InvalidDataException("无法解析响应 [{0}] [{1}]={2}".F(header, rs.Count, rs.ToHex(32, "-")));
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

            var rs = GetResponse(ns);

            if (isQuit) Logined = false;

            return rs;
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

        private Packet ReadBlock(Packet pk, Stream ms)
        {
            var rs = ReadPacket(pk, ms);

            if (Log != null && Log != Logger.Null)
            {
                if (rs.Count <= 32)
                    WriteLog("=> {0}", rs.ToStr());
                else
                    WriteLog("=> [{0}]", rs.Count);
            }

            return rs;
        }

        private Packet[] ReadBlocks(Packet pk, Stream ms)
        {
            var header = (Char)pk[0];

            // 结果集数量
            var p = pk.IndexOf(NewLine);
            if (p <= 0) throw new InvalidDataException("无法解析响应 {0} [{1}]".F(header, pk.Count));

            var n = pk.Slice(1, p - 1).ToStr().ToInt();

            pk = pk.Slice(p + 2);
            if (Log != null && Log != Logger.Null) WriteLog("=> *{0} [{1}] {2}", n, pk.Count, pk.Slice(0, 32).ToStr().Replace(Environment.NewLine, "\\r\\n"));

            var arr = new Packet[n];
            for (var i = 0; i < n; i++)
            {
                var rs = ReadPacket(pk, ms);
                arr[i] = rs;

                // 下一块，在前一块末尾加 \r\n
                pk = pk.Slice(rs.Offset + rs.Total + 2 - pk.Offset);
            }

            return arr;
        }

        private Packet ReadPacket(Packet pk, Stream ms)
        {
            // 数据不足，最小1字节长度+2换行+0数据+2换行
            if (pk.Total < 1 + 2 + 0 + 2) ReadMore(pk, ms, 1 + 2 + 0 + 2);

            var header = (Char)pk[0];
            if (header != '$')
            {
                // 如果一个响应包刚好结束，末尾两个字节可能不是\r\n，而是\0\0，然后\r\n出现在下一个包开头
                var k = pk.IndexOf(new[] { (Byte)'$' });
                if (k > 0)
                {
                    pk = pk.Slice(k, pk.Total - k);
                    header = (Char)pk[0];
                }
            }

            var p = pk.IndexOf(NewLine);
            if (p <= 0) throw new InvalidDataException("无法解析响应 [{0}] [{1}]={2}".F((Byte)header, pk.Count, pk.ToHex(32, "-")));

            // 解析长度
            var len = pk.Slice(1, p - 1).ToStr().ToInt();

            // 出错或没有内容
            if (len <= 0) return pk.Slice(p, 0);

            // 数据不足时，继续从网络流读取
            var dlen = pk.Total - (p + 2);
            var cur = pk;
            while (dlen < len)
            {
                // 需要读取更多数据，加2字节的结尾换行
                var over = len - dlen + 2;
                var count = ReadMore(cur, ms, over);
                if (cur.Next != null) cur = cur.Next;
                dlen += count;
            }

            // 解析内容，跳过长度后的\r\n
            pk = pk.Slice(p + 2, len);

            return pk;
        }

        private Int32 ReadMore(Packet pk, Stream ms, Int32 over)
        {
            var count = 0;
            var remain = pk.Data.Length - pk.Offset - pk.Count;

            // 优先使用缓冲区
            if (over <= remain && pk.Next == null)
            {
                // over是最低保障值，可以酌情扩大
                count = ms.Read(pk.Data, pk.Offset + pk.Count, remain);
                if (count > 0) pk.Set(pk.Data, pk.Offset, pk.Count + count);
            }
            else
            {
                // over是最低保障值，可以酌情扩大
                if (over < pk.Data.Length) over = pk.Data.Length;

                var buf = new Byte[over];
                count = ms.Read(buf, 0, over);
                if (count > 0) pk.Append(new Packet(buf, 0, count));
            }

            return count;
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
                _ps.Add(new Command(cmd, args));
                return default(TResult);
            }

            var type = typeof(TResult);
            var rs = Execute(cmd, args);
            if (rs is String str)
            {
                try
                {
                    return rs.ChangeType<TResult>();
                }
                catch (Exception ex)
                {
                    //if (type.GetTypeCode() != TypeCode.Object)
                    throw new Exception("不能把字符串[{0}]转为类型[{1}]".F(str, type.FullName), ex);
                }
            }

            if (rs is Packet pk) return FromBytes<TResult>(pk);

            if (rs is Packet[] pks)
            {
                if (typeof(TResult) == typeof(Packet[])) return (TResult)rs;

                var elmType = type.GetElementTypeEx();
                var arr = Array.CreateInstance(elmType, pks.Length);
                for (var i = 0; i < pks.Length; i++)
                {
                    arr.SetValue(FromBytes(pks[i], elmType), i);
                }
                return (TResult)(Object)arr;
            }

            return default(TResult);
        }

        private IList<Command> _ps;
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

            if (!requireResult) return null;

            // 获取响应
            var list = new List<Object>();
            foreach (var item in ps)
            {
                var rs = GetResponse(ns);
                list.Add(rs);
            }

            return list.ToArray();
        }

        class Command
        {
            public String Name { get; set; }
            public Object[] Args { get; set; }

            public Command(String name, Object[] args)
            {
                Name = name;
                Args = args;
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
            var ps = new List<Packet>();
            foreach (var item in values)
            {
                ps.Add(item.Key.GetBytes());
                ps.Add(ToBytes(item.Value));
            }

            var rs = ExecuteCommand("MSET", ps.ToArray());

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