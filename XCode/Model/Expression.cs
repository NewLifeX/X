using System;
using System.Text;

namespace XCode
{
    /// <summary>表达式基类</summary>
    public class Expression
    {
        private Object _Value;
        /// <summary>值</summary>
        public Object Value { get { return _Value; } set { _Value = value; } }

        private Int32 _Strict;
        /// <summary>严格模式。在严格模式下将放弃一些不满足要求的表达式。默认false</summary>
        public Int32 Strict { get { return _Strict; } set { _Strict = value; } }

        public Expression() { }

        public Expression(Object value) { Value = value; }

        /// <summary>设置严格模式</summary>
        /// <param name="strict">严格模式。为Null的参数都忽略</param>
        /// <param name="fullStrict">完全严格模式。整型0、时间最小值、空字符串，都忽略</param>
        /// <returns></returns>
        public Expression SetStrict(Boolean strict = true, Boolean fullStrict = true)
        {
            if (fullStrict)
                Strict = 2;
            else if (strict)
                Strict = 1;

            return this;
        }

        ///// <summary>是否有效表达式</summary>
        //public Boolean IsValid { get { } }

        public void Write(StringBuilder sb)
        {
            if (Value != null) sb.Append(Value + "");
        }

        public virtual String GetString() { return Value == null ? null : Value + ""; }

        ///// <summary>输出该表达式的字符串形式</summary>
        ///// <returns></returns>
        //public override String ToString() { return GetString(); }

        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(Expression obj)
        {
            return obj != null ? obj.GetString() : null;
        }

        #region 重载运算符
        /// <summary>重载运算符实现And操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator &(Expression exp, Object value)
        {
            // 如果exp为空，主要考虑右边
            if (exp == null) CreateWhere(value);

            // 左边构造条件表达式，自己是也好，新建立也好
            var where = CreateWhere(exp);

            // 如果右边为空，创建的表达式将会失败，直接返回左边
            return where.And(Create(value));
        }

        /// <summary>为指定值创建表达式，如果目标值本身就是表达式则直接返回</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static Expression Create(Object value)
        {
            if (value == null) return null;

            var exp = value as Expression;
            if (exp == null) exp = new Expression { Value = value };
            return exp;
        }

        //static WhereExpression CreateWhere(Expression exp)
        //{
        //    var where = exp as WhereExpression;
        //    if (where == null) where = new WhereExpression(exp);
        //    return where;
        //}

        static WhereExpression CreateWhere(Object value)
        {
            if (value == null) return new WhereExpression();
            if (value is WhereExpression) return (value as WhereExpression);

            return new WhereExpression(Create(value));
        }

        /// <summary>重载运算符实现Or操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static WhereExpression operator |(Expression exp, Object value)
        {
            // 如果exp为空，主要考虑右边
            if (exp == null) CreateWhere(value);

            // 左边构造条件表达式，自己是也好，新建立也好
            var where = CreateWhere(exp);

            // 如果右边为空，创建的表达式将会失败，直接返回左边
            return where.Or(Create(value));
        }
        #endregion
    }
}