using System;
using System.Dynamic;
using System.Globalization;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>包装程序集内部类的动态对象</summary>
    public class DynamicInternal : DynamicObject
    {
        private Object Real { get; set; }

        /// <summary>类型转换</summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override Boolean TryConvert(ConvertBinder binder, out Object result)
        {
            result = Real;

            return true;
        }

        /// <summary>成员取值</summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override Boolean TryGetMember(GetMemberBinder binder, out Object result)
        {
            var property = Real.GetType().GetProperty(binder.Name, BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                result = null;
            }
            else
            {
                result = property.GetValue(Real, null);
                result = Wrap(result);
            }
            return true;
        }

        /// <summary>调用成员</summary>
        /// <param name="binder"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override Boolean TryInvokeMember(InvokeMemberBinder binder, Object[] args, out Object result)
        {
            result = Real.GetType().InvokeMember(binder.Name, BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, Real, args, CultureInfo.InvariantCulture);

            return true;
        }

        /// <summary>包装</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Object Wrap(Object obj)
        {
            if (obj == null) return null;
            if (obj.GetType().IsPublic) return obj;

            return new DynamicInternal { Real = obj };
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return Real.ToString();
        }
    }
}