using System;
using System.Collections;
using System.Reflection;
using System.Text;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>字段位大小特性。</summary>
    /// <remarks>
    /// 用于模仿C/C++的位域
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class BitSizeAttribute : Attribute
    {
        private Int32 _Size;
        /// <summary>大小</summary>
        public Int32 Size { get { return _Size; } set { _Size = value; } }

        /// <summary>通过Offset指定字段的偏移位置</summary>
        /// <param name="size"></param>
        public BitSizeAttribute(Int32 size) { Size = size; }

        #region 方法
        /// <summary>把当前字段所属部分附加到目标数字</summary>
        /// <param name="target">目标数字</param>
        /// <param name="value">当前字段的数值</param>
        /// <param name="offset">当前字段偏移</param>
        /// <returns></returns>
        public Int32 Set(Int32 target, Int32 value, Int32 offset)
        {
            // 过滤数字中的干扰数据
            var mask = GetMask();
            value &= mask;

            // 先清零所属位，避免其它地方干扰
            target &= ~(mask << offset);

            // 当前字段移位到它所属部分
            target |= (value << offset);

            return target;
        }

        /// <summary>从目标数字里面获取当前字段所属部分</summary>
        /// <param name="target">目标数字</param>
        /// <param name="offset">当前字段偏移</param>
        /// <returns></returns>
        public Int32 Get(Int32 target, Int32 offset)
        {
            var mask = GetMask();

            // 移位到它应该在的位置
            var value = target >> offset;
            value &= mask;

            return value;
        }

        /// <summary>根据大小计算掩码</summary>
        /// <returns></returns>
        Int32 GetMask()
        {
            var v = 0;
            for (int i = 0; i < Size; i++)
            {
                v <<= 1;
                v++;
            }
            return v;
        }
        #endregion
    }
}