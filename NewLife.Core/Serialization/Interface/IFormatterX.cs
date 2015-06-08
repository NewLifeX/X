using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NewLife.Log;

namespace NewLife.Serialization
{
    /// <summary>序列化接口</summary>
    public interface IFormatterX
    {
        #region 属性
        /// <summary>数据流</summary>
        Stream Stream { get; set; }

        /// <summary>主对象</summary>
        Stack<Object> Hosts { get; }

        /// <summary>成员</summary>
        MemberInfo Member { get; set; }
        #endregion

        #region 方法
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Boolean Write(Object value, Type type = null);

        /// <summary>读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Object Read(Type type);

        /// <summary>读取指定类型对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Read<T>();

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean TryRead(Type type, ref Object value);
        #endregion

        #region 调试日志
        /// <summary>日志提供者</summary>
        ILog Log { get; set; }
        #endregion
    }

    /// <summary>序列化处理器接口</summary>
    /// <typeparam name="THost"></typeparam>
    public interface IHandler<THost> : IComparable<IHandler<THost>> where THost : IFormatterX
    {
        /// <summary>宿主读写器</summary>
        THost Host { get; set; }

        /// <summary>优先级</summary>
        Int32 Priority { get; set; }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Boolean Write(Object value, Type type);

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean TryRead(Type type, ref Object value);
    }

    /// <summary>序列化接口</summary>
    public abstract class FormatterBase //: IFormatterX
    {
        #region 属性
        private Int64 _StartPosition = 0;

        private Stream _Stream;
        /// <summary>数据流。默认实例化一个内存数据流</summary>
        public virtual Stream Stream { get { return _Stream ?? (_Stream = new MemoryStream()); } set { _Stream = value; _StartPosition = value == null ? 0 : value.Position; } }

        private Stack<Object> _Hosts = new Stack<Object>();
        /// <summary>主对象</summary>
        public Stack<Object> Hosts { get { return _Hosts; } }

        private MemberInfo _Member;
        /// <summary>成员</summary>
        public MemberInfo Member { get { return _Member; } set { _Member = value; } }

        private Encoding _Encoding = Encoding.Default;
        /// <summary>字符串编码，默认Default</summary>
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }
        #endregion

        #region 方法
        /// <summary>获取流里面的数据</summary>
        /// <returns></returns>
        public Byte[] GetBytes()
        {
            var ms = Stream;
            var pos = ms.Position;
            if (pos == 0 || pos == _StartPosition) return new Byte[0];

            if (ms is MemoryStream && pos == ms.Length && _StartPosition == 0)
                return (ms as MemoryStream).ToArray();

            ms.Position = _StartPosition;
            return ms.ReadBytes(pos - _StartPosition);
        }
        #endregion

        #region 跟踪日志
        private ILog _Log = XTrace.Debug ? XTrace.Log : Logger.Null;
        /// <summary>日志提供者</summary>
        public ILog Log { get { return _Log; } set { _Log = value; } }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WriteLog(String format, params Object[] args)
        {
            Log.Info(format, args);
        }
        #endregion
    }

    /// <summary>读写处理器基类</summary>
    public abstract class HandlerBase<THost, THandler> : IHandler<THost>
        where THost : IFormatterX
        where THandler : IHandler<THost>
    {
        private THost _Host;
        /// <summary>宿主读写器</summary>
        public THost Host { get { return _Host; } set { _Host = value; } }

        private Int32 _Priority;
        /// <summary>优先级</summary>
        public Int32 Priority { get { return _Priority; } set { _Priority = value; } }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public abstract Boolean Write(Object value, Type type);

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract Boolean TryRead(Type type, ref Object value);

        Int32 IComparable<IHandler<THost>>.CompareTo(IHandler<THost> other)
        {
            // 优先级较大在前面
            return this.Priority.CompareTo(other.Priority);
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Host.Log.Info(format, args);
        }
    }
}