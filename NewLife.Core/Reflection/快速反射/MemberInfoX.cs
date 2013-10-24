using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>快速访问成员</summary>
    public abstract class MemberInfoX
    {
        #region 属性
        private MemberInfo _Member;
        /// <summary>成员</summary>
        public MemberInfo Member { get { return _Member; } set { _Member = value; } }

        /// <summary>名称</summary>
        public virtual String Name { get { return Member.Name; } }

        /// <summary>默认查找标志</summary>
        public const BindingFlags DefaultBinding = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        #endregion

        #region 扩展属性
        /// <summary>成员类型</summary>
        public virtual Type Type
        {
            get
            {
                if (Member == null) return null;
                switch (Member.MemberType)
                {
                    case MemberTypes.Constructor:
                        return (Member as ConstructorInfo).DeclaringType;
                    case MemberTypes.Field:
                        return (Member as FieldInfo).FieldType;
                    case MemberTypes.Method:
                        return (Member as MethodInfo).ReturnType;
                    case MemberTypes.Property:
                        return (Member as PropertyInfo).PropertyType;
                    case MemberTypes.TypeInfo:
                    case MemberTypes.NestedType:
                        return Member as Type;
                    default:
                        break;
                }
                return null;
            }
        }

        /// <summary>目标类型</summary>
        public Type TargetType { get { return Member.DeclaringType; } }

        /// <summary>是否类型</summary>
        public Boolean IsType
        {
            get
            {
                return Member != null && (Member.MemberType == MemberTypes.TypeInfo || Member.MemberType == MemberTypes.NestedType);
            }
        }

        private String _DocName;
        /// <summary>文档名</summary>
        public String DocName
        {
            get
            {
                if (_DocName == null)
                {
                    _DocName = OrcasNamer.GetName(Member);

                    if (_DocName == null) _DocName = "";
                }
                return _DocName;
            }
            //set { _DocName = value; }
        }

        private List<String> hasLoad = new List<String>();

        private String _DisplayName;
        /// <summary>显示名</summary>
        public String DisplayName
        {
            get
            {
                if (String.IsNullOrEmpty(_DisplayName) && !hasLoad.Contains("DisplayName"))
                {
                    hasLoad.Add("DisplayName");

                    _DisplayName = GetCustomAttributeValue<DisplayNameAttribute, String>();
                    if (_DisplayName.IsNullOrWhiteSpace()) _DisplayName = Name;
                }
                return _DisplayName;
            }
        }

        private String _Description;
        /// <summary>说明</summary>
        public String Description
        {
            get
            {
                if (String.IsNullOrEmpty(_Description) && !hasLoad.Contains("Description"))
                {
                    hasLoad.Add("Description");

                    _Description = GetCustomAttributeValue<DescriptionAttribute, String>();
                }
                return _Description;
            }
        }
        #endregion

        #region 构造
        /// <summary>初始化快速访问成员</summary>
        /// <param name="member"></param>
        protected MemberInfoX(MemberInfo member) { Member = member; }

        /// <summary>创建快速访问成员</summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static MemberInfoX Create(MemberInfo member)
        {
            if (member == null) return null;

            switch (member.MemberType)
            {
                case MemberTypes.All:
                    break;
                case MemberTypes.Constructor:
                    return ConstructorInfoX.Create(member as ConstructorInfo);
                case MemberTypes.Custom:
                    break;
                case MemberTypes.Event:
                    return EventInfoX.Create(member as EventInfo);
                case MemberTypes.Field:
                    return FieldInfoX.Create(member as FieldInfo);
                case MemberTypes.Method:
                    return MethodInfoX.Create(member as MethodInfo);
                case MemberTypes.Property:
                    return PropertyInfoX.Create(member as PropertyInfo);
                case MemberTypes.TypeInfo:
                case MemberTypes.NestedType:
                    return TypeX.Create(member as Type);
                default:
                    break;
            }
            return null;
        }

        /// <summary>通过指定类型和成员名称，创建快速访问成员。按照属性、字段、构造、方法、事件的顺序</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static MemberInfoX Create(Type type, String name)
        {
            if (type == null || String.IsNullOrEmpty(name)) return null;

            var mis = type.GetMember(name, DefaultBinding | BindingFlags.IgnoreCase);
            if (mis == null || mis.Length < 1)
            {
                //return null;
                // 基类的字段是无法通过这种方法得到的
                return FieldInfoX.Create(type, name);
            }
            if (mis.Length == 1) return Create(mis[0]);

            var ts = new MemberTypes[] { MemberTypes.Property, MemberTypes.Field, MemberTypes.Constructor, MemberTypes.Method, MemberTypes.Event };
            foreach (var item in ts)
            {
                foreach (var mi in mis)
                {
                    if (mi.MemberType == item) return Create(mi);
                }
            }

            return Create(mis[0]);
        }
        #endregion

        #region 调用
        /// <summary>执行方法</summary>
        /// <param name="obj"></param>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        public virtual Object Invoke(Object obj, params Object[] parameters) { throw new NotImplementedException(); }

        /// <summary>取值</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual Object GetValue(Object obj) { throw new NotImplementedException(); }

        /// <summary>赋值</summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public virtual void SetValue(Object obj, Object value) { throw new NotImplementedException(); }

        /// <summary>静态 取值</summary>
        /// <returns></returns>
        public Object GetValue() { return GetValue(null); }

        /// <summary>静态 赋值</summary>
        /// <param name="value"></param>
        public void SetValue(Object value) { SetValue(null, value); }

        /// <summary>属性/字段 索引器</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual Object this[Object obj]
        {
            get { return GetValue(obj); }
            set { SetValue(obj, value); }
        }

        /// <summary>静态 属性/字段 值</summary>
        public Object Value
        {
            get { return GetValue(null); }
            set { SetValue(null, value); }
        }

        /// <summary>创建实例</summary>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        public virtual Object CreateInstance(params Object[] parameters) { throw new NotImplementedException(); }
        #endregion

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator MemberInfo(MemberInfoX obj)
        {
            return obj != null ? obj.Member : null;
        }

        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator MemberInfoX(MemberInfo obj)
        {
            return Create(obj);
        }
        #endregion

        #region 重载
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (Type != null ? Type.Name : null) + "." + (Member != null ? Member.Name : null);
        }
        #endregion

        #region 辅助
        /// <summary>是否有引用参数</summary>
        /// <param name="method"></param>
        /// <returns></returns>
        protected static Boolean HasRefParam(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException("method");

            var ps = method.GetParameters();
            if (ps == null || ps.Length < 1) return false;

            foreach (var item in ps)
            {
                if (item.ParameterType.IsByRef) return true;
            }

            return false;
        }

        /// <summary>获取自定义属性的值。可用于ReflectionOnly加载的程序集</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult GetCustomAttributeValue<TAttribute, TResult>()
        {
            return Member.GetCustomAttributeValue<TAttribute, TResult>(true);
        }
        #endregion
    }
}