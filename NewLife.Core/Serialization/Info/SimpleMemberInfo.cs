using System;

namespace NewLife.Serialization
{
    /// <summary>简单成员信息</summary>
    class SimpleMemberInfo : IObjectMemberInfo
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } private set { _Name = value; } }

        private Type _Type;
        /// <summary>类型</summary>
        public Type Type { get { return _Type; } private set { _Type = value; } }

        private Object _Value;

        /// <summary>对目标对象取值赋值</summary>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        public object this[object target] { get { return _Value; } set { _Value = value; } }

        ///// <summary>是否可读</summary>
        //public bool CanRead { get { return true; } }

        ///// <summary>是否可写</summary>
        //public bool CanWrite { get { return true; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="name">名称</param>
        /// <param name="type">类型</param>
        /// <param name="value"></param>
        public SimpleMemberInfo(String name, Type type, Object value)
        {
            Name = name;
            Type = type;
            _Value = value;
        }
        #endregion

        #region 已重载
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Type.GetTypeCode(Type) != TypeCode.Object)
                return String.Format("{0} {1} {2}", Name, Type.Name, _Value);
            else
                return String.Format("{0} {1}", Name, Type.Name);
        }
        #endregion
    }
}