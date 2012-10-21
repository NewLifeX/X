using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Log;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace NewLife.Serialization
{
    /// <summary>读写器基类</summary>
    public abstract class ReaderWriterBase<TSettings> : /*NewLife.DisposeBase,*/ IReaderWriter where TSettings : ReaderWriterSetting, new()
    {
        #region 属性
        private String _Name;
        /// <summary>读写器名称</summary>
        public virtual String Name
        {
            get
            {
                if (String.IsNullOrEmpty(_Name))
                {
                    _Name = this.GetType().Name;
                    if (_Name.EndsWith("Reader", StringComparison.OrdinalIgnoreCase))
                        _Name = _Name.Substring(0, _Name.Length - "Reader".Length);
                    if (_Name.EndsWith("Writer", StringComparison.OrdinalIgnoreCase))
                        _Name = _Name.Substring(0, _Name.Length - "Writer".Length);
                }
                return _Name;
            }
        }

        private Stream _Stream;
        /// <summary>数据流。默认实例化一个MemoryStream，设置值时将重置Depth为1</summary>
        public virtual Stream Stream
        {
            get { return _Stream ?? (_Stream = new MemoryStream()); }
            set
            {
                if (_Stream != value)
                {
                    Depth = 1;

                    // 如果原来使用跟踪流，新的也使用跟踪流
                    if (Debug && _Stream is TraceStream && value != null && !(value is TraceStream))
                        _Stream = new TraceStream(value);
                    else
                        _Stream = value;
                }
            }
        }

        private TSettings _Settings;
        /// <summary>序列化设置</summary>
        public virtual TSettings Settings
        {
            get { return _Settings ?? (_Settings = new TSettings()); }
            set { _Settings = value; }
        }

        /// <summary>序列化设置</summary>
        ReaderWriterSetting IReaderWriter.Settings { get { return Settings; } set { Settings = (TSettings)value; } }

        private Int32 _Depth;
        /// <summary>层次深度</summary>
        public Int32 Depth
        {
            get
            {
                if (_Depth < 1) _Depth = 1;
                return _Depth;
            }
            set { _Depth = value; }
        }

        /// <summary>是否使用大小，如果使用，将在写入数组、集合和字符串前预先写入大小</summary>
        protected virtual Boolean UseSize { get { return true; } }

        private Object _CurrentObject;
        /// <summary>当前对象</summary>
        public Object CurrentObject { get { return _CurrentObject; } set { _CurrentObject = value; } }

        private IObjectMemberInfo _CurrentMember;
        /// <summary>当前成员</summary>
        public IObjectMemberInfo CurrentMember { get { return _CurrentMember; } set { _CurrentMember = value; } }

        private IDictionary _Items;
        /// <summary>用于存放使用者的上下文数据</summary>
        public IDictionary Items { get { return _Items ?? (_Items = new Hashtable()); } set { _Items = value; } }
        #endregion

        #region 方法
        /// <summary>重置</summary>
        public virtual void Reset() { Depth = 1; }

        /// <summary>是否精确类型</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected static Boolean IsExactType(Type type)
        {
            // 特殊处理Type
            if (type == null || type.IsInterface || type.IsAbstract && type != typeof(Type) || type == typeof(Object) || type == typeof(Exception))
                return false;
            else
                return true;
        }
        #endregion

        #region 备份还原环境
        private Stack<Dictionary<String, Object>> _stack;

        /// <summary>备份当前环境，用于临时切换数据流等</summary>
        /// <returns>本次备份项集合</returns>
        public virtual IDictionary<String, Object> Backup()
        {
            if (_stack == null) _stack = new Stack<Dictionary<String, Object>>();

            var dic = new Dictionary<String, Object>();
            dic["Stream"] = Stream;
            //dic["Depth"] = Depth;
            //dic["CurrentObject"] = CurrentObject;
            //dic["CurrentMember"] = CurrentMember;

            _stack.Push(dic);

            return dic;
        }

        /// <summary>恢复最近一次备份</summary>
        /// <returns>本次还原项集合</returns>
        public virtual IDictionary<String, Object> Restore()
        {
            if (_stack == null || _stack.Count <= 0) throw new Exception("没有任何备份项！");

            var dic = _stack.Pop();

            Object obj = null;
            if (dic.TryGetValue("Stream", out obj)) Stream = obj as Stream;
            //Stream = dic["Stream"] as Stream;
            //Depth = (Int32)dic["Depth"];
            //CurrentObject = dic["CurrentObject"];
            //CurrentMember = dic["CurrentMember"] as IObjectMemberInfo;

            return dic;
        }
        #endregion

        #region 获取成员
        /// <summary>获取需要序列化的成员</summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>需要序列化的成员</returns>
        public IObjectMemberInfo[] GetMembers(Type type, Object value)
        {
            if (type == null)
            {
                if (value == null) throw new ArgumentNullException("type");

                type = value.GetType();
            }

            var mis = OnGetMembers(type, value);

            if (OnGotMembers != null)
            {
                var e = new EventArgs<Type, Object, IObjectMemberInfo[]>(type, value, mis);
                OnGotMembers(this, e);
                mis = e.Arg3;
            }

            // 过滤掉被忽略的成员
            var members = Settings.IgnoreMembers;
            if (members.Count > 0)
            {
                mis = mis.Where(m => !members.Contains(m.Name)).ToArray();
            }

            return mis;
        }

        /// <summary>获取需要序列化的成员（属性或字段）</summary>
        /// <param name="type">指定类型</param>
        /// <param name="value">对象</param>
        /// <returns>需要序列化的成员</returns>
        protected virtual IObjectMemberInfo[] OnGetMembers(Type type, Object value)
        {
            if (type == null) throw new ArgumentNullException("type");

            return ObjectInfo.GetMembers(type, value, Settings.UseField, Settings.IsBaseFirst);
        }

        /// <summary>获取指定类型中需要序列化的成员时触发。使用者可以修改、排序要序列化的成员。</summary>
        public event EventHandler<EventArgs<Type, Object, IObjectMemberInfo[]>> OnGotMembers;
        #endregion

        #region 对象默认值
        /// <summary>判断一个对象的某个成员是否默认值</summary>
        /// <param name="value"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        internal static Boolean IsDefault(Object value, IObjectMemberInfo member)
        {
            if (value == null) return false;

            Object def = ObjectInfo.GetDefaultObject(value.GetType());
            return Object.Equals(member[value], member[def]);
        }
        #endregion

        #region 方法
        /// <summary>已重载。增加输出设置信息</summary>
        /// <returns></returns>
        public override string ToString()
        {
            //var msg = base.ToString();
            var msg = this.GetType().Name;

            var ms = Stream;
            if (ms != null && ms.CanRead) msg = String.Format("{0} Stream={1}", msg, ms.Length);

            return msg;
        }
        #endregion

        #region 跟踪日志
        private Boolean _Debug;
        /// <summary>是否调试</summary>
        public Boolean Debug { get { return _Debug; } set { _Debug = value; } }

        /// <summary>使用跟踪流。实际上是重新包装一次Stream，必须在设置Stream，使用之前</summary>
        public virtual void EnableTraceStream()
        {
            var stream = Stream;
            if (stream == null || stream is TraceStream) return;

            Stream = new TraceStream(stream) { Encoding = Settings.Encoding };
        }

        /// <summary>显示成员列表</summary>
        /// <param name="action"></param>
        /// <param name="members"></param>
        protected void ShowMembers(String action, IObjectMemberInfo[] members)
        {
            if (!Debug) return;

            var sb = new StringBuilder();
            foreach (var item in members)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append(item.Name);
            }
            WriteLog(action, sb.ToString());
        }

        /// <summary>调试输出</summary>
        /// <param name="action">操作</param>
        /// <param name="args">参数</param>
        public void WriteLog(String action, params Object[] args)
        {
            WriteLog(0, action, args);
        }

        static ConsoleColor[][] colors = new ConsoleColor[][] { 
            new ConsoleColor[] { ConsoleColor.Green,ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.Yellow },
            new ConsoleColor[] { ConsoleColor.Yellow, ConsoleColor.White, ConsoleColor.Magenta,ConsoleColor.Green }
        };

        static Boolean? _IsConsole;
        /// <summary>是否控制台</summary>
        static Boolean IsConsole { get { return (_IsConsole ?? (_IsConsole = Runtime.IsConsole)).Value; } }

        /// <summary>调试输出</summary>
        /// <param name="colorIndex">颜色方案</param>
        /// <param name="action">操作</param>
        /// <param name="args">参数</param>
        public void WriteLog(Int32 colorIndex, String action, params Object[] args)
        {
            if (!Debug) return;

            if (IsConsole)
            {
                ConsoleColor color = Console.ForegroundColor;

                // 缩进
                SetDebugIndent();

                // 红色动作
                Console.ForegroundColor = colors[colorIndex][0];
                Console.Write(action);

                if (args != null && args.Length > 0)
                {
                    // 白色参数
                    //Console.ForegroundColor = ConsoleColor.White;

                    for (int i = 0; i < args.Length; i++)
                    {
                        Console.ForegroundColor = colors[colorIndex][i % colors.Length + 1];
                        Console.Write(" ");
                        Console.Write(args[i]);
                    }
                }

                Console.ForegroundColor = color;
                Console.WriteLine();
            }
            else
            {
                // 缩进
                SetDebugIndent();

                // 动作
                XTrace.Write(action);

                if (args != null && args.Length > 0)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        XTrace.Write(" ");
                        XTrace.Write("" + args[i]);
                    }
                }

                XTrace.WriteLine("");
            }
        }

        /// <summary>设置调试缩进</summary>
        /// <param name="indent">缩进</param>
        public void SetDebugIndent(Int32 indent)
        {
            if (!Debug) return;

            if (IsConsole)
            {
                try
                {
                    Console.CursorLeft = indent * 4;
                }
                catch { }
            }
            else
            {
                var msg = new String(' ', indent * 4);
                XTrace.Write(msg);
            }
        }

        /// <summary>设置调试缩进</summary>
        public void SetDebugIndent()
        {
            if (!Debug) return;

            SetDebugIndent(Depth - 1);
        }
        #endregion
    }
}