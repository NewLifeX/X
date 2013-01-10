using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using NewLife.Configuration;
using NewLife.Exceptions;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Messaging
{
    /// <summary>消息实体基类</summary>
    /// <remarks>
    /// 用消息实体来表达行为和数据，更加直观。
    /// 同时，指定一套序列化和反序列化机制，实现消息实体与传输形式（二进制数据、XML、Json）的互相转换。
    /// 如果消息较为简单，建议重载<see cref="OnWrite"/>和<see cref="OnRead"/>以得到更好的性能。
    /// 
    /// 消息实体仿照Windows消息来设计，拥有一部分系统内置消息，同时允许用户自定义消息。
    /// </remarks>
    public abstract class Message
    {
        #region 属性
        /// <summary>消息类型</summary>
        /// <remarks>第一个字节的第一位决定是否存在消息头。</remarks>
        [XmlIgnore]
        public abstract MessageKind Kind { get; }

        [NonSerialized]
        private Object _UserState;
        /// <summary>在消息处理过程中附带的用户对象。不参与序列化</summary>
        [XmlIgnore]
        public Object UserState { get { return _UserState; } set { _UserState = value; } }
        #endregion

        #region 构造、注册
        static Message()
        {
            Init();
        }

        /// <summary>初始化</summary>
        static void Init()
        {
            var container = ObjectContainer.Current;
            var asm = Assembly.GetExecutingAssembly();
            // 搜索已加载程序集里面的消息类型
            foreach (var item in AssemblyX.FindAllPlugins(typeof(Message), true))
            {
                var msg = TypeX.CreateInstance(item) as Message;
                if (msg != null)
                {
                    if (item.Assembly != asm && msg.Kind < MessageKind.UserDefine) throw new XException("不允许{0}采用小于{1}的保留编码{2}！", item.FullName, MessageKind.UserDefine, msg.Kind);

                    container.Register(typeof(Message), null, msg, msg.Kind);
                }
                //if (msg != null) container.Register<Message>(msg, msg.Kind);
            }
        }
        #endregion

        #region 序列化/反序列化
        /// <summary>序列化当前消息到流中</summary>
        /// <param name="stream">数据流</param>
        /// <param name="rwkind">序列化类型</param>
        public void Write(Stream stream, RWKinds rwkind = RWKinds.Binary)
        {
            // 二进制增加头部
            if (rwkind == RWKinds.Binary)
            {
                // 基类写入编号，保证编号在最前面
                stream.WriteByte((Byte)Kind);
            }
            else
            {
                var n = (Byte)Kind;
                var bts = Encoding.ASCII.GetBytes(n.ToString());
                stream.Write(bts, 0, bts.Length);
            }

            OnWrite(stream, rwkind);
        }

        /// <summary>把消息写入流中，默认调用序列化框架</summary>
        /// <param name="stream">数据流</param>
        /// <param name="rwkind">序列化类型</param>
        protected virtual void OnWrite(Stream stream, RWKinds rwkind)
        {
            var writer = RWService.CreateWriter(rwkind);
            writer.Stream = stream;
            OnReadWriteSet(writer);
            writer.Settings.Encoding = new UTF8Encoding(false);

            if (Debug)
            {
                writer.Debug = true;
                writer.EnableTraceStream();
            }

            writer.WriteObject(this);
            writer.Flush();
        }

        /// <summary>序列化为数据流</summary>
        /// <param name="rwkind"></param>
        /// <returns></returns>
        public MemoryStream GetStream(RWKinds rwkind = RWKinds.Binary)
        {
            var ms = new MemoryStream();
            Write(ms, rwkind);
            ms.Position = 0;
            return ms;
        }

        /// <summary>从流中读取消息</summary>
        /// <param name="stream">数据流</param>
        /// <param name="rwkind"></param>
        /// <param name="ignoreException">忽略异常。如果忽略异常，读取失败时将返回空，并还原数据流位置</param>
        /// <returns></returns>
        public static Message Read(Stream stream, RWKinds rwkind = RWKinds.Binary, Boolean ignoreException = false)
        {
            if (stream == null || stream.Length - stream.Position < 1) return null;

            #region 根据第一个字节判断消息类型
            var start = stream.Position;
            // 消息类型，不同序列化方法的识别方式不同
            var kind = (MessageKind)0;
            Type type = null;

            if (rwkind == RWKinds.Binary)
            {
                // 检查第一个字节
                var ch = stream.ReadByte();
                if (ch < 0) return null;

                kind = (MessageKind)ch;
            }
            else
            {
                // 前面的数字表示消息种类
                var sb = new StringBuilder(32);
                Char c;
                while (true)
                {
                    c = (Char)stream.ReadByte();
                    if (c < '0' || c > '9') break;
                    sb.Append(c);
                }
                // 多读了一个，退回去
                stream.Seek(-1, SeekOrigin.Current);
                kind = (MessageKind)Convert.ToByte(sb.ToString());
            }
            #endregion

            #region 识别消息类型
            if (type == null) type = ObjectContainer.Current.ResolveType<Message>(kind);
            if (type == null)
            {
                if (!ignoreException) throw new XException("无法识别的消息类型（Kind={0}）！", kind);

                stream.Position = start;
                return null;
            }
            #endregion

            #region 读取消息
            var msg = TypeX.CreateInstance(type, null) as Message;
            if (stream.Position == stream.Length) return msg;

            try
            {
                var rs = msg.OnRead(stream, rwkind);
                if (!rs) throw new XException("数据格式不正确！");

                return msg;
            }
            catch (Exception ex)
            {
                if (ignoreException)
                {
                    stream.Position = start;
                    return null;
                }

                var em = ex.Message;
                if (DumpStreamWhenError)
                {
                    stream.Position = start;
                    var bin = String.Format("{0:yyyy_MM_dd_HHmmss_fff}.msg", DateTime.Now);
                    //bin = Path.Combine(XTrace.LogPath, bin);
                    //if (!Path.IsPathRooted(bin)) bin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, bin);
                    //bin = Path.GetFullPath(bin);
                    //var dir = Path.GetDirectoryName(bin);
                    //if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    bin = XTrace.LogPath.CombinePath(bin).GetFullPath().EnsureDirectory();
                    File.WriteAllBytes(bin, stream.ReadBytes());
                    em = String.Format("已Dump数据流到{0}。{1}", bin, em);
                }
                throw new XException(ex, "无法从数据流中读取{0}（Kind={1}）消息！{2}", type.Name, kind, em);
            }
            #endregion
        }

        /// <summary>从流中读取消息内容，默认调用序列化框架</summary>
        /// <param name="stream"></param>
        /// <param name="rwkind"></param>
        /// <returns></returns>
        protected virtual Boolean OnRead(Stream stream, RWKinds rwkind)
        {
            var reader = RWService.CreateReader(rwkind);
            reader.Stream = stream;
            OnReadWriteSet(reader);

            if (Debug)
            {
                reader.Debug = true;
                reader.EnableTraceStream();
            }

            // 传msg进去，因为是引用类型，所以问题不大
            Object msg = this;
            return reader.ReadObject(msg.GetType(), ref msg, null);
        }

        /// <summary>从流中读取消息</summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static TMessage Read<TMessage>(Stream stream) where TMessage : Message
        {
            return Read(stream) as TMessage;
        }

        /// <summary>读写前设置。每个消息可根据自己需要进行调整</summary>
        /// <param name="rw"></param>
        protected virtual void OnReadWriteSet(IReaderWriter rw)
        {
            var setting = rw.Settings;
            //setting.IsBaseFirst = true;
            //setting.EncodeInt = true;
            setting.UseTypeFullName = false;
            setting.UseObjRef = true;

            if (setting is BinarySettings)
            {
                var bset = setting as BinarySettings;
                bset.EncodeInt = true;
            }
            else
            {
                //setting.Encoding = new UTF8Encoding(false);
            }
        }
        #endregion

        #region 方法
        /// <summary>探测消息类型，不移动流指针</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MessageKind PeekKind(Stream stream)
        {
            var n = stream.ReadByte();
            stream.Seek(-1, SeekOrigin.Current);
            return (MessageKind)(n & 0x7F);
        }

        /// <summary>探测消息类型，不移动流指针</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Type PeekType(Stream stream)
        {
            var kind = PeekKind(stream);
            return ObjectContainer.Current.ResolveType<Message>(kind);
        }

        /// <summary>从源消息克隆设置和可序列化成员数据</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public virtual Message CopyFrom(Message msg)
        {
            if (msg != null)
            {
                if (msg.GetType() != this.GetType()) throw new XException("不能从{0}复制消息到{1}！", msg.GetType().Name, this.GetType().Name);

                // 遍历可序列化成员
                foreach (var item in ObjectInfo.GetMembers(this.GetType()))
                {
                    item[this] = item[msg];
                }
            }
            return this;
        }
        #endregion

        #region 设置
        [ThreadStatic]
        private static Boolean? _Debug;
        /// <summary>是否调试，输出序列化过程</summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug == null) _Debug = Config.GetConfig<Boolean>("NewLife.Message.Debug", false);
                return _Debug.Value;
            }
            set { _Debug = value; }
        }

        private static Boolean? _DumpStreamWhenError;
        /// <summary>出错时Dump数据流到文件中</summary>
        public static Boolean DumpStreamWhenError
        {
            get
            {
                if (_DumpStreamWhenError == null) _DumpStreamWhenError = Config.GetConfig<Boolean>("NewLife.Message.DumpStreamWhenError", false);
                return _DumpStreamWhenError.Value;
            }
            set { _DumpStreamWhenError = value; }
        }
        #endregion

        #region 重载
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Kind < MessageKind.UserDefine)
                return String.Format("Kind={0}", Kind);
            else
                return String.Format("Kind={0} Type={1}", Kind, this.GetType().Name);
        }
        #endregion
    }
}