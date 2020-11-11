using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;

//#nullable enable
namespace NewLife.Caching
{
    /// <summary>Redis客户端</summary>
    /// <remarks>
    /// 以极简原则进行设计，每个客户端不支持并行命令处理，可通过多客户端多线程解决。
    /// </remarks>
    public class RedisClient : DisposeBase
    {
        #region 属性
        /// <summary>客户端</summary>
        public TcpClient Client { get; set; }

        /// <summary>内容类型</summary>
        public NetUri Server { get; set; }

        /// <summary>宿主</summary>
        public Redis Host { get; set; }

        /// <summary>是否已登录</summary>
        public Boolean Logined { get; private set; }

        /// <summary>登录时间</summary>
        public DateTime LoginTime { get; private set; }

        /// <summary>是否正在处理命令</summary>
        public Boolean Busy { get; private set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="redis"></param>
        /// <param name="server"></param>
        public RedisClient(Redis redis, NetUri server)
        {
            Host = redis;
            Server = server;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

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

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => Server + "";
        #endregion

        #region 核心方法
        /// <summary>新建连接获取数据流</summary>
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
                active = ns != null && tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
            }
            catch { }

            // 如果连接不可用，则重新建立连接
            if (!active)
            {
                Logined = false;

                Client = null;
                tc.TryDispose();
                if (!create) return null;

                var timeout = Host.Timeout;
                tc = new TcpClient
                {
                    SendTimeout = timeout,
                    ReceiveTimeout = timeout
                };
                //tc.Connect(Server.Address, Server.Port);

                try
                {
                    // 采用异步来解决连接超时设置问题
                    var ar = tc.BeginConnect(Server.Address, Server.Port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(timeout, true))
                    {
                        tc.Close();
                        throw new TimeoutException($"连接[{Server}][{timeout}ms]超时！");
                    }

                    tc.EndConnect(ar);

                    Client = tc;
                    ns = tc.GetStream();
                }
                catch
                {
                    // 连接异常时，放弃该客户端连接对象。上层连接池将切换新的服务端节点
                    Dispose();
                    throw;
                }
            }

            return ns;
        }

#if !NET4
        private async Task<Stream> GetStreamAsync(Boolean create)
        {
            var tc = Client;
            NetworkStream ns = null;

            // 判断连接是否可用
            var active = false;
            try
            {
                ns = tc?.GetStream();
                active = ns != null && tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
            }
            catch { }

            // 如果连接不可用，则重新建立连接
            if (!active)
            {
                Logined = false;

                Client = null;
                tc.TryDispose();
                if (!create) return null;

                var timeout = Host.Timeout;
                tc = new TcpClient
                {
                    SendTimeout = timeout,
                    ReceiveTimeout = timeout
                };

                await tc.ConnectAsync(Server.Address, Server.Port);

                Client = tc;
                ns = tc.GetStream();
            }

            return ns;
        }
#endif

        private static readonly Byte[] _NewLine = new[] { (Byte)'\r', (Byte)'\n' };

        /// <summary>发出请求</summary>
        /// <param name="ms"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <param name="oriArgs">原始参数，仅用于输出日志</param>
        /// <returns></returns>
        protected virtual void GetRequest(Stream ms, String cmd, Packet[] args, Object[] oriArgs)
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

                for (var i = 0; i < args.Length; i++)
                {
                    var item = args[i];
                    var size = item.Total;
                    var sizes = size.ToString().GetBytes();

                    // 指令日志。简单类型显示原始值，复杂类型显示序列化后字符串
                    if (log != null)
                    {
                        log.Append(' ');
                        var ori = oriArgs?[i];
                        switch (ori.GetType().GetTypeCode())
                        {
                            case TypeCode.Object:
                                log.AppendFormat("[{0}]{1}", size, item.ToStr(null, 0, 1024)?.TrimEnd());
                                break;
                            case TypeCode.DateTime:
                                log.Append(((DateTime)ori).ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                break;
                            default:
                                log.Append(ori);
                                break;
                        }
                    }

                    //str = "${0}\r\n".F(item.Length);
                    //ms.Write(str.GetBytes());
                    ms.WriteByte((Byte)'$');
                    ms.Write(sizes);
                    ms.Write(_NewLine);
                    //ms.Write(item);
                    item.CopyTo(ms);
                    ms.Write(_NewLine);
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
                var b = ms.ReadByte();
                if (b == -1) break;

                var header = (Char)b;
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
                    {
                        XTrace.WriteLine("无法解析响应[{0:X2}] {1}", (Byte)header, ms.ReadBytes().ToHex("-"));
                        throw new InvalidDataException($"无法解析响应 [{header}]");
                    }
                }
            }

            return list;
        }

        /// <summary>执行命令，发请求，取响应</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <param name="oriArgs">原始参数，仅用于输出日志</param>
        /// <returns></returns>
        protected virtual Object ExecuteCommand(String cmd, Packet[] args, Object[] oriArgs)
        {
            var isQuit = cmd == "QUIT";

            var ns = GetStream(!isQuit);
            if (ns == null) return null;

            // 验证登录
            CheckLogin(cmd);

            var ms = Pool.MemoryStream.Get();
            GetRequest(ms, cmd, args, oriArgs);

            // WriteTo与位置无关，CopyTo与位置相关
            //ms.Position = 0;
            if (ms.Length > 0) ms.WriteTo(ns);
            ms.Put();

            var rs = GetResponse(ns, 1);

            if (isQuit) Logined = false;

            return rs.FirstOrDefault();
        }

#if !NET4
        /// <summary>异步接收响应</summary>
        /// <param name="ns">网络数据流</param>
        /// <param name="count">响应个数</param>
        /// <param name="cancellationToken">取消通知</param>
        /// <returns></returns>
        protected virtual async Task<IList<Object>> GetResponseAsync(Stream ns, Int32 count, CancellationToken cancellationToken)
        {
            /*
             * 响应格式
             * 1：简单字符串，非二进制安全字符串，一般是状态回复。  +开头，例：+OK\r\n 
             * 2: 错误信息。-开头， 例：-ERR unknown command 'mush'\r\n
             * 3: 整型数字。:开头， 例：:1\r\n
             * 4：大块回复值，最大512M。  $开头+数据长度。 例：$4\r\mush\r\n
             * 5：多条回复。*开头， 例：*2\r\n$3\r\nfoo\r\n$3\r\nbar\r\n
             */

            var list = new List<Object>();
            var ms = ns;

            // 取巧进行异步操作，只要异步读取到第一个字节，后续同步读取
            var buf = new Byte[1];
            if (cancellationToken == CancellationToken.None) cancellationToken = new CancellationTokenSource(Host.Timeout).Token;
            var n = await ms.ReadAsync(buf, 0, buf.Length, cancellationToken);
            if (n <= 0) return list;

            var header = (Char)buf[0];

            // 多行响应
            for (var i = 0; i < count; i++)
            {
                // 解析响应
                if (i > 0)
                {
                    var b = ms.ReadByte();
                    if (b == -1) break;

                    header = (Char)b;
                }

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
                    {
                        XTrace.WriteLine("无法解析响应[{0:X2}] {1}", (Byte)header, ms.ReadBytes().ToHex("-"));
                        throw new InvalidDataException($"无法解析响应 [{header}]");
                    }
                }
            }

            return list;
        }

        /// <summary>异步执行命令，发请求，取响应</summary>
        /// <param name="cmd">命令</param>
        /// <param name="args">参数数组</param>
        /// <param name="oriArgs">原始参数，仅用于输出日志</param>
        /// <param name="cancellationToken">取消通知</param>
        /// <returns></returns>
        protected virtual async Task<Object> ExecuteCommandAsync(String cmd, Packet[] args, Object[] oriArgs, CancellationToken cancellationToken)
        {
            var isQuit = cmd == "QUIT";

            var ns = await GetStreamAsync(!isQuit);
            if (ns == null) return null;

            // 验证登录
            CheckLogin(cmd);

            var ms = Pool.MemoryStream.Get();
            GetRequest(ms, cmd, args, oriArgs);

            // WriteTo与位置无关，CopyTo与位置相关
            ms.Position = 0;
            if (ms.Length > 0) await ms.CopyToAsync(ns, 4096, cancellationToken);
            ms.Put();

            await ns.FlushAsync(cancellationToken);

            var rs = await GetResponseAsync(ns, 1, cancellationToken);

            if (isQuit) Logined = false;

            return rs.FirstOrDefault();
        }
#endif

        private void CheckLogin(String cmd)
        {
            if (Logined) return;
            if (cmd.EqualIgnoreCase("Auth", "Select")) return;

            if (!Host.Password.IsNullOrEmpty() /*&& cmd != "AUTH"*/)
            {
                //var ars = ExecuteCommand("AUTH", new Packet[] { Host.Password.GetBytes() });
                //if (ars as String != "OK") throw new Exception("登录失败！" + ars);

                if (!Auth(Host.UserName, Host.Password)) throw new Exception("登录失败！");
            }

            if (Host.Db > 0) Select(Host.Db);

            Logined = true;
            LoginTime = DateTime.Now;
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

            if (rs is Packet pk && Log != null && Log != Logger.Null)
            {
                WriteLog("=> [{0}]{1}", pk.Count, pk.ToStr(null, 0, 1024)?.TrimEnd());
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
                var b = ms.ReadByte();
                if (b == -1) break;

                var header = (Char)b;
                if (header == '$')
                {
                    arr[i] = ReadPacket(ms);
                }
                else if (header == '+' || header == ':')
                {
                    arr[i] = ReadLine(ms);
                }
                else if (header == '*')
                {
                    arr[i] = ReadBlocks(ms);
                }

                if (arr[i] is Packet pk && Log != null && Log != Logger.Null)
                {
                    WriteLog("=> [{0}]{1}", pk.Count, pk.ToStr(null, 0, 1024)?.TrimEnd());
                }
            }

            return arr;
        }

        private static Packet ReadPacket(Stream ms)
        {
            var len = ReadLine(ms).ToInt(-1);
            if (len <= 0) return null;
            //if (len <= 0) throw new InvalidDataException();

            var buf = new Byte[len + 2];
            var p = 0;
            while (p < buf.Length)
            {
                // 等待，直到读完需要的数据，避免大包丢数据
                var count = ms.Read(buf, p, buf.Length - p);
                if (count <= 0) break;

                p += count;
            }

            return new Packet(buf, 0, p - 2);
        }

        private static String ReadLine(Stream ms)
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
        public virtual Object Execute(String cmd, params Object[] args)
        {
            using var span = Host.Tracer?.NewSpan($"redis:{Server.Host ?? Server.Address.ToString()}:{cmd}", args);

            return ExecuteCommand(cmd, args.Select(e => Host.Encoder.Encode(e)).ToArray(), args);
        }

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
                return default;
            }

            var rs = Execute(cmd, args);
            if (rs is TResult rs2) return rs2;
            if (rs == null) return default;
            if (rs != null && TryChangeType(rs, typeof(TResult), out var target)) return (TResult)target;

            return default;
        }

        /// <summary>尝试执行命令。返回基本类型、对象、对象数组</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Boolean TryExecute<TResult>(String cmd, Object[] args, out TResult value)
        {
            var rs = Execute(cmd, args);
            if (rs is TResult rs2)
            {
                value = rs2;
                return true;
            }

            value = default;
            if (rs == null) return false;

            if (rs != null && TryChangeType(rs, typeof(TResult), out var target)) value = (TResult)target;

            return true;
        }

#if NET4
        /// <summary>异步执行命令。返回基本类型、对象、对象数组</summary>
        /// <param name="cmd">命令</param>
        /// <param name="args">参数数组</param>
        /// <param name="cancellationToken">取消通知</param>
        /// <returns></returns>
        public virtual Task<TResult> ExecuteAsync<TResult>(String cmd, Object[] args, CancellationToken cancellationToken = default) => throw new NotSupportedException();
#else
        /// <summary>异步执行命令。返回字符串、Packet、Packet[]</summary>
        /// <param name="cmd">命令</param>
        /// <param name="args">参数数组</param>
        /// <param name="cancellationToken">取消通知</param>
        /// <returns></returns>
        public virtual async Task<Object> ExecuteAsync(String cmd, Object[] args, CancellationToken cancellationToken = default)
        {
            using var span = Host.Tracer?.NewSpan($"redis:{Server.Host ?? Server.Address.ToString()}:{cmd}", args);

            return await ExecuteCommandAsync(cmd, args.Select(e => Host.Encoder.Encode(e)).ToArray(), args, cancellationToken);
        }

        /// <summary>异步执行命令。返回基本类型、对象、对象数组</summary>
        /// <param name="cmd">命令</param>
        /// <param name="args">参数数组</param>
        /// <returns></returns>
        public virtual async Task<TResult> ExecuteAsync<TResult>(String cmd, params Object[] args) => await ExecuteAsync<TResult>(cmd, args, CancellationToken.None);

        /// <summary>异步执行命令。返回基本类型、对象、对象数组</summary>
        /// <param name="cmd">命令</param>
        /// <param name="args">参数数组</param>
        /// <param name="cancellationToken">取消通知</param>
        /// <returns></returns>
        public virtual async Task<TResult> ExecuteAsync<TResult>(String cmd, Object[] args, CancellationToken cancellationToken)
        {
            // 管道模式
            if (_ps != null)
            {
                _ps.Add(new Command(cmd, args, typeof(TResult)));
                return default;
            }

            var rs = await ExecuteAsync(cmd, args, cancellationToken);
            if (rs is TResult rs2) return rs2;
            if (rs == null) return default;
            if (rs != null && TryChangeType(rs, typeof(TResult), out var target)) return (TResult)target;

            return default;
        }
#endif

        /// <summary>尝试转换类型</summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual Boolean TryChangeType(Object value, Type type, out Object target)
        {
            target = null;

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
                    throw new Exception($"不能把字符串[{str}]转为类型[{type.FullName}]", ex);
                }
            }

            if (value is Packet pk)
            {
                target = Host.Encoder.Decode(pk, type);
                return true;
            }

            if (value is Object[] objs)
            {
                if (type == typeof(Object[])) { target = value; return true; }
                if (type == typeof(Packet[])) { target = objs.Cast<Packet>().ToArray(); return true; }

                // 基础类型遇到空结果时返回默认值
                if (objs.Length == 0 && Type.GetTypeCode(type) != TypeCode.Object) return false;

                var elmType = type.GetElementTypeEx();
                var arr = Array.CreateInstance(elmType, objs.Length);
                for (var i = 0; i < objs.Length; i++)
                {
                    if (objs[i] is Packet pk3)
                        arr.SetValue(Host.Encoder.Decode(pk3, elmType), i);
                    else if (objs[i] != null && objs[i].GetType().As(elmType))
                        arr.SetValue(objs[i], i);
                }
                target = arr;
                return true;
            }

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

            // 验证登录
            CheckLogin(null);

            // 整体打包所有命令
            var ms = Pool.MemoryStream.Get();
            foreach (var item in ps)
            {
                GetRequest(ms, item.Name, item.Args.Select(e => Host.Encoder.Encode(e)).ToArray(), item.Args);
            }

            // 整体发出
            if (ms.Length > 0) ms.WriteTo(ns);
            ms.Put();

            if (!requireResult) return new Object[ps.Count];

            // 获取响应
            var list = GetResponse(ns, ps.Count);
            for (var i = 0; i < list.Count; i++)
            {
                if (TryChangeType(list[i], ps[i].Type, out var target) && target != null) list[i] = target;
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
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public Boolean Auth(String username, String password)
        {
            var rs = username.IsNullOrEmpty() ?
                Execute<String>("AUTH", password) :
                Execute<String>("AUTH", username, password);

            return rs == "OK";
        }

        /// <summary>退出</summary>
        /// <returns></returns>
        public Boolean Quit() => Execute<String>("QUIT") == "OK";
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

                if (item.Value == null) throw new NullReferenceException();
                ps.Add(item.Value);
            }

            //var rs = ExecuteCommand("MSET", ps.ToArray());
            var rs = Execute<String>("MSET", ps.ToArray());

            return rs == "OK";
        }

        /// <summary>批量获取</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public IDictionary<String, T> GetAll<T>(IEnumerable<String> keys)
        {
            var ks = keys.ToArray();

            var dic = new Dictionary<String, T>();
            if (Execute("MGET", ks) is not Object[] rs) return dic;

            for (var i = 0; i < ks.Length && i < rs.Length; i++)
            {
                if (rs[i] is Packet pk) dic[ks[i]] = (T)Host.Encoder.Decode(pk, typeof(T));
            }

            return dic;
        }
        #endregion

        #region 辅助
        private static readonly ConcurrentDictionary<String, Byte[]> _cache0 = new ConcurrentDictionary<String, Byte[]>();
        private static readonly ConcurrentDictionary<String, Byte[]> _cache1 = new ConcurrentDictionary<String, Byte[]>();
        private static readonly ConcurrentDictionary<String, Byte[]> _cache2 = new ConcurrentDictionary<String, Byte[]>();
        private static readonly ConcurrentDictionary<String, Byte[]> _cache3 = new ConcurrentDictionary<String, Byte[]>();
        /// <summary>获取命令对应的字节数组，全局缓存</summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Byte[] GetHeaderBytes(String cmd, Int32 args = 0)
        {
            if (args == 0) return _cache0.GetOrAdd(cmd, k => $"*1\r\n${k.Length}\r\n{k}\r\n".GetBytes());
            if (args == 1) return _cache1.GetOrAdd(cmd, k => $"*2\r\n${k.Length}\r\n{k}\r\n".GetBytes());
            if (args == 2) return _cache2.GetOrAdd(cmd, k => $"*3\r\n${k.Length}\r\n{k}\r\n".GetBytes());
            if (args == 3) return _cache3.GetOrAdd(cmd, k => $"*4\r\n${k.Length}\r\n{k}\r\n".GetBytes());

            return $"*{1 + args}\r\n${cmd.Length}\r\n{cmd}\r\n".GetBytes();
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
//#nullable restore