using System;
using System.Text;

namespace XCode
{
    /// <summary>逗号连接表达式</summary>
    public class ConcatExpression : Expression
    {
        #region 属性
        private StringBuilder _Builder = new StringBuilder();
        /// <summary>内置字符串</summary>
        public StringBuilder Builder { get { return _Builder; } set { _Builder = value; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ConcatExpression() { }

        /// <summary>实例化</summary>
        /// <param name="exp"></param>
        public ConcatExpression(String exp) { Builder.Append(exp + ""); }
        #endregion

        #region 方法
        /// <summary>增加</summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public ConcatExpression And(String exp)
        {
            if (String.IsNullOrEmpty(exp)) return this;

            //if (Builder.Length > 0) Builder.Append(",");
            Builder.Separate(",").Append(exp);

            return this;
        }

        /// <summary>已重载。</summary>
        /// <param name="needBracket">外部是否需要括号。如果外部要求括号，而内部又有Or，则加上括号</param>
        /// <returns></returns>
        public override string GetString(Boolean needBracket = false)
        {
            if (Builder == null || Builder.Length <= 0) return null;

            return Builder.ToString();
        }

        ///// <summary>类型转换</summary>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //public static implicit operator String(ConcatExpression obj)
        //{
        //    return obj != null ? obj.GetString() : null;
        //}
        #endregion

        #region 重载运算符
        /// <summary>重载运算符实现And操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static ConcatExpression operator &(WhereExpression exp, ConcatExpression value)
        {
            var left = exp.GetString();
            var ce = new ConcatExpression(left);

            if (value == null) return ce;

            //return ce.And(value.GetString());
            // 条件表达式遇上连接表达式，不需要And或者逗号，只需要一个空格
            ce.Builder.Append(" ").Append(value.GetString());
            return ce;
        }

        /// <summary>重载运算符实现And操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        [Obsolete("==>&")]
        public static ConcatExpression operator +(WhereExpression exp, ConcatExpression value)
        {
            var left = exp.GetString();
            var ce = new ConcatExpression(left);

            if (value == null) return ce;

            //return ce.And(value);
            // 条件表达式遇上连接表达式，不需要And或者逗号，只需要一个空格
            ce.Builder.Append(" ").Append(value);
            return ce;
        }

        /// <summary>重载运算符实现And操作，同时通过布尔型支持AndIf</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static ConcatExpression operator &(ConcatExpression exp, String value)
        {
            if (value == null) return exp;

            //if (value is ConcatExpression)
            //    exp.And((value as ConcatExpression).GetString());
            //else
            //    exp.And(value.ToString());
            exp.And(value);

            return exp;
        }
        #endregion
    }
}