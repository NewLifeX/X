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

        /// <summary>检查Or，当前层外部是否需要加括号</summary>
        public override Boolean HasOr
        {
            get
            {
                // 子表达式都没有，当然空了
                var exps = Expressions;
                if (exps.Count == 0) return false;

                // 第一个有效子表达式不处理
                var valid = 0;
                var first = 0;
                for (int i = 0; i < exps.Count - 1; i += 2)
                {
                    var item = exps[i + 1];
                    item.Strict = Strict;
                    if (item.IsEmpty) continue;

                    // 如果第一个表达式都还没有过，则不检测Or
                    if (valid > 0)
                    {
                        // 除第一个有效子表达式以外，其它子表达式不为空且包含Or，则这里也有Or
                        // 注意，仅考察当前层的Or，不考察内层表达式的Or
                        if (exps[i].Text == "Or") return true;
                    }
                    valid++;
                    first = i + 1;
                }

                // 如果只有一个有效，则以它内部为准
                if (valid == 1) return exps[first].HasOr;

                return false;
            }
        }

        /// <summary>是否为空。构造输出时，空表达式没有输出，跟严格模式设置有很大关系</summary>
        public override Boolean IsEmpty
        {
            get
            {
                // 子表达式都没有，当然空了
                var exps = Expressions;
                if (exps.Count == 0) return true;

                for (int i = 0; i < exps.Count - 1; i += 2)
                {
                    var item = exps[i + 1];
                    item.Strict = Strict;
                    // 任意一个子表达式不为空，则当前表达式也不为空
                    if (!item.IsEmpty) return false;
                }

                return true;
            }
        }

        /// <summary>当前表达式作为子表达式</summary>
        /// <returns></returns>
        public WhereExpression AsChild() { return new WhereExpression(this); }

        /// <summary>输出条件表达式的字符串表示，遍历表达式集合并拼接起来</summary>
        /// <returns></returns>
        public override String GetString()
        {
            var exps = Expressions;
            if (exps.Count == 0) return null;
            if (IsEmpty) return null;

            var first = false;
            var sb = new StringBuilder();
            // 注意：And/Or和表达式成对出现
            for (int i = 0; i < exps.Count - 1; i += 2)
            {
                // 设置严格模式
                var exp = exps[i + 1];
                exp.Strict = Strict;
                if (exp.IsEmpty) continue;

                // 跳过没有返回的表达式
                var str = exp.GetString();
                if (str.IsNullOrWhiteSpace()) continue;

                // 括号
                var bracket = false;
                // 加上前一个And/Or
                if (sb.Length > 0)
                {
                    var concat = exps[i].Text;
                    //// And连接，如果前面有Or，则必须加括号
                    //if (concat == "And" && _regOr.IsMatch(sb.ToString()))
                    //{
                    //    // 有可能本身就有括号了
                    //    if (!(sb[0] == '(' && sb[sb.Length - 1] == ')'))
                    //    {
                    //        sb.Insert(0, "(");
                    //        sb.Append(")");
                    //    }
                    //}
                    sb.AppendFormat(" {0} ", concat);
                    // 如果连接符是And，并且后面包含Or，则增加括号
                    // 注意，仅考察当前层的Or，不考察内层表达式的Or
                    if (concat == "And" && exp.HasOr)
                    {
                        bracket = true;
                        sb.Append("(");
                    }
                }
                sb.Append(str);
                if (bracket) sb.Append(")");

                first = true;
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