using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NewLife.Collections;

namespace XCode
{
    /// <summary>表达式基类</summary>
    public class Expression
    {
        #region 属性
        /// <summary>文本表达式</summary>
        public String Text { get; private set; }
        #endregion

        #region 构造
        /// <summary>实例化简单表达式</summary>
        public Expression() { }

        /// <summary>用一段文本实例化简单表达式</summary>
        /// <param name="value"></param>
        public Expression(String value) => Text = value;
        #endregion

        #region 方法
        /// <summary>用于匹配Or关键字的正则表达式</summary>
        internal protected static Regex _regOr = new Regex(@"\bOr\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>获取表达式的文本表示</summary>
        /// <param name="ps">参数字典</param>
        /// <returns></returns>
        public String GetString(IDictionary<String, Object> ps)
        {
            var sb = Pool.StringBuilder.Get();
            GetString(sb, ps);

            return sb.Put(true);
        }

        /// <summary>获取字符串</summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="ps">参数字典</param>
        public virtual void GetString(StringBuilder builder, IDictionary<String, Object> ps)
        {
            var txt = Text;
            if (txt.IsNullOrEmpty()) return;

            if (_regOr.IsMatch(txt))
                builder.AppendFormat("({0})", txt);
            else
                builder.Append(txt);
        }

        /// <summary>输出该表达式的字符串形式</summary>
        /// <returns></returns>
        public override String ToString() => GetString(null);

        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(Expression obj) => obj?.ToString();
        #endregion

        #region 重载运算符
        /// <summary>重载运算符实现And操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator &(Expression exp, Expression value) => And(exp, value);

        /// <summary>重载运算符实现And操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator &(Expression exp, String value) => And(exp, new Expression(value));

        static WhereExpression And(Expression exp, Expression value)
        {
            // 如果exp为空，主要考虑右边
            if (exp == null) return CreateWhere(value);

            // 左边构造条件表达式，自己是也好，新建立也好
            if (value == null) return CreateWhere(exp);

            return new WhereExpression(exp, Operator.And, value);
        }

        /// <summary>重载运算符实现Or操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator |(Expression exp, Expression value) => Or(exp, value);

        /// <summary>重载运算符实现Or操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator |(Expression exp, String value) => Or(exp, new Expression(value));

        static WhereExpression Or(Expression exp, Expression value)
        {
            //// 如果exp为空，主要考虑右边
            //if (exp == null) return value;

            //// 左边构造条件表达式，自己是也好，新建立也好
            ////var where = CreateWhere(exp);
            //if (value == null) return exp;

            // 如果右边为空，创建的表达式将会失败，直接返回左边
            //return where.Or(value);
            //return new WhereExpression(exp, OperatorExpression.Or, value);
            return new WhereExpression(exp, Operator.Or, value);
        }

        /// <summary>重载运算符实现+操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator +(Expression exp, Expression value)
        {
            //if (exp == null) return value;
            //if (value == null) return exp;

            return new WhereExpression(exp, Operator.Space, value);
        }

        internal static WhereExpression CreateWhere(Expression value)
        {
            if (value == null) return null;
            if (value is WhereExpression where) return where;

            return new WhereExpression(value, Operator.Space, null);
        }
        #endregion
    }
}