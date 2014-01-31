using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NewLife.Serialization
{
    /// <summary>二进制序列化接口</summary>
    public interface IXml : IFormatterX
    {
        #region 属性
        /// <summary>编码</summary>
        Encoding Encoding { get; set; }

        /// <summary>处理器列表</summary>
        List<IXmlHandler> Handlers { get; }
        #endregion

        #region 方法
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Boolean Write(Object value, String name = null, Type type = null);

        /// <summary>获取Xml写入器</summary>
        /// <returns></returns>
        XmlWriter GetWriter();

        /// <summary>获取Xml读取器</summary>
        /// <returns></returns>
        XmlReader GetReader();
        #endregion
    }

    /// <summary>二进制读写处理器接口</summary>
    public interface IXmlHandler : IComparable<IXmlHandler>
    {
        /// <summary>宿主读写器</summary>
        IXml Host { get; set; }

        /// <summary>优先级</summary>
        Int32 Priority { get; set; }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Boolean Write(Object value, Type type);

        ///// <summary>读取一个对象</summary>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //Boolean Read(Object value);
    }

    /// <summary>Xml读写处理器基类</summary>
    public abstract class XmlHandlerBase : IXmlHandler
    {
        private IXml _Host;
        /// <summary>宿主读写器</summary>
        public IXml Host { get { return _Host; } set { _Host = value; } }

        private Int32 _Priority;
        /// <summary>优先级</summary>
        public Int32 Priority { get { return _Priority; } set { _Priority = value; } }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public abstract Boolean Write(Object value, Type type);

        Int32 IComparable<IXmlHandler>.CompareTo(IXmlHandler other)
        {
            // 优先级较大在前面
            return this.Priority.CompareTo(other.Priority);
        }
    }
}