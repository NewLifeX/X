//using System;
//using System.Collections.Generic;
//using System.Text;
//using XCode.Configuration;
//using XCode.Model;

//namespace XCode.Expressions
//{
//    /// <summary>
//    /// 字段表达式
//    /// </summary>
//    public class FieldExpression
//    {
//        #region 属性
//        private IField _Field;
//        /// <summary>字段</summary>
//        public IField Field
//        {
//            get { return _Field; }
//            set { _Field = value; }
//        }

//        private Object _Value;
//        /// <summary>值</summary>
//        public Object Value
//        {
//            get { return _Value; }
//            set { _Value = value; }
//        }

//        private FieldOperators _Operator;
//        /// <summary>操作</summary>
//        public FieldOperators Operator
//        {
//            get { return _Operator; }
//            set { _Operator = value; }
//        }
//        #endregion

//        #region 构造
//        public FieldExpression(IField field, FieldOperators op, Object value)
//        {
//            Field = field;
//            Operator = op;
//            Value = value;
//        }
//        #endregion

//        #region 输出
//        public override string ToString()
//        {
//            IEntityOperate op = EntityFactory.CreateOperate(Field.Field.Table.EntityType);
//            String name = op.FormatName(Field.Name);

//            Boolean isValueField = false;
//            String name2 = null;
//            if (Value is IField)
//            {
//                isValueField = true;
//                name2 = op.FormatName((Value as IField).Name);
//            }

//            switch (Operator)
//            {
//                case FieldOperators.Equal:
//                    if (Value == null) return String.Format("{0} is null", name);
//                    if (isValueField)
//                        return String.Format("{0}={1}", name, name2);
//                    else
//                        return op.MakeCondition(Field.Field, Value, "=");
//                case FieldOperators.NotEqual:
//                    if (Value == null) return String.Format("{0} is not null", name);
//                    if (isValueField)
//                        return String.Format("{0}<>{1}", name, name2);
//                    else
//                        return op.MakeCondition(Field.Field, Value, "<>");
//                case FieldOperators.Greater:
//                    if (isValueField)
//                        return String.Format("{0}>{1}", name, name2);
//                    else
//                        return op.MakeCondition(Field.Field, Value, ">");
//                case FieldOperators.Less:
//                    if (isValueField)
//                        return String.Format("{0}<{1}", name, name2);
//                    else
//                        return op.MakeCondition(Field.Field, Value, "<");
//                case FieldOperators.GreaterOrEqual:
//                    if (isValueField)
//                        return String.Format("{0}>={1}", name, name2);
//                    else
//                        return op.MakeCondition(Field.Field, Value, ">=");
//                case FieldOperators.LessOrEqual:
//                    if (isValueField)
//                        return String.Format("{0}<={1}", name, name2);
//                    else
//                        return op.MakeCondition(Field.Field, Value, "<=");
//                case FieldOperators.StartWith:
//                    return op.MakeCondition(Field.Field, Value, "like '{0}%'");
//                case FieldOperators.EndWith:
//                    return op.MakeCondition(Field.Field, Value, "like '%{0}'");
//                case FieldOperators.Contain:
//                    return op.MakeCondition(Field.Field, Value, "like '%{0}%'");
//                case FieldOperators.In:
//                    break;
//                default:
//                    break;
//            }
//            return base.ToString();
//        }
//        #endregion

//        #region 重载运算符
//        public static WhereExpression operator &(FieldExpression field, Object value)
//        {
//            WhereExpression exp = new WhereExpression();
//            exp.And(field);
//            exp.And(value.ToString());
//            return exp;
//        }
//        public static WhereExpression operator |(FieldExpression field, Object value)
//        {
//            WhereExpression exp = new WhereExpression();
//            exp.And(field);
//            exp.Or(value.ToString());
//            return exp;
//        }
//        #endregion

//        #region 类型转换
//        /// <summary>
//        /// 类型转换
//        /// </summary>
//        /// <param name="obj"></param>
//        /// <returns></returns>
//        public static implicit operator String(FieldExpression obj)
//        {
//            return obj != null ? obj.ToString() : null;
//        }
//        #endregion
//    }
//}