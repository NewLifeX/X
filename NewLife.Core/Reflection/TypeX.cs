using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Collections;
using System.ComponentModel;

namespace NewLife.Reflection
{
    /// <summary>
    /// 类型辅助类
    /// </summary>
    public class TypeX : MemberInfoX
    {
        #region 属性
        private Type _BaseType;
        /// <summary>类型</summary>
        public Type BaseType
        {
            get { return _BaseType; }
            //set { _Type = value; }
        }

        FastCreateInstanceHandler _Handler;
        /// <summary>
        /// 快速调用委托，延迟到首次使用才创建
        /// </summary>
        FastCreateInstanceHandler Handler
        {
            get
            {
                if (_Handler == null)
                {
                    if (BaseType.IsValueType)
                        _Handler = CreateDelegate<FastCreateInstanceHandler>(BaseType, typeof(Object), new Type[] { typeof(Object[]) });
                    else if (BaseType.IsArray)
                        _Handler = CreateDelegate<FastCreateInstanceHandler>(BaseType, typeof(Object), new Type[] { typeof(Object[]) });
                    else
                    {
                        ListX<ConstructorInfo> list = Constructors;
                        if (list != null && list.Count > 0) _Handler = CreateDelegate<FastCreateInstanceHandler>(list[0], typeof(Object), new Type[] { typeof(Object[]) });
                    }
                }
                return _Handler;
            }
        }
        #endregion

        #region 构造
        private TypeX(Type type) : base(type) { _BaseType = type; }

        private static DictionaryCache<Type, TypeX> cache = new DictionaryCache<Type, TypeX>();
        /// <summary>
        /// 创建类型辅助对象
        /// </summary>
        /// <param name="asm"></param>
        /// <returns></returns>
        public static TypeX Create(Type asm)
        {
            return cache.GetItem(asm, delegate(Type key)
            {
                return new TypeX(key);
            });
        }
        #endregion

        #region 调用
        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override Object CreateInstance(params Object[] parameters)
        {
            if (BaseType.IsValueType || BaseType.IsArray)
                return Handler.Invoke(parameters);
            else
            {
                Type[] paramTypes = Type.EmptyTypes;
                if (parameters != null && parameters.Length > 0)
                {
                    List<Type> list = new List<Type>();
                    foreach (Object item in parameters)
                    {
                        if (item != null)
                            list.Add(item.GetType());
                        else
                            list.Add(typeof(Object));
                    }
                    paramTypes = list.ToArray();
                }
                return GetHandler(paramTypes).Invoke(parameters);
            }
        }

        DictionaryCache<ConstructorInfo, FastCreateInstanceHandler> _cache = new DictionaryCache<ConstructorInfo, FastCreateInstanceHandler>();
        FastCreateInstanceHandler GetHandler(Type[] paramTypes)
        {
            ConstructorInfo constructor = BaseType.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, paramTypes, null);
            if (constructor == null) throw new Exception("没有找到匹配的构造函数！");

            return _cache.GetItem(constructor, delegate(ConstructorInfo key)
            {
                return CreateDelegate<FastCreateInstanceHandler>(key, typeof(Object), new Type[] { typeof(Object[]) });
            });
        }

        /// <summary>
        /// 快速调用委托
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        delegate Object FastCreateInstanceHandler(Object[] parameters);
        #endregion

        #region 扩展属性
        private List<String> hasLoad = new List<String>();

        /// <summary>
        /// 是否系统类型
        /// </summary>
        /// <returns></returns>
        public Boolean IsSystemType
        {
            get
            {
                return BaseType.Assembly.FullName.EndsWith("PublicKeyToken=b77a5c561934e089");
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

                    //AssemblyDescriptionAttribute av = Attribute.GetCustomAttribute(Asm, typeof(AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute;
                    //if (av != null) _Description = av.Description;
                    //AssemblyDescriptionAttribute av = GetCustomAttribute<AssemblyDescriptionAttribute>();
                    //if (av != null) _Description = av.Description;
                    _Description = GetCustomAttributeValue<DescriptionAttribute, String>();
                }
                return _Description;
            }
            //set { _Description = value; }
        }
        #endregion

        #region 成员缓冲
        private ListX<MemberInfo> _Members;
        /// <summary>所有成员</summary>
        public ListX<MemberInfo> Members
        {
            get
            {
                if (_Members == null && !hasLoad.Contains("Members"))
                {
                    _Members = new ListX<MemberInfo>(BaseType.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
                    hasLoad.Add("Members");
                }
                return _Members == null ? null : _Members.Clone();
            }
            //set { _Members = value; }
        }

        //static ListX<MemberInfo> GetMembers(Type type)
        //{
        //    ListX<MemberInfo> list = new ListX<MemberInfo>();
        //    MemberInfo[] mis = type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        //    if (mis != null && mis.Length > 0) list.AddRange(mis);

        //    if (type.BaseType != null)
        //    {
        //        ListX<MemberInfo> list2 = GetMembers(type.BaseType);
        //        if (list2 != null) list.AddRange(list2);
        //    }

        //    if (list.Count < 1) return null;
        //    return list;
        //}

        ListX<T> GetMembers<T>(MemberTypes memberType) where T : MemberInfo
        {
            if (Members == null || Members.Count < 1) return null;

            ListX<T> list = new ListX<T>();
            foreach (MemberInfo item in Members)
            {
                if (item.MemberType == memberType) list.Add(item as T);
            }
            return list.Count > 0 ? list : null;
        }

        private ListX<FieldInfo> _Fields;
        /// <summary>字段集合</summary>
        public ListX<FieldInfo> Fields
        {
            get
            {
                if (_Fields == null && !hasLoad.Contains("Fields"))
                {
                    _Fields = GetMembers<FieldInfo>(MemberTypes.Field);
                    hasLoad.Add("Fields");
                }
                return _Fields == null ? null : _Fields.Clone();
            }
            //set { _Fields = value; }
        }

        private ListX<PropertyInfo> _Properties;
        /// <summary>属性集合</summary>
        public ListX<PropertyInfo> Properties
        {
            get
            {
                if (_Properties == null && !hasLoad.Contains("Properties"))
                {
                    _Properties = GetMembers<PropertyInfo>(MemberTypes.Property);
                    hasLoad.Add("Properties");
                }
                return _Properties == null ? null : _Properties.Clone();
            }
            //set { _Properties = value; }
        }

        private ListX<MethodInfo> _Methods;
        /// <summary>方法集合</summary>
        public ListX<MethodInfo> Methods
        {
            get
            {
                if (_Methods == null && !hasLoad.Contains("Methods"))
                {
                    _Methods = GetMembers<MethodInfo>(MemberTypes.Method);
                    hasLoad.Add("Methods");
                }
                return _Methods == null ? null : _Methods.Clone();
            }
            //set { _Methods = value; }
        }

        private ListX<ConstructorInfo> _Constructors;
        /// <summary>构造函数集合</summary>
        public ListX<ConstructorInfo> Constructors
        {
            get
            {
                if (_Constructors == null && !hasLoad.Contains("Constructors"))
                {
                    _Constructors = GetMembers<ConstructorInfo>(MemberTypes.Constructor);
                    hasLoad.Add("Constructors");
                }
                return _Constructors == null ? null : _Constructors.Clone();
            }
            //set { _Constructors = value; }
        }

        private ListX<EventInfo> _Events;
        /// <summary>事件集合</summary>
        public ListX<EventInfo> Events
        {
            get
            {
                if (_Events == null && !hasLoad.Contains("Events"))
                {
                    _Events = GetMembers<EventInfo>(MemberTypes.Event);
                    hasLoad.Add("Events");
                }
                return _Events == null ? null : _Events.Clone();
            }
            //set { _Events = value; }
        }

        private ListX<Type> _Interfaces;
        /// <summary>接口集合</summary>
        public ListX<Type> Interfaces
        {
            get
            {
                if (_Interfaces == null && !hasLoad.Contains("Interfaces"))
                {
                    _Interfaces = new ListX<System.Type>(BaseType.GetInterfaces());
                    hasLoad.Add("Interfaces");
                }
                return _Interfaces == null ? null : _Interfaces.Clone();
            }
            //set { _Interfaces = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 是否指定类型的插件
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Boolean IsPlugin(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            //为空、不是类、抽象类、泛型类 都不是实体类
            if (!BaseType.IsClass || BaseType.IsAbstract || BaseType.IsGenericType) return false;

            if (type.IsInterface)
            {
                //if (!Interfaces.Contains(type)) return false;
                if (Interfaces == null || Interfaces.Count < 1) return false;

                Boolean b = false;
                foreach (Type item in Interfaces)
                {
                    if (item == type) { b = true; break; }

                    if (item.FullName == type.FullName && item.AssemblyQualifiedName == type.AssemblyQualifiedName) { b = true; break; }
                }
                if (!b) return false;
            }
            else
            {
                if (!type.IsAssignableFrom(BaseType)) return false;
            }

            return true;
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            String des = Description;
            if (!String.IsNullOrEmpty(des))
                return des;
            else
                return BaseType.FullName;
        }

        /// <summary>
        /// 判断两个类型是否相同，避免引用加载和执行上下文加载的相同类型显示不同
        /// </summary>
        /// <param name="type1"></param>
        /// <param name="type2"></param>
        /// <returns></returns>
        public static Boolean Equal(Type type1, Type type2)
        {
            if (type1 == type2) return true;

            return type1.FullName == type2.FullName && type1.AssemblyQualifiedName == type2.AssemblyQualifiedName;
        }

        /// <summary>
        /// 获取自定义属性的值。可用于ReflectionOnly加载的程序集
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult GetCustomAttributeValue<TAttribute, TResult>()
        {
            return AttributeX.GetCustomAttributeValue<TAttribute, TResult>(BaseType, true);
        }
        #endregion
    }
}