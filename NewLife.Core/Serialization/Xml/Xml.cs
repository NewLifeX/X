using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Xml;

namespace NewLife.Serialization
{
    /// <summary>Xml序列化</summary>
    public class Xml : FormatterBase, IXml
    {
        #region 属性
        private Int32 _Depth;
        /// <summary>深度</summary>
        public Int32 Depth { get { return _Depth; } set { _Depth = value; } }

        private List<IXmlHandler> _Handlers;
        /// <summary>处理器列表</summary>
        public List<IXmlHandler> Handlers { get { return _Handlers ?? (_Handlers = new List<IXmlHandler>()); } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public Xml()
        {
            // 遍历所有处理器实现
            var list = new List<IXmlHandler>();
            foreach (var item in typeof(IXmlHandler).GetAllSubclasses(true))
            {
                var handler = item.CreateInstance() as IXmlHandler;
                handler.Host = this;
                list.Add(handler);
            }
            //list.Add(new XmlGeneral { Host = this });
            //list.Add(new XmlComposite { Host = this });
            // 根据优先级排序
            list.Sort();

            _Handlers = list;
        }
        #endregion

        #region 处理器
        /// <summary>添加处理器</summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Xml AddHandler(IXmlHandler handler)
        {
            if (handler != null)
            {
                handler.Host = this;
                _Handlers.Add(handler);
                // 根据优先级排序
                _Handlers.Sort();
            }

            return this;
        }

        /// <summary>添加处理器</summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="priority"></param>
        /// <returns></returns>
        public Xml AddHandler<THandler>(Int32 priority = 0) where THandler : IXmlHandler, new()
        {
            var handler = new THandler();
            handler.Host = this;
            if (priority != 0) handler.Priority = priority;

            return AddHandler(handler);
        }
        #endregion

        #region 写入
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public Boolean Write(Object value, String name = null, Type type = null)
        {
            if (type == null)
            {
                if (value == null) return true;

                type = value.GetType();
            }

            var writer = GetWriter();

            // 检查接口
            if (value is IXmlSerializable)
            {
                (value as IXmlSerializable).WriteXml(writer);
                return true;
            }

            if (String.IsNullOrEmpty(name))
            {
                // 优先采用类型上的XmlRoot特性
                name = type.GetCustomAttributeValue<XmlRootAttribute, String>(true);
                if (String.IsNullOrEmpty(name)) name = GetName(type);
            }

            // 一般类型为空是顶级调用
            if (Hosts.Count == 0) WriteLog("XmlWrite {0} {1}", name ?? type.Name, value);

            // 要先写入根
            Depth++;
            if (Depth == 1) writer.WriteStartDocument();
            writer.WriteStartElement(name);
            try
            {
                foreach (var item in Handlers)
                {
                    if (item.Write(value, type)) return true;
                }

                writer.WriteValue(value);

                return false;
            }
            finally
            {
                if (writer.WriteState != WriteState.Start)
                {
                    writer.WriteEndElement();
                    if (Depth == 1) writer.WriteEndDocument();
                }
                writer.Flush();
                Depth--;
            }
        }

        Boolean IFormatterX.Write(Object value, Type type) { return Write(value, null, type); }

        private XmlWriter _Writer;
        /// <summary>获取Xml写入器</summary>
        /// <returns></returns>
        public XmlWriter GetWriter()
        {
            if (_Writer == null)
            {
                var set = new XmlWriterSettings();
                set.Encoding = Encoding.TrimPreamble();
                set.Indent = true;

                _Writer = XmlWriter.Create(Stream, set);
            }

            return _Writer;
        }
        #endregion

        #region 读取
        /// <summary>读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object Read(Type type)
        {
            var value = type.CreateInstance();
            if (!TryRead(type, ref value)) throw new Exception("读取失败！");

            return value;
        }

        /// <summary>读取指定类型对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Read<T>()
        {
            return (T)(Object)Read(typeof(T));
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryRead(Type type, ref Object value)
        {
            var reader = GetReader();
            // 移动到第一个元素
            while (reader.NodeType != XmlNodeType.Element) { if (!reader.Read())return false; }
            
            if (Hosts.Count == 0) WriteLog("XmlRead {0} {1}", type.Name, value);

            foreach (var item in Handlers)
            {
                if (item.TryRead(type, ref value)) return true;
            }

            value = GetReader().ReadContentAs(type, null);
            return true;

            //return false;
        }

        private XmlReader _Reader;
        /// <summary>获取Xml读取器</summary>
        /// <returns></returns>
        public XmlReader GetReader()
        {
            if (_Reader == null) _Reader = XmlReader.Create(Stream);

            return _Reader;
        }
        #endregion

        #region 辅助方法
        static String GetName(Type type)
        {
            if (type.HasElementType) return "ArrayOf" + GetName(type.GetElementType());

            var name = type.GetName();
            name = name.Replace("<", "_");
            //name = name.Replace(">", "_");
            name = name.Replace(",", "_");
            name = name.Replace(">", "");
            return name;
        }

        #endregion
    }
}