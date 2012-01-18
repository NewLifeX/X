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
    /// 
    /// 在服务端，通过调用消息的<see cref="Invoke"/>方法执行调用。
    /// 如有异常，返回异常消息；
    /// 如返回空，返回空消息；
    /// 否则返回实体消息<see cref="EntityMessage"/>
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

        #region 客户端
        //public static Object Invoke(MethodInfo method, params Object[] ps)
        //{
        //    return new MethodMessage() { Method = method, Parameters = ps }.Invoke();
        //}

        //public static Object Invoke(Type type, String name, params Object[] ps)
        //{
        //    return new MethodMessage() { Type = type, Name = name, Parameters = ps }.Invoke();
        //}

        //public Object Invoke()
        //{

        //}
        #endregion

        #region 服务端
        /// <summary>处理消息</summary>
        /// <returns></returns>
        public Message Invoke()
        {
            var method = Method;
            try
            {
                if (method == null) throw new ArgumentNullException("Method", String.Format("无法找到目标方法{0}.{1}！", Type, Name));

                var rs = method.Invoke(null, Parameters);
                if (rs == null)
                    return new NullMessage();
                else
                    return new EntityMessage() { Value = rs };
            }
            catch (Exception ex)
            {
                return new ExceptionMessage() { Value = ex };
            }
        }
        #endregion
    }
}