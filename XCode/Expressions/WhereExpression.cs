//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace XCode.Expressions
//{
//    /// <summary>
//    /// 条件表达式
//    /// </summary>
//    public class WhereExpression
//    {
//        #region 属性
//        private Int32 addCount = 0;
//        private Int32 orCount = 0;
//        #endregion

//        #region 输出
//        private StringBuilder builder = new StringBuilder();

//        void Append(String action, String content)
//        {
//            if (builder.Length > 0) builder.AppendFormat(" {0} ", action.Trim());
//            builder.Append(content);
//        }

//        /// <summary>
//        /// 输出Where字句
//        /// </summary>
//        /// <returns></returns>
//        public override string ToString()
//        {
//            return builder.ToString();
//        }
//        #endregion

//        //public WhereExpression And(WhereExpression exp)
//        //{
//        //    if (!String.IsNullOrEmpty(sql)) Append("And", sql);

//        //    return this;
//        //}

//        public WhereExpression And(String sql)
//        {
//            if (!String.IsNullOrEmpty(sql)) Append("And", sql);

//            return this;
//        }

//        public WhereExpression Or(String sql)
//        {
//            if (!String.IsNullOrEmpty(sql)) Append("Or", sql);

//            return this;
//        }

//        public WhereExpression AndIf(Boolean condition, String sql)
//        {
//            return condition ? And(sql) : this;
//        }

//        public WhereExpression OrIf(Boolean condition, String sql)
//        {
//            return condition ? Or(sql) : this;
//        }

//        /// <summary>
//        /// 类型转换
//        /// </summary>
//        /// <param name="obj"></param>
//        /// <returns></returns>
//        public static implicit operator String(WhereExpression obj)
//        {
//            return obj != null ? obj.ToString() : null;
//        }

//        #region 重载运算符
//        private Boolean skipNext = false;

//        public static WhereExpression operator &(WhereExpression field, Object value)
//        {
//            // 如果是布尔型，表明是下一段的条件语句
//            if (value is Boolean)
//            {
//                field.skipNext = !(Boolean)value;
//                return field;
//            }
//            // 如果上一个要求这里跳过，则跳过
//            if (field.skipNext)
//            {
//                field.skipNext = false;
//                return field;
//            }

//            field.And(value.ToString());
//            return field;
//        }
//        public static WhereExpression operator |(WhereExpression field, Object value)
//        {
//            // 如果是布尔型，表明是下一段的条件语句
//            if (value is Boolean)
//            {
//                field.skipNext = !(Boolean)value;
//                return field;
//            }
//            // 如果上一个要求这里跳过，则跳过
//            if (field.skipNext)
//            {
//                field.skipNext = false;
//                return field;
//            }

//            field.Or(value.ToString());
//            return field;
//        }
//        #endregion
//    }
//}