using System;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>快速Emit反射</summary>
    public class EmitReflect : DefaultReflect
    {
        #region 反射获取
        /// <summary>根据名称获取类型</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        public override Type GetType(String typeName, Boolean isLoadAssembly)
        {
            return TypeX.GetType(typeName, isLoadAssembly);
        }

        /// <summary>获取方法</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes">参数类型数组</param>
        /// <returns></returns>
        public override MethodInfo GetMethod(Type type, String name, params Type[] paramTypes)
        {
            var method = base.GetMethod(type, name, paramTypes);
            if (method != null) return method;

            return TypeX.GetMethod(type, name, paramTypes);
        }
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        public override Object CreateInstance(Type type, params Object[] parameters) { return TypeX.CreateInstance(type, parameters); }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public override Object Invoke(Object target, MethodBase method, params Object[] parameters)
        {
            return MethodInfoX.Create(method).Invoke(target, parameters);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public override Object GetValue(Object target, PropertyInfo property)
        {
            return PropertyInfoX.Create(property).GetValue(target);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public override Object GetValue(Object target, FieldInfo field)
        {
            return FieldInfoX.Create(field).GetValue(target);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public override void SetValue(Object target, PropertyInfo property, Object value)
        {
            PropertyInfoX.Create(property).SetValue(target, value);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public override void SetValue(Object target, FieldInfo field, Object value)
        {
            FieldInfoX.Create(field).SetValue(target, value);
        }
        #endregion
    }
}