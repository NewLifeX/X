using System;
using System.Collections.Generic;
using System.Text;
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

        //private Object _Value;
        ///// <summary>值</summary>
        //public Object Value { get { return _Value; } set { _Value = value; } }
        #endregion

        #region 构造
        //public FieldExpression(FieldItem field, String action, Object value) { }
        #endregion

        #region 输出
        /// <summary>已重载。输出字段表达式的字符串形式</summary>
        /// <returns></returns>
        public override String GetString()
        {
            if (Field == null) return null;

            // 严格模式
            if (Strict > 0)
            {
                if (Value == null) return null;

                // 如果数据为空，则返回
                if (Strict > 1)
                {
                    // 整型
                    if (Field.Type.IsIntType() && Value.ToInt() <= 0) return null;
                    // 字符串
                    if (Field.Type == typeof(String) && Value + "" == "") return null;
                    // 时间
                    if (Field.Type == typeof(DateTime) && Value.ToDateTime() <= DateTime.MinValue) return null;
                }
            }

            var op = Field.Factory;
            String sql = null;
            var name = op.FormatName(Field.ColumnName);
            if (!String.IsNullOrEmpty(Action) && Action.Contains("{0}"))
            {
                if (Action.Contains("%"))
                    sql = name + " Like " + op.FormatValue(Field, String.Format(Action, Value));
                else
                    sql = name + String.Format(Action, op.FormatValue(Field, Value));
            }
            else
            {
                // 右值本身就是FieldItem，属于对本表字段进行操作
                //if (value is FieldItem)
                //    sql = String.Format("{0}{1}{2}", name, Action, op.FormatName((value as FieldItem).ColumnName));
                // 减少一步类型转换
                var fi = Value as FieldItem;
                if (fi != null)
                    sql = String.Format("{0}{1}{2}", name, Action, op.FormatName(fi.ColumnName));
                else
                    sql = String.Format("{0}{1}{2}", name, Action, op.FormatValue(Field, Value));
            }
            return sql;
        }
        #endregion
    }
}