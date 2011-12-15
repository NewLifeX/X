using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NewLife.Reflection;
using System.Collections;

namespace NewLife.Serialization
{
    /// <summary>字段大小特性。可以通过Size指定字符串或数组的固有大小，为0表示自动计算；也可以通过指定参考字段ReferenceName，然后从其中获取大小</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class FieldSizeAttribute : Attribute
    {
        private Int32 _Size;
        /// <summary>大小。0表示自动计算大小</summary>
        public Int32 Size { get { return _Size; } set { _Size = value; } }

        private String _ReferenceName;
        /// <summary>参考大小字段名</summary>
        public String ReferenceName { get { return _ReferenceName; } set { _ReferenceName = value; } }

        /// <summary>通过Size指定字符串或数组的固有大小，为0表示自动计算</summary>
        /// <param name="size"></param>
        public FieldSizeAttribute(Int32 size) { Size = size; }

        /// <summary>指定参考字段ReferenceName，然后从其中获取大小</summary>
        /// <param name="referenceName"></param>
        public FieldSizeAttribute(String referenceName) { ReferenceName = referenceName; }

        #region 方法
        /// <summary>找到所引用的参考字段</summary>
        /// <param name="member"></param>
        /// <returns></returns>
        MemberInfoX FindReference(MemberInfo member)
        {
            if (member == null) return null;
            if (Size > 0 || String.IsNullOrEmpty(ReferenceName)) return null;

            var mx = MemberInfoX.Create(member.DeclaringType, ReferenceName);
            if (mx == null) return null;

            // 目标字段必须是整型
            TypeCode tc = Type.GetTypeCode(mx.Type);
            if (tc >= TypeCode.Int16 && tc <= TypeCode.UInt64) return mx;

            return null;
        }

        /// <summary>设置目标对象的引用大小值</summary>
        /// <param name="target"></param>
        /// <param name="member"></param>
        /// <param name="encoding"></param>
        internal void SetReferenceSize(Object target, MemberInfo member, Encoding encoding)
        {
            var mx = FindReference(member);
            if (mx == null) return;

            // 获取当前成员（加了特性）的值
            var value = MemberInfoX.Create(member).GetValue(target);
            if (value == null) return;

            // 尝试计算大小
            Int32 size = 0;
            if (value is String)
            {
                if (encoding == null) encoding = Encoding.UTF8;

                size = encoding.GetByteCount("" + value);
            }
            else if (value.GetType().IsArray)
            {
                size = (value as Array).Length;
            }
            else if (value is IEnumerable)
            {
                foreach (var item in value as IEnumerable)
                {
                    size++;
                }
            }

            // 给参考字段赋值
            mx.SetValue(target, size);
        }

        /// <summary>获取目标对象的引用大小值</summary>
        /// <param name="target"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        internal Int32 GetReferenceSize(Object target, MemberInfo member)
        {
            var mx = FindReference(member);
            if (mx == null) return -1;

            return Convert.ToInt32(mx.GetValue(target));
        }
        #endregion
    }
}