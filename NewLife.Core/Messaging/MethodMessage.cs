using System;
using System.Collections;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Reflection;

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

        [NonSerialized]
        private Type _Type;
        /// <summary>实体类型。可以是接口或抽象类型（要求对象容器能识别）</summary>
        [XmlIgnore]
        public Type Type
        {
            get
            {
                if (_Type == null && !String.IsNullOrEmpty(_TypeName)) _Type = TypeX.GetType(_TypeName);
                return _Type;
            }
            set
            {
                _Type = value;
                if (value != null)
                    _TypeName = value.FullName;
                else
                    _TypeName = null;
            }
        }

        private String _TypeName;
        /// <summary>实体类型名。可以是接口或抽象类型（要求对象容器能识别）</summary>
        public String TypeName { get { return _TypeName; } set { _TypeName = value; } }

        private String _Name;
        /// <summary>方法名</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private Object[] _Parameters;
        /// <summary>参数数组</summary>
        public Object[] Parameters { get { return _Parameters; } set { _Parameters = value; } }

        [NonSerialized]
        private MethodInfo _Method;
        /// <summary>方法对象</summary>
        [XmlIgnore]
        public MethodInfo Method
        {
            get
            {
                if (_Method == null && Type != null && !String.IsNullOrEmpty(Name))
                {
                    // 静态、公共、非公共
                    //_Method = Type.GetMethod(Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    _Method = TypeX.GetMethod(Type, Name, TypeX.GetTypeArray(Parameters));
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

        /// <summary>根据包括类型和方法名的完整方法名，以及参数，创建方法消息</summary>
        /// <param name="fullMethodName">完整方法名</param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static MethodMessage Create(String fullMethodName, params Object[] ps)
        {
            if (String.IsNullOrEmpty(fullMethodName)) throw new ArgumentNullException("fullMethodName");
            Int32 p = fullMethodName.LastIndexOf(".");
            if (p <= 0 || p == fullMethodName.Length - 1) throw new ArgumentOutOfRangeException("fullMethodName", "完整方法名中未找到类型名！");

            var msg = new MethodMessage();
            msg.TypeName = fullMethodName.Substring(0, p);
            msg.Name = fullMethodName.Substring(p + 1);
            msg.Parameters = ps;
            return msg;
        }
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

                var rs = MethodInfoX.Create(method).Invoke(null, Parameters);
                if (rs == null)
                    return new NullMessage();
                else if (rs is IList)
                    // 采用实体集合消息返回列表型数据，避免序列化反序列化实体集合所带来的各种问题，到了客户端后，一律转化为List<T>
                    return new EntitiesMessage() { Values = rs as IList };
                else
                    return new EntityMessage() { Value = rs };
            }
            catch (Exception ex)
            {
                return new ExceptionMessage() { Value = ex };
            }
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1}.{2}", base.ToString(), TypeName, Name);
        }
        #endregion
    }
}