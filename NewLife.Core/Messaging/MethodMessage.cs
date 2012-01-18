using System;
using NewLife.Linq;
using System.Xml.Serialization;
using System.Reflection;

namespace NewLife.Messaging
{
    /// <summary>远程方法调用消息</summary>
    /// <remarks>
    /// 根据方法名<see cref="Name"/>在类型<see cref="Type"/>中找到方法，如果有多个签名，还得根据参数数组<see cref="Parameters"/>来选择。
    /// 仅支持无返回或单一返回，不支持out/ref等参数。
    /// </remarks>
    public class MethodMessage : Message
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Method; } }

        private Type _Type;
        /// <summary>实体类型。可以是接口或抽象类型（要求对象容器能识别）</summary>
        public Type Type { get { return _Type; } set { _Type = value; } }

        private String _Name;
        /// <summary>方法名</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private Object[] _Parameters;
        /// <summary>参数数组</summary>
        public Object[] Parameters { get { return _Parameters; } set { _Parameters = value; } }

        private MethodInfo _Method;
        /// <summary>方法对象</summary>
        public MethodInfo Method
        {
            get
            {
                if (_Method == null && Type != null && !String.IsNullOrEmpty(Name))
                {
                    // 静态、公共、非公共
                    _Method = Type.GetMethod(Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }
                return _Method;
            }
            set
            {
                _Method = value;
                if (value != null)
                {
                    Type = value.DeclaringType;
                    Name = value.Name;
                }
            }
        }
    }
}