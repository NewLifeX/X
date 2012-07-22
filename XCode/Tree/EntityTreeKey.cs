using System;

namespace XCode
{
    /// <summary>模型字段排序模式</summary>
    public enum EntityTreeKeys
    {
        /// <summary>关联主键</summary>
        Key,

        /// <summary>关联父级键</summary>
        ParentKey,

        /// <summary>关联排序键</summary>
        SortKey,

        /// <summary>关联名称键</summary>
        NameKey
    }

    /// <summary>用于指定实体树各个键</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class EntityTreeKeyAttribute : Attribute
    {
        private EntityTreeKeys _Key;
        /// <summary>实体树键类型</summary>
        public EntityTreeKeys Key { get { return _Key; } set { _Key = value; } }

        private String _Value;
        /// <summary>实体树键名</summary>
        public String Value { get { return _Value; } set { _Value = value; } }

        /// <summary>指定实体类的模型字段排序模式</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public EntityTreeKeyAttribute(EntityTreeKeys key, String value)
        {
            Key = key;
            Value = value;
        }
    }
}