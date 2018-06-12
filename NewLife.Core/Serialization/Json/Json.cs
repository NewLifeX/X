using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>Json序列化</summary>
    public class Json : FormatterBase, IJson
    {
        #region 属性
        /// <summary>是否缩进</summary>
        public Boolean Indented { get; set; }

        /// <summary>处理器列表</summary>
        public IList<IJsonHandler> Handlers { get; private set; }

        //private StringBuilder _builder;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public Json()
        {
            UseProperty = true;

            //_builder = new StringBuilder();

            // 遍历所有处理器实现
            var list = new List<IJsonHandler>
            {
                new JsonGeneral { Host = this },
                new JsonComposite { Host = this },
                new JsonArray { Host = this }
            };
            //list.Add(new JsonDictionary { Host = this });
            // 根据优先级排序
            list.Sort();

            Handlers = list;
        }
        #endregion

        #region 处理器
        /// <summary>添加处理器</summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Json AddHandler(IJsonHandler handler)
        {
            if (handler != null)
            {
                handler.Host = this;
                Handlers.Add(handler);
                // 根据优先级排序
                (Handlers as List<IJsonHandler>).Sort();
            }

            return this;
        }

        /// <summary>添加处理器</summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="priority"></param>
        /// <returns></returns>
        public Json AddHandler<THandler>(Int32 priority = 0) where THandler : IJsonHandler, new()
        {
            var handler = new THandler
            {
                Host = this
            };
            if (priority != 0) handler.Priority = priority;

            return AddHandler(handler);
        }

        /// <summary>获取处理器</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetHandler<T>() where T : class, IJsonHandler
        {
            foreach (var item in Handlers)
            {
                if (item is T) return item as T;
            }

            return default(T);
        }
        #endregion

        #region 写入
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual Boolean Write(Object value, Type type = null)
        {
            if (type == null)
            {
                if (value == null) return true;

                type = value.GetType();

                // 一般类型为空是顶级调用
                if (Hosts.Count == 0) WriteLog("JsonWrite {0} {1}", type.Name, value);
            }

            //foreach (var item in Handlers)
            //{
            //    if (item.Write(value, type)) return true;
            //}

            var sb = Pool.StringBuilder.Get();
            Write(sb, value);

            Stream.Write(sb.Put(true).GetBytes());

            return false;
        }

        /// <summary>写入字符串</summary>
        /// <param name="value"></param>
        public virtual void Write(String value)
        {
            //_builder.Append(value);
        }

        /// <summary>写入</summary>
        /// <param name="sb"></param>
        /// <param name="value"></param>
        public virtual void Write(StringBuilder sb, Object value)
        {
            if (value == null) return;
            var type = value.GetType();

            foreach (var item in Handlers)
            {
                if (item.Write(value, type)) return;
            }
        }
        #endregion

        #region 读取
        /// <summary>读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual Object Read(Type type)
        {
            var value = type.CreateInstance();
            if (!TryRead(type, ref value)) throw new Exception("读取失败！");

            return value;
        }

        /// <summary>读取指定类型对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [DebuggerHidden]
        public T Read<T>() => (T)(Object)Read(typeof(T));

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual Boolean TryRead(Type type, ref Object value)
        {
            if (Hosts.Count == 0) WriteLog("JsonRead {0} {1}", type.Name, value);

            foreach (var item in Handlers)
            {
                if (item.TryRead(type, ref value)) return true;
            }
            return false;
        }

        /// <summary>读取</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Boolean Read(String value) => true;

        /// <summary>读取字节</summary>
        /// <returns></returns>
        public virtual Byte ReadByte()
        {
            var b = Stream.ReadByte();
            if (b < 0) throw new Exception("数据流超出范围！");
            return (Byte)b;
        }
        #endregion

        #region 辅助函数

        #endregion
    }
}