using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        public Expression(String value) { Text = value; }
        #endregion

        #region 方法
        /// <summary>用于匹配Or关键字的正则表达式</summary>
        internal protected static Regex _regOr = new Regex(@"\bOr\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>获取表达式的文本表示</summary>
        /// <param name="needBracket">外部是否需要括号。如果外部要求括号，而内部又有Or，则加上括号</param>
        /// <param name="ps">参数字典</param>
        /// <returns></returns>
        public virtual String GetString(Boolean needBracket, IDictionary<String, Object> ps)
        {
            if (Text.IsNullOrWhiteSpace()) return Text;

            // 如果外部要求括号，而内部又有Or，则加上括号
            if (needBracket && _regOr.IsMatch(Text)) return "({0})".F(Text);

            return Text;
        }

        /// <summary>输出该表达式的字符串形式</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (this.GetType() == typeof(Expression)) return Text;

            return GetString(false, null);
        }

        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(Expression obj) { return obj?.GetString(false, null); }

        /// <summary>拉平表达式</summary>
        /// <returns></returns>
        public virtual Expression Flatten()
        {
            /*
             * 1，非条件表达式，直接返回
             * 2，条件表达式只有一个子项，返回子项拉平
             * 3，多个子项
             */

            return this;
        }
        #endregion

        #region 重载运算符
        /// <summary>重载运算符实现And操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator &(Expression exp, Expression value) { return And(exp, value); }

        /// <summary>重载运算符实现And操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator &(Expression exp, String value) { return And(exp, new Expression(value)); }

        static WhereExpression And(Expression exp, Expression value)
        {
            // 如果exp为空，主要考虑右边
            if (exp == null) return CreateWhere(value);

            // 左边构造条件表达式，自己是也好，新建立也好
            //var where = CreateWhere(exp);
            if (value == null) return CreateWhere(exp);

            // 如果右边为空，创建的表达式将会失败，直接返回左边
            //return where.And(value);
            return new WhereExpression(exp, OperatorExpression.And, value);
        }

        /// <summary>重载运算符实现Or操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator |(Expression exp, Expression value) { return Or(exp, value); }

        /// <summary>重载运算符实现Or操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator |(Expression exp, String value) { return Or(exp, new Expression(value)); }

        static WhereExpression Or(Expression exp, Expression value)
        {
            // 如果exp为空，主要考虑右边
            if (exp == null) return CreateWhere(value);

            // 左边构造条件表达式，自己是也好，新建立也好
            //var where = CreateWhere(exp);
            if (value == null) return CreateWhere(exp);

            // 如果右边为空，创建的表达式将会失败，直接返回左边
            //return where.Or(value);
            return new WhereExpression(exp, OperatorExpression.Or, value);
        }

        /// <summary>重载运算符实现+操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator +(Expression exp, Expression value)
        {
            if (exp == null) return value;
            if (value == null) return exp;

            return new WhereExpression(exp, OperatorExpression.Blank, value);
        }

        internal static WhereExpression CreateWhere(Expression value)
        {
            if (value == null) return new WhereExpression();
            if (value is WhereExpression) return (value as WhereExpression);

            return new WhereExpression(value);
        }
        #endregion
    }
}