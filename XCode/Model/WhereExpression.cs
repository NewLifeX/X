using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace XCode
{
    /// <summary>条件表达式</summary>
    public class WhereExpression : Expression
    {
        #region 属性
        //private StringBuilder _Builder = new StringBuilder();
        ///// <summary>内置字符串</summary>
        //public StringBuilder Builder { get { return _Builder; } set { _Builder = value; } }

        //private List<Boolean> _Actions;
        ///// <summary>与或的动作集合，true表示与</summary>
        //public List<Boolean> Actions { get { return _Actions; } set { _Actions = value; } }

        private List<Expression> _Expressions = new List<Expression>();
        /// <summary>表达式集合</summary>
        public List<Expression> Expressions { get { return _Expressions; } set { _Expressions = value; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public WhereExpression() { }

        ///// <summary>实例化</summary>
        ///// <param name="exp"></param>
        //public WhereExpression(String exp)
        //{
        //    Builder.Append(exp);
        //}

        public WhereExpression(Expression exp) { And(exp); }
        #endregion

        #region 方法
        /// <summary>And操作</summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public WhereExpression And(Expression exp)
        {
            if (exp != null)
            {
                Expressions.Add(new Expression("And"));
                Expressions.Add(exp);
            }

            return this;
        }

        /// <summary>Or操作</summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public WhereExpression Or(Expression exp)
        {
            if (exp != null)
            {
                Expressions.Add(new Expression("Or"));
                Expressions.Add(exp);
            }

            return this;
        }

        /// <summary>输出条件表达式的字符串表示，遍历表达式集合并拼接起来</summary>
        /// <returns></returns>
        public override String GetString()
        {
            if (Expressions.Count == 0) return null;

            var sb = new StringBuilder();
            // 注意：第一个位置是And/Or，无效
            for (int i = 0; i < Expressions.Count - 1; i += 2)
            {
                var exp = Expressions[i + 1];
                exp.Strict = Strict;
                var str = exp.GetString();
                if (!str.IsNullOrWhiteSpace())
                {
                    // 加上前一个And/Or
                    if (sb.Length > 0) sb.AppendFormat(" {0} ", Expressions[i].GetString());
                    sb.Append(str);
                }
            }
            return sb.Length == 0 ? null : sb.ToString();
        }

        //void Append(String action, String content)
        //{
        //    if (Builder.Length > 0 && Builder[Builder.Length - 1] != '(') Builder.AppendFormat(" {0} ", action.Trim());
        //    Builder.Append(content);
        //}

        //static Regex _regOr = new Regex("\bOr\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        ///// <summary>And操作</summary>
        ///// <param name="exp"></param>
        ///// <returns></returns>
        //public WhereExpression And(String exp)
        //{
        //    if (String.IsNullOrEmpty(exp)) return this;

        //    //if (Builder.Length > 0 && Builder[Builder.Length - 1] != '(') Builder.Append(" And ");
        //    //Builder.Append(exp);
        //    if (!String.IsNullOrEmpty(exp))
        //    {
        //        // And连接，如果左右两端其中一段有Or，则必须加括号
        //        if (Builder.Length > 0 && _regOr.IsMatch(Builder.ToString()))
        //        {
        //            // 有可能本身就有括号了
        //            if (!(Builder[0] == '(' && Builder[Builder.Length - 1] == ')'))
        //            {
        //                Builder.Insert(0, "(");
        //                Builder.Append(")");
        //            }
        //        }

        //        // And连接，如果左右两端其中一段有Or，则必须加括号
        //        if (_regOr.IsMatch(exp))
        //        {
        //            // 有可能本身就有括号了
        //            if (!(exp[0] == '(' && exp[exp.Length - 1] == ')')) { exp = "(" + exp + ")"; }
        //        }

        //        Append("And", exp);
        //    }

        //    return this;
        //}

        ///// <summary>Or操作</summary>
        ///// <param name="exp"></param>
        ///// <returns></returns>
        //public WhereExpression Or(String exp)
        //{
        //    if (String.IsNullOrEmpty(exp)) return this;

        //    //if (Builder.Length > 0 && Builder[Builder.Length - 1] != '(') Builder.Append(" Or ");
        //    //Builder.Append(exp);
        //    if (!String.IsNullOrEmpty(exp)) Append("Or", exp);

        //    return this;
        //}

        /// <summary>有条件And操作</summary>
        /// <param name="condition"></param>
        /// <param name="exp"></param>
        /// <returns></returns>
        public WhereExpression AndIf(Boolean condition, Expression exp) { return condition ? And(exp) : this; }

        /// <summary>有条件Or操作</summary>
        /// <param name="condition"></param>
        /// <param name="exp"></param>
        /// <returns></returns>
        public WhereExpression OrIf(Boolean condition, Expression exp) { return condition ? Or(exp) : this; }

        ///// <summary>左括号</summary>
        ///// <returns></returns>
        //public WhereExpression Left() { Builder.Append("("); return this; }

        ///// <summary>右括号</summary>
        ///// <returns></returns>
        //public WhereExpression Right() { Builder.Append(")"); return this; }

        ///// <summary>已重载。</summary>
        ///// <returns></returns>
        //public override string ToString()
        //{
        //    if (Builder == null || Builder.Length <= 0) return null;

        //    var str = Builder.ToString();
        //    if (str.Length <= 5 && str.Replace(" ", null) == "1=1") return null;
        //    return str;
        //}

        ///// <summary>类型转换</summary>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //public static implicit operator String(WhereExpression obj)
        //{
        //    return obj != null ? obj.ToString() : null;
        //}
        #endregion

        #region 重载运算符
        //private Boolean skipNext = false;

        ///// <summary>重载运算符实现And操作，同时通过布尔型支持AndIf</summary>
        ///// <param name="exp"></param>
        ///// <param name="value">数值</param>
        ///// <returns></returns>
        //public static WhereExpression operator &(WhereExpression exp, Object value)
        //{
        //    if (value == null) return exp;

        //    // 如果是布尔型，表明是下一段的条件语句
        //    if (value is Boolean)
        //    {
        //        if (exp != null) exp.skipNext = !(Boolean)value;
        //        return exp;
        //    }

        //    // 如果exp为空
        //    if (exp == null) return new WhereExpression(value + "");

        //    // 如果上一个要求这里跳过，则跳过
        //    if (exp.skipNext)
        //    {
        //        exp.skipNext = false;
        //        return exp;
        //    }

        //    // 检查空对象
        //    if (value is WhereExpression && (value as WhereExpression).Builder.Length <= 0) return exp;

        //    exp.And(value.ToString());
        //    return exp;
        //}

        ///// <summary>重载运算符实现Or操作，同时通过布尔型支持OrIf</summary>
        ///// <param name="exp"></param>
        ///// <param name="value">数值</param>
        ///// <returns></returns>
        //public static WhereExpression operator |(WhereExpression exp, Object value)
        //{
        //    if (value == null) return exp;

        //    // 如果是布尔型，表明是下一段的条件语句
        //    if (value is Boolean)
        //    {
        //        if (exp != null) exp.skipNext = !(Boolean)value;
        //        return exp;
        //    }

        //    // 如果exp为空
        //    if (exp == null) return new WhereExpression(value + "");

        //    // 如果上一个要求这里跳过，则跳过
        //    if (exp.skipNext)
        //    {
        //        exp.skipNext = false;
        //        return exp;
        //    }

        //    // 检查空对象
        //    if (value is WhereExpression && (value as WhereExpression).Builder.Length <= 0) return exp;

        //    exp.Or(value.ToString());
        //    return exp;
        //}
        #endregion
    }
}