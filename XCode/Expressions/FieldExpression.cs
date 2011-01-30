using System;
using System.Collections.Generic;
using System.Text;
using XCode.Configuration;

namespace XCode.Expressions
{
    class FieldExpression<TEntity> where TEntity : Entity<TEntity>, new()
    {
        private FieldItem _Field;
        /// <summary>字段</summary>
        public FieldItem Field
        {
            get { return _Field; }
            set { _Field = value; }
        }

        public FieldExpression(FieldItem field) { Field = field; }

        private String SqlDataFormat(String name, Object value) { return Entity<TEntity>.Meta.FormatValue(name, value); }

        //WhereExpression Equal(Object obj)
        //{
        //    WhereExpression ep = new WhereExpression();
        //    String str = String.Format("{0}={1}", Field.Name, SqlDataFormat(obj, Field));
        //}
    }
}
