using System;
using XCode.Configuration;

namespace XCode
{
    /// <summary>格式化表达式。通过字段、格式化字符串和右值去构建表达式</summary>
    /// <remarks>右值可能为空，比如{0} Is Null</remarks>
    public class FormatExpression : Expression
    {
        #region 属性
        private FieldItem _Field;
        /// <summary>字段</summary>
        public FieldItem Field { get { return _Field; } set { _Field = value; } }

        private String _Format;
        /// <summary>格式化字符串</summary>
        public String Format { get { return _Format; } set { _Format = value; } }

        /// <summary>不可能有Or</summary>
        public override bool HasOr { get { return false; } }

        /// <summary>是否为空。构造输出时，空表达式没有输出，跟严格模式设置有很大关系</summary>
        public override bool IsEmpty
        {
            get
            {
                if (Field == null || Format.IsNullOrWhiteSpace()) return true;

                // 严格模式下，判断字段表达式是否有效
                if (Strict > 0 && Format.Contains("{1}"))
                {
                    // 所有空值无效
                    if (Text == null) return true;

                    // 如果数据为空，则返回
                    if (Strict > 1 && Text == String.Empty) return true;
                }

                return false;
            }
        }
        #endregion

        #region 构造
        /// <summary>构造格式化表达式</summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <param name="value"></param>
        public FormatExpression(FieldItem field, String format, String value)
        {
            Field = field;
            Format = format;
            Text = value;
        }
        #endregion

        #region 输出
        /// <summary>已重载。输出字段表达式的字符串形式</summary>
        /// <returns></returns>
        public override String GetString()
        {
            if (IsEmpty) return null;

            var op = Field.Factory;
            return String.Format(Format, Field.FormatedName, op.FormatValue(Field, Text));
        }
        #endregion
    }
}