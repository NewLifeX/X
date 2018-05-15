using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Reflection;
using XCode.Configuration;

namespace XCode
{
    /// <summary>格式化表达式。通过字段、格式化字符串和右值去构建表达式</summary>
    /// <remarks>右值可能为空，比如{0} Is Null</remarks>
    public class FormatExpression : Expression
    {
        #region 属性
        /// <summary>字段</summary>
        public FieldItem Field { get; set; }

        /// <summary>格式化字符串</summary>
        public String Format { get; set; }

        /// <summary>操作数</summary>
        public Object Value { get; set; }
        #endregion

        #region 构造
        /// <summary>构造格式化表达式</summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <param name="value"></param>
        public FormatExpression(FieldItem field, String format, Object value)
        {
            Field = field;
            Format = format;
            Value = value;
        }
        #endregion

        #region 输出
        /// <summary>已重载。输出字段表达式的字符串形式</summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="ps">参数字典</param>
        /// <returns></returns>
        public override void GetString(StringBuilder builder, IDictionary<String, Object> ps)
        {
            var fi = Field;
            if (fi == null || Format.IsNullOrWhiteSpace()) return;

            if (ps == null)
            {
                builder.AppendFormat(Format, fi.FormatedName, Value);
                return;
            }

            // 参数化处理
            var name = fi.Name;
            var i = 2;
            while (ps.ContainsKey(name)) name = fi.Name + i++;

            // 数值留给字典
            var type = fi.Type;
            if (type.IsEnum) type = typeof(Int32);
            ps[name] = Value.ChangeType(type);

            var op = fi.Factory;
            builder.AppendFormat(Format, fi.FormatedName, op.Session.FormatParameterName(name));
        }
        #endregion
    }
}