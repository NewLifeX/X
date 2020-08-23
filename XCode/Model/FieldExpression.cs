using System;
using System.Collections.Generic;
using System.Text;
using NewLife;
using XCode.Configuration;
using XCode.DataAccessLayer;

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

        /// <summary>是否为空</summary>
        public override Boolean IsEmpty => Field == null;
        #endregion

        #region 构造
        /// <summary>构造字段表达式</summary>
        /// <param name="field"></param>
        public FieldExpression(FieldItem field) => Field = field;

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
        /// <param name="db">数据库</param>
        /// <param name="builder">字符串构建器</param>
        /// <param name="ps">参数字典</param>
        /// <returns></returns>
        public override void GetString(IDatabase db, StringBuilder builder, IDictionary<String, Object> ps)
        {
            if (Field == null) return;

            // 部分场景外部未能传入数据库，此时内部尽力获取
            if (db == null) db = Field?.Factory.Session.Dal.Db;

            var columnName = db.FormatName(Field.Field);
            if (Action.IsNullOrEmpty())
            {
                builder.Append(columnName);
                return;
            }

            // 右值是字段
            if (Value is FieldItem fi)
            {
                builder.AppendFormat("{0}{1}{2}", columnName, Action, db.FormatName(fi.Field));
                return;
            }

            if (ps == null)
            {
                builder.AppendFormat("{0}{1}{2}", columnName, Action, db.FormatValue(Field.Field, Value));
                return;
            }

            // 参数化处理
            var name = Field.Name;
            var i = 2;
            while (ps.ContainsKey(name)) name = Field.Name + i++;

            // 数值留给字典
            ps[name] = Value;

            builder.AppendFormat("{0}{1}{2}", columnName, Action, db.FormatParameterName(name));
        }
        #endregion
    }
}