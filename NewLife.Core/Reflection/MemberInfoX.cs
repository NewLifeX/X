using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

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
        /// <summary>
        /// 创建委托
        /// </summary>
        /// <typeparam name="TDelegate"></typeparam>
        /// <param name="method"></param>
        /// <returns></returns>
        public static TDelegate CreateDelegate<TDelegate>(MethodBase method)
        {
            if (!typeof(Delegate).IsAssignableFrom(typeof(TDelegate))) throw new ArgumentOutOfRangeException("TDelegate");

            Type type = typeof(TDelegate);
            ParameterInfo[] ps = type.GetConstructors()[0].GetParameters();
            List<Type> types = new List<Type>();
            foreach (ParameterInfo item in ps)
            {
                types.Add(item.ParameterType);
            }

            //定义一个没有名字的动态方法
            DynamicMethod dynamicMethod = new DynamicMethod(String.Empty, method.ReflectedType, types.ToArray(), method.DeclaringType.Module, true);
            ILGenerator il = dynamicMethod.GetILGenerator();
            GetMethodInvoker(il, method);

            return (TDelegate)(Object)dynamicMethod.CreateDelegate(type);
        }

        /// <summary>
        /// 获取方法的调用代码
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected static void GetMethodInvoker(ILGenerator il, MethodBase method)
        {
            EmitHelper help = new EmitHelper(il);

            if (!method.IsStatic) il.Emit(OpCodes.Ldarg_0);

            if (method is MethodInfo)
            {
                help.PushParams(method)
                    .Call(method as MethodInfo)
                    .Ret(method as MethodInfo);
            }
            else if (method is ConstructorBuilder)
            {

            }
        }
        #endregion

        #region 调用
        /// <summary>
        /// 执行方法
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual Object Invoke(Object obj, Object[] parameters) { throw new NotImplementedException(); }

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
        /// 创建实例
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual Object CreateInstance(Object[] parameters) { throw new NotImplementedException(); }
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
                    break;
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