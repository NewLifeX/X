using System;
using System.Collections.Generic;
using XCode.Configuration;

namespace XCode
{
    /// <summary>字段表达式</summary>
    public class FieldExpression : Expression
    {
        #region 属性
        /// <summary>字段</summary>
        public FieldItem Field { get; set; }

        /// <summary>动作</summary>
        public String Action { get; set; }

        /// <summary>值</summary>
        public Object Value { get; set; }
        #endregion

        #region 构造
        /// <summary>构造字段表达式</summary>
        /// <param name="field"></param>
        /// <param name="action"></param>
        /// <param name="value"></param>
        public FieldExpression(FieldItem field, String action, Object value)
        {
            Field = field;
            Action = action;
            Value = value;
        }
        #endregion

        #region 输出
        /// <summary>已重载。输出字段表达式的字符串形式</summary>
        /// <param name="needBracket">外部是否需要括号。如果外部要求括号，而内部又有Or，则加上括号</param>
        /// <param name="ps">参数字典</param>
        /// <returns></returns>
        public override String GetString(Boolean needBracket, IDictionary<String, Object> ps)
        {
            if (Field == null) return null;

            var op = Field.Factory;

            var fi = Value as FieldItem;
            if (fi != null) return String.Format("{0}{1}{2}", Field.FormatedName, Action, op.FormatName(fi.ColumnName));

            if (ps == null) return String.Format("{0}{1}{2}", Field.FormatedName, Action, op.FormatValue(Field, Value));

            // 参数化处理
            var name = Field.Name;
            var i = 2;
            while (ps.ContainsKey(name)) name = Field.Name + i++;

            // 数值留给字典
            ps[name] = Value;

            return String.Format("{0}{1}{2}", Field.FormatedName, Action, op.Session.FormatParameterName(name));
        }
        #endregion
    }
}