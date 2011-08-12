//using System;
//using System.Collections.Generic;
//using System.Text;
//using XCode.Configuration;
//using XCode.Expressions;

//namespace XCode.Model
//{
//    public class EntityField : IField
//    {
//        #region 属性
//        //private String _Name;
//        /// <summary>名称</summary>
//        public String Name
//        {
//            get { return Field.Name; }
//            //set { _Name = value; }
//        }

//        private FieldItem _Field;
//        /// <summary>字段</summary>
//        public FieldItem Field
//        {
//            get { return _Field; }
//            //set { _Field = value; }
//        }
//        #endregion

//        #region 构造
//        public EntityField(FieldItem field) { _Field = field; }
//        #endregion

//        #region 方法
//        public FieldExpression Equal(object value)
//        {
//            return new FieldExpression(this, FieldOperators.Equal, value);
//        }

//        public FieldExpression NotEqual(object value)
//        {
//            return new FieldExpression(this, FieldOperators.NotEqual, value);
//        }

//        public FieldExpression Greater(object value)
//        {
//            return new FieldExpression(this, FieldOperators.Greater, value);
//        }

//        public FieldExpression Less(object value)
//        {
//            return new FieldExpression(this, FieldOperators.Less, value);
//        }

//        public FieldExpression GreaterOrEqual(object value)
//        {
//            return new FieldExpression(this, FieldOperators.GreaterOrEqual, value);
//        }

//        public FieldExpression LessOrEqual(object value)
//        {
//            return new FieldExpression(this, FieldOperators.LessOrEqual, value);
//        }

//        public FieldExpression StartWith(object value)
//        {
//            return new FieldExpression(this, FieldOperators.StartWith, value);
//        }

//        public FieldExpression EndWith(object value)
//        {
//            return new FieldExpression(this, FieldOperators.EndWith, value);
//        }

//        public FieldExpression Contain(object value)
//        {
//            return new FieldExpression(this, FieldOperators.Contain, value);
//        }

//        public FieldExpression In(object value)
//        {
//            return new FieldExpression(this, FieldOperators.In, value);
//        }
//        #endregion

//        #region 重载运算符
//        public static FieldExpression operator ==(EntityField field, Object value) { return field.Equal(value); }
//        public static FieldExpression operator !=(EntityField field, Object value) { return field.NotEqual(value); }
//        public static FieldExpression operator >(EntityField field, Object value) { return field.Greater(value); }
//        public static FieldExpression operator <(EntityField field, Object value) { return field.Less(value); }
//        public static FieldExpression operator >=(EntityField field, Object value) { return field.GreaterOrEqual(value); }
//        public static FieldExpression operator <=(EntityField field, Object value) { return field.LessOrEqual(value); }
//        //public static FieldExpression operator +(EntityField field, Object value) { return field.StartWith(value); }
//        //public static FieldExpression operator -(EntityField field, Object value) { return field.EndWith(value); }
//        //public static FieldExpression operator &(EntityField field, Object value) { return field.Contain(value); }
//        //public static FieldExpression operator |(EntityField field, Object value) { return field.In(value); }
//        #endregion

//        #region 类型转换
//        /// <summary>
//        /// 类型转换
//        /// </summary>
//        /// <param name="obj"></param>
//        /// <returns></returns>
//        public static implicit operator String(EntityField obj)
//        {
//            return !obj.Equals(null) ? obj.Name : null;
//        }
//        #endregion
//    }
//}