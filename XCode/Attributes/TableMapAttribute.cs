using System;
using System.Reflection;

namespace XCode
{
    /// <summary>
    /// 表映射属性。
    /// 用于一个表的某个字段到另一个表的某个字段的映射关系
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class TableMapAttribute : Attribute
    {
        private TableMapType _MapType;
        /// <summary>映射类型</summary>
        public TableMapType MapType
        {
            get { return _MapType; }
            set { _MapType = value; }
        }

        private String _LocalColumn;
        /// <summary>本地列</summary>
        public String LocalColumn
        {
            get { return _LocalColumn; }
            set { _LocalColumn = value; }
        }

        private Type _MapEntity;
        /// <summary>要映射的实体类型</summary>
        public Type MapEntity
        {
            get { return _MapEntity; }
            set { _MapEntity = value; }
        }

        private String _MapColumn;
        /// <summary>要映射的实体类型的字段</summary>
        public String MapColumn
        {
            get { return _MapColumn; }
            set { _MapColumn = value; }
        }

        //private PropertyInfo _LocalField;
        ///// <summary>本地域</summary>
        //public PropertyInfo LocalField
        //{
        //    get { return _LocalField; }
        //    set { _LocalField = value; }
        //}

        /// <summary>
        /// 构造函数。指定一个多表映射关系
        /// </summary>
        /// <param name="maptype">映射类型</param>
        /// <param name="localcolumn">本地列</param>
        /// <param name="mapentity">要映射的实体类型</param>
        /// <param name="mapcolumn">要映射的实体类型的字段</param>
        public TableMapAttribute(TableMapType maptype, String localcolumn, Type mapentity, String mapcolumn)
        {
            MapType = maptype;
            LocalColumn = localcolumn;
            MapEntity = mapentity;
            MapColumn = mapcolumn;

            CheckValid();
        }

        /// <summary>
        /// 检查参数是否有效
        /// </summary>
        /// <returns></returns>
        public Boolean CheckValid()
        {
            if (String.IsNullOrEmpty(LocalColumn)) throw new ArgumentNullException("LocalColumn", "必须指定多表映射的本地关联字段！");
            if (MapEntity == null) throw new ArgumentNullException("MapEntity", "必须指定多表映射的关联实体！");
            if (String.IsNullOrEmpty(MapColumn)) throw new ArgumentNullException("MapColumn", "必须指定多表映射的关联表的关联字段！");
            return true;
        }

        /// <summary>
        /// 检索应用于类型成员的自定义属性。
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static TableMapAttribute GetCustomAttribute(MemberInfo element)
        {
            return GetCustomAttribute(element, typeof(TableMapAttribute)) as TableMapAttribute;
        }
    }

    /// <summary>
    /// 表映射类型。
    /// </summary>
    public enum TableMapType
    {
        /// <summary>
        /// 内部联接
        /// </summary>
        Inner_Join,
        /// <summary>
        /// 左向外部联接
        /// </summary>
        Left_Outer_Join,
        /// <summary>
        /// 右向外部联接
        /// </summary>
        Right_Outer_Join,
        /// <summary>
        /// 完整外部联接
        /// </summary>
        Full_Outer_Join,
        /// <summary>
        /// 交叉联接
        /// </summary>
        Cross_Join
    }
}