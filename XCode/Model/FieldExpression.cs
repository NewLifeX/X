using System;
using XCode.Common;
using XCode.Configuration;

namespace XCode
{
    /// <summary>字段表达式</summary>
    public class FieldExpression : Expression
    {
        #region 属性
        private FieldItem _Field;
        /// <summary>字段</summary>
        public FieldItem Field { get { return _Field; } set { _Field = value; } }

        private String _Action;
        /// <summary>动作</summary>
        public String Action { get { return _Action; } set { _Action = value; } }

        private Object _Value;
        /// <summary>值</summary>
        public Object Value { get { return _Value; } set { _Value = value; } }

        /// <summary>不可能有Or</summary>
        public override bool HasOr { get { return false; } }

        /// <summary>是否为空。构造输出时，空表达式没有输出，跟严格模式设置有很大关系</summary>
        public override bool IsEmpty
        {
            get
            {
                if (Field == null) return true;

                // 严格模式下，判断字段表达式是否有效
                if (Strict > 0)
                {
                    // 所有空值无效
                    if (Value == null) return true;

                    // 如果数据为空，则返回
                    if (Strict > 1)
                    {
                        // 整型
                        if (Field.Type.IsIntType() && Value.ToInt() <= 0) return true;
                        // 字符串
                        if (Field.Type == typeof(String) && Value + "" == "") return true;
                        // 时间
                        if (Field.Type == typeof(DateTime) && Value.ToDateTime() <= DateTime.MinValue) return true;
                    }
                }

                return false;
            }
        }
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
        /// <returns></returns>
        public override String GetString()
        {
            if (IsEmpty) return null;

            var op = Field.Factory;

            var fi = Value as FieldItem;
            if (fi != null)
                return String.Format("{0}{1}{2}", Field.FormatedName, Action, op.FormatName(fi.ColumnName));
            else
                return String.Format("{0}{1}{2}", Field.FormatedName, Action, op.FormatValue(Field, Value));
        }
        #endregion
    }
}