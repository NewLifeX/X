using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using NewLife.Collections;
using System.Threading;

namespace NewLife.Reflection
{
    /// <summary>
    /// 快速访问成员
    /// </summary>
    public abstract class MemberInfoX
    {
        #region 属性
        private MemberInfo _Member;
        /// <summary>成员</summary>
        public MemberInfo Member
        {
            get { return _Member; }
            set { _Member = value; }
        }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 成员类型
        /// </summary>
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
                        return Member as Type;
                    default:
                        break;
                }
                return null;
            }
        }

        /// <summary>
        /// 目标类型
        /// </summary>
        public Type TargetType { get { return Member.DeclaringType; } }
        #endregion

        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="member"></param>
        protected MemberInfoX(MemberInfo member) { Member = member; }
        #endregion

        #region 生成代码
//        /// <summary>
//        /// 创建委托
//        /// </summary>
//        /// <typeparam name="TDelegate"></typeparam>
//        /// <param name="method"></param>
//        /// <param name="retType"></param>
//        /// <param name="paramTypes"></param>
//        /// <returns></returns>
//        internal protected static TDelegate CreateDelegate<TDelegate>(MethodBase method, Type retType, Type[] paramTypes)
//        {
//            if (!typeof(Delegate).IsAssignableFrom(typeof(TDelegate))) throw new ArgumentOutOfRangeException("TDelegate");

//            //定义一个没有名字的动态方法
//            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, retType, paramTypes, method.DeclaringType.Module, true);
//            ILGenerator il = dynamicMethod.GetILGenerator();

//            if (method is MethodInfo)
//                GetMethodInvoker(il, dynamicMethod, method as MethodInfo);
//            else if (method is ConstructorInfo)
//                GetConstructorInvoker(il, method as ConstructorInfo);

//#if DEBUG
//            SaveIL(dynamicMethod, delegate(ILGenerator il2)
//            {
//                if (method is MethodInfo)
//                    GetMethodInvoker(il2, dynamicMethod, method as MethodInfo);
//                else if (method is ConstructorInfo)
//                    GetConstructorInvoker(il2, method as ConstructorInfo);
//            });
//#endif

//            return (TDelegate)(Object)dynamicMethod.CreateDelegate(typeof(TDelegate));
//        }

//        /// <summary>
//        /// 创建不需要目标方法的委托
//        /// </summary>
//        /// <typeparam name="TDelegate"></typeparam>
//        /// <param name="targetType"></param>
//        /// <param name="retType"></param>
//        /// <param name="paramTypes"></param>
//        /// <returns></returns>
//        internal protected static TDelegate CreateDelegate<TDelegate>(Type targetType, Type retType, Type[] paramTypes)
//        {
//            if (!typeof(Delegate).IsAssignableFrom(typeof(TDelegate))) throw new ArgumentOutOfRangeException("TDelegate");

//            //定义一个没有名字的动态方法
//            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, retType, paramTypes, targetType.Module, true);
//            ILGenerator il = dynamicMethod.GetILGenerator();

//            if (targetType.IsValueType)
//                GetValueTypeInvoker(il, targetType);
//            else if (targetType.IsArray)
//                GetCreateArrayInvoker(il, targetType.GetElementType());
//            else
//                throw new NotSupportedException();

//#if DEBUG
//            SaveIL(dynamicMethod, delegate(ILGenerator il2)
//            {
//                if (targetType.IsValueType)
//                    GetValueTypeInvoker(il2, targetType);
//                else if (targetType.IsArray)
//                    GetCreateArrayInvoker(il2, targetType.GetElementType());
//            });
//#endif

//            return (TDelegate)(Object)dynamicMethod.CreateDelegate(typeof(TDelegate));
//        }

#if DEBUG
        //private static DynamicAssembly asm = null;
        //private static Timer timer = null;

        /// <summary>
        /// 保存IL
        /// </summary>
        /// <param name="method"></param>
        /// <param name="action"></param>
        internal protected static void SaveIL(MethodInfo method, Action<ILGenerator> action)
        {
            //if (asm == null) asm = new DynamicAssembly(String.Format("FastTest_{0:yyyyMMddHHmmssfff}", DateTime.Now));
            //if (asm == null)
            //{
            //    asm = new DynamicAssembly("FastTest");
            //    timer = new Timer(delegate { asm.Save(null); });
            //}
            //asm.AddGlobalMethod(method, action);
            ////asm.Save(null);
            //timer.Change(1000, Timeout.Infinite);

            DynamicAssembly asm = new DynamicAssembly(String.Format("FastTest_{0:yyyyMMddHHmmssfff}", DateTime.Now));
            asm.AddGlobalMethod(method, action);
            asm.Save(null);
        }
#endif

//        /// <summary>
//        /// 获取方法的调用代码
//        /// </summary>
//        /// <param name="il"></param>
//        /// <param name="method">要创建的方法</param>
//        /// <param name="target">目标方法</param>
//        private static void GetMethodInvoker(ILGenerator il, MethodInfo method, MethodInfo target)
//        {
//            // Object Method(Object, Object[] args)
//            // Method(Object, Object[] args)

//            EmitHelper help = new EmitHelper(il);

//            if (!target.IsStatic) il.Emit(OpCodes.Ldarg_0);

//            help.PushParams(target)
//                .Call(target);

//            if (method.ReturnType != null && method.ReturnType != typeof(void))
//            {
//                if (target.ReturnType != typeof(void))
//                    help.BoxIfValueType(target.ReturnType).Ret();
//                else
//                    help.Ldnull().Ret();
//            }
//            else
//            {
//                help.Ret();
//            }
//        }

//        private static void GetValueTypeInvoker(ILGenerator il, Type targetType)
//        {
//            // 声明目标类型的本地变量
//            il.DeclareLocal(targetType);
//            // 加载地址
//            il.Emit(OpCodes.Ldloca_S, 0);
//            // 创建对象
//            il.Emit(OpCodes.Initobj, targetType);
//            // 加载对象
//            il.Emit(OpCodes.Ldloc_0);
//            il.Emit(OpCodes.Ret);
//        }

//        /// <summary>
//        /// Object Method(Object[] args)
//        /// </summary>
//        /// <param name="il"></param>
//        /// <param name="elementType"></param>
//        private static void GetCreateArrayInvoker(ILGenerator il, Type elementType)
//        {
//            il.Emit(OpCodes.Ldarg_0);
//            il.Emit(OpCodes.Ldc_I4_0);
//            il.Emit(OpCodes.Ldelem_Ref);
//            il.Emit(OpCodes.Unbox_Any, typeof(Int32));
//            il.Emit(OpCodes.Newarr, elementType);
//            il.Emit(OpCodes.Ret);
//        }

//        /// <summary>
//        /// Object Method(Object[] args)
//        /// </summary>
//        /// <param name="il"></param>
//        /// <param name="method"></param>
//        internal protected static void GetConstructorInvoker(ILGenerator il, ConstructorInfo method)
//        {
//            EmitHelper help = new EmitHelper(il);

//            Type targetType = method.DeclaringType;
//            if (targetType.IsValueType)
//                GetValueTypeInvoker(il, targetType);
//            else if (targetType.IsArray)
//                GetCreateArrayInvoker(il, targetType.GetElementType());
//            else
//            {
//                // 其它类型
//                help.PushParams(0, method);

//                // 创建对象
//                il.Emit(OpCodes.Newobj, method);
//            }

//            il.Emit(OpCodes.Ret);
//        }
        #endregion

        #region 调用
        /// <summary>
        /// 执行方法
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual Object Invoke(Object obj, params Object[] parameters) { throw new NotImplementedException(); }

        /// <summary>
        /// 取值
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual Object GetValue(Object obj) { throw new NotImplementedException(); }

        /// <summary>
        /// 赋值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public virtual void SetValue(Object obj, Object value) { throw new NotImplementedException(); }

        /// <summary>
        /// 静态 取值
        /// </summary>
        /// <returns></returns>
        public Object GetValue() { return GetValue(null); }

        /// <summary>
        /// 静态 赋值
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(Object value) { SetValue(null, value); }

        /// <summary>
        /// 属性/字段 索引器
        /// </summary>
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

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual Object CreateInstance(params Object[] parameters) { throw new NotImplementedException(); }
        #endregion

        #region 类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator MemberInfo(MemberInfoX obj)
        {
            return obj != null ? obj.Member : null;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator MemberInfoX(MemberInfo obj)
        {
            if (obj == null) return null;

            switch (obj.MemberType)
            {
                case MemberTypes.All:
                    break;
                case MemberTypes.Constructor:
                    return ConstructorInfoX.Create(obj as ConstructorInfo);
                case MemberTypes.Custom:
                    break;
                case MemberTypes.Event:
                    break;
                case MemberTypes.Field:
                    return FieldInfoX.Create(obj as FieldInfo);
                case MemberTypes.Method:
                    return MethodInfoX.Create(obj as MethodInfo);
                case MemberTypes.NestedType:
                    break;
                case MemberTypes.Property:
                    return PropertyInfoX.Create(obj as PropertyInfo);
                case MemberTypes.TypeInfo:
                    return TypeX.Create(obj as Type);
                default:
                    break;
            }
            return null;
        }
        #endregion

        #region 重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (Type != null ? Type.Name : null) + "." + (Member != null ? Member.Name : null);
        }
        #endregion

        #region 辅助
        /// <summary>
        /// 是否有引用参数
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        protected static Boolean HasRefParam(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException("method");

            ParameterInfo[] ps = method.GetParameters();
            if (ps == null || ps.Length < 1) return false;

            foreach (ParameterInfo item in ps)
            {
                if (item.ParameterType.IsByRef) return true;
            }

            return false;
        }
        #endregion
    }
}