using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using NewLife.Collections;

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

        FastHandler _Handler;
        /// <summary>
        /// 快速调用委托，延迟到首次使用才创建
        /// </summary>
        FastHandler Handler
        {
            get
            {
                if (_Handler == null)
                {
                    if (BaseType.IsValueType || BaseType.IsArray)
                        _Handler = GetConstructorInvoker(BaseType, null);
                    else
                    {
                        ListX<ConstructorInfo> list = Constructors;
                        if (list != null && list.Count > 0) _Handler = GetConstructorInvoker(BaseType, list[0]);
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
        /// <param name="type"></param>
        /// <returns></returns>
        public static TypeX Create(Type type)
        {
            return cache.GetItem(type, delegate(Type key)
            {
                return new TypeX(key);
            });
        }
        #endregion

        #region 创建动态方法
        delegate Object FastHandler(Object[] parameters);

        private static FastHandler GetConstructorInvoker(Type target, ConstructorInfo constructor)
        {
            // 定义一个没有名字的动态方法。
            // 关联到模块，并且跳过JIT可见性检查，可以访问所有类型的所有成员
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object[]) }, target.Module, true);
            {
                ILGenerator il = dynamicMethod.GetILGenerator();
                EmitHelper help = new EmitHelper(il);
                if (target.IsValueType)
                    help.NewValueType(target).BoxIfValueType(target).Ret();
                else if (target.IsArray)
                    help.PushParams(0, new Type[] { typeof(Int32) }).NewArray(target.GetElementType()).Ret();
                else
                    help.PushParams(0, constructor).NewObj(constructor).Ret();
            }
#if DEBUG
            //SaveIL(dynamicMethod, delegate(ILGenerator il)
            //     {
            //         EmitHelper help = new EmitHelper(il);
            //         if (target.IsValueType)
            //             help.NewValueType(target).BoxIfValueType(target).Ret();
            //         else if (target.IsArray)
            //             help.PushParams(0, new Type[] { typeof(Int32) }).NewArray(target.GetElementType()).Ret();
            //         else
            //             help.PushParams(0, constructor).NewObj(constructor).Ret();
            //     });
#endif

            return (FastHandler)dynamicMethod.CreateDelegate(typeof(FastHandler));
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
                // 准备参数类型数组，以匹配构造函数
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
                //Type[] paramTypes = Type.GetTypeArray(parameters);
                return GetHandler(paramTypes).Invoke(parameters);
            }
        }

        DictionaryCache<ConstructorInfo, FastHandler> _cache = new DictionaryCache<ConstructorInfo, FastHandler>();
        FastHandler GetHandler(Type[] paramTypes)
        {
            ConstructorInfo constructor = BaseType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, paramTypes, null);
            //if (constructor == null) throw new Exception("没有找到匹配的构造函数！");
            if (constructor == null) return null;

            return _cache.GetItem(constructor, delegate(ConstructorInfo key)
            {
                return GetConstructorInvoker(BaseType, key);
            });
        }

        /// <summary>
        /// 快速反射创建指定类型的实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Object CreateInstance(Type type, params Object[] parameters)
        {
            if (type == null) throw new ArgumentNullException("type");

            return Create(type).CreateInstance(parameters);
        }

        /// <summary>
        /// 取值，返回自己
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Object GetValue(Object obj)
        {
            return obj;
        }
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
                    _Members = new ListX<MemberInfo>(BaseType.GetMembers(DefaultBinding));
                    hasLoad.Add("Members");
                }
                return _Members == null ? null : _Members.Clone();
            }
            //set { _Members = value; }
        }

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

        #region 获取成员
        //public ListX<FieldInfo> GetFields(BindingFlags bindingAttr, Attribute[] includes, Attribute[] excludes)
        //{

        //}
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
            //if (!BaseType.IsClass || BaseType.IsAbstract || BaseType.IsGenericType) return false;
            // 允许值类型，仅排除接口
            if (BaseType.IsInterface || BaseType.IsAbstract || BaseType.IsGenericType) return false;

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

        /// <summary>
        /// 根据名称获取类型
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <returns></returns>
        public static Type GetType(String typeName)
        {
            return GetType(typeName, false);
        }

        static DictionaryCache<String, Type> typeCache = new DictionaryCache<String, Type>();
        /// <summary>
        /// 根据名称获取类型
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        public static Type GetType(String typeName, Boolean isLoadAssembly)
        {
            if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");

            //String key = (isLoadAssembly ? "1" : "0") + typeName;

            // isLoadAssembly不参与缓存的键，对于缓存来说，只要能找到类型就行，不必关心是否外部程序集
            return typeCache.GetItem<Boolean>(typeName, isLoadAssembly, GetTypeInternal);
        }

        private static Type GetTypeInternal(String typeName, Boolean isLoadAssembly)
        {
            if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");

            // 基本获取
            Type type = Type.GetType(typeName);
            if (type != null) return type;

            // 尝试本程序集
            Assembly[] asms = new[] { 
                Assembly.GetExecutingAssembly(),
                Assembly.GetCallingAssembly(), 
                Assembly.GetEntryAssembly() };

            foreach (Assembly asm in asms)
            {
                if (asm != null) type = asm.GetType(typeName);
                if (type != null) return type;
            }

            // 尝试所有程序集
            ListX<AssemblyX> list = AssemblyX.GetAssemblies();
            if (list != null && list.Count > 0)
            {
                foreach (AssemblyX asm in list)
                {
                    type = asm.Asm.GetType(typeName);
                    if (type != null) return type;
                }
            }

            // 尝试加载程序集
            AssemblyX.ReflectionOnlyLoad();
            list = AssemblyX.ReflectionOnlyGetAssemblies();
            if (list != null && list.Count > 0)
            {
                foreach (AssemblyX asm in list)
                {
                    type = asm.Asm.GetType(typeName);
                    if (type != null)
                    {
                        // 真实加载
                        Assembly asm2 = Assembly.LoadFile(asm.Asm.Location);
                        Type type2 = asm2.GetType(typeName);
                        if (type2 != null) type = type2;

                        return type;
                    }
                }
            }

            // 尝试系统的
            if (!typeName.Contains("."))
            {
                type = Type.GetType("System." + typeName);
                if (type != null) return type;
                type = Type.GetType("NewLife." + typeName);
                if (type != null) return type;
            }

            return null;
        }

        static DictionaryCache<String, Type> typeCache2 = new DictionaryCache<String, Type>();
        /// <summary>
        /// 从指定程序集查找指定名称的类型，如果查找不到，则进行忽略大小写的查找
        /// </summary>
        /// <param name="asm"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Type GetType(Assembly asm, String typeName)
        {
            if (asm == null) throw new ArgumentNullException("asm");
            if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");

            return typeCache2.GetItem<Assembly>(typeName, asm, GetTypeInternal);
        }

        private static Type GetTypeInternal(String typeName, Assembly asm)
        {
            Type type = asm.GetType(typeName);
            if (type == null) type = asm.GetType(typeName, false, true);
            if (type == null)
            {
                Type[] ts = asm.GetTypes();
                foreach (Type item in ts)
                {
                    if (item.Name == typeName)
                    {
                        type = item;
                        break;
                    }
                }
                if (type == null)
                {
                    foreach (Type item in ts)
                    {
                        if (String.Equals(item.Name, typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            type = item;
                            break;
                        }
                    }
                }
            }

            return type;
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

        #region 类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator Type(TypeX obj)
        {
            return obj != null ? obj.Type : null;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator TypeX(Type obj)
        {
            return obj != null ? Create(obj) : null;
        }
        #endregion
    }
}