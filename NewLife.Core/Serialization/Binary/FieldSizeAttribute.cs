using System;
using System.Collections;
using System.Reflection;
using System.Text;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>字段大小特性。</summary>
    /// <remarks>
    /// 可以通过Size指定字符串或数组的固有大小，为0表示自动计算；也可以通过指定参考字段ReferenceName，然后从其中获取大小。
    /// 支持_Header._Questions形式的多层次引用字段
    /// </remarks>
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

        /// <summary>指定参考字段ReferenceName，然后从其中获取大小</summary>
        /// <param name="referenceName"></param>
        /// <param name="size">在参考字段值基础上的增量，可以是正数负数</param>
        public FieldSizeAttribute(String referenceName, Int32 size) { ReferenceName = referenceName; Size = size; }

        #region 方法
        /// <summary>找到所引用的参考字段</summary>
        /// <param name="target"></param>
        /// <param name="member"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        MemberInfoX FindReference(Object target, MemberInfo member, out Object value)
        {
            value = null;

            if (member == null) return null;
            if (String.IsNullOrEmpty(ReferenceName)) return null;

            // 考虑ReferenceName可能是圆点分隔的多重结构
            MemberInfoX mx = null;
            Type type = member.DeclaringType;
            value = target;
            var ss = ReferenceName.Split(".");
            for (int i = 0; i < ss.Length; i++)
            {
                mx = MemberInfoX.Create(type, ss[i]);
                if (mx == null) return null;

                // 最后一个不需要计算
                if (i < ss.Length - 1)
                {
                    type = mx.Type;
                    value = mx.GetValue(value);
                }
            }

            //var mx = MemberInfoX.Create(member.DeclaringType, ReferenceName);
            //if (mx == null) return null;

            // 目标字段必须是整型
            TypeCode tc = Type.GetTypeCode(mx.Type);
            if (tc >= TypeCode.SByte && tc <= TypeCode.UInt64) return mx;

            return null;
        }

        /// <summary>设置目标对象的引用大小值</summary>
        /// <param name="target"></param>
        /// <param name="member"></param>
        /// <param name="encoding"></param>
        internal void SetReferenceSize(Object target, MemberInfo member, Encoding encoding)
        {
            Object v = null;
            var mx = FindReference(target, member, out v);
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
            mx.SetValue(v, size - Size);
        }

        /// <summary>获取目标对象的引用大小值</summary>
        /// <param name="target"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        internal Int32 GetReferenceSize(Object target, MemberInfo member)
        {
            Object v = null;
            var mx = FindReference(target, member, out v);
            if (mx == null) return -1;

            return Convert.ToInt32(mx.GetValue(v)) + Size;
        }
        #endregion
    }
}