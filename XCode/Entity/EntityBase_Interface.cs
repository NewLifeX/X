using System;
using System.Collections.Generic;
using System.ComponentModel;
using XCode.Configuration;

namespace XCode
{
    public partial class EntityBase : ICustomTypeDescriptor, IEditableObject
    {
        #region INotifyPropertyChanged接口
        /// <summary>属性改变。重载时记得调用基类的该方法，以设置脏数据属性，否则数据将无法Update到数据库。</summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="newValue">新属性值</param>
        /// <returns>是否允许改变</returns>
        protected virtual Boolean OnPropertyChanging(String fieldName, Object newValue)
        {
            // 如果数据没有改变，不应该影响脏数据
            //if (IsFromDatabase && CheckEqual(this[fieldName], newValue)) return false;
            if (CheckEqual(this[fieldName], newValue)) return false;

            Dirtys[fieldName] = true;
            return true;
        }

        /// <summary>检查相等，主要特殊处理时间相等</summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        private Boolean CheckEqual(Object v1, Object v2)
        {
            if (v1 == null || v2 == null) return Equals(v1, v2);

            switch (Type.GetTypeCode(v1.GetType()))
            {
                case TypeCode.DateTime:
                    {
                        var d1 = (DateTime)v1;
                        var d2 = (DateTime)v2;

                        // 时间存储包括年月日时分秒，后面还有微秒，而我们数据库存储默认不需要微秒，所以时间的相等判断需要做特殊处理
                        return d1.Date == d2.Date &&
                            d1.Hour == d2.Hour &&
                            d1.Minute == d2.Minute &&
                            d1.Second == d2.Second;
                    }
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return Convert.ToInt64(v1) == Convert.ToInt64(v2);
                case TypeCode.String:
                    return v1 + "" == v2 + "";
                default:
                    break;
            }

            return Equals(v1, v2);
        }

        /// <summary>属性改变。重载时记得调用基类的该方法，以设置脏数据属性，否则数据将无法Update到数据库。</summary>
        /// <param name="fieldName">字段名</param>
        protected virtual void OnPropertyChanged(String fieldName) { }
        #endregion

        #region ICustomTypeDescriptor 成员
        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            // 重载。从DescriptionAttribute和BindColumnAttribute中获取备注，创建DisplayNameAttribute特性
            var atts = TypeDescriptor.GetAttributes(this, true);

            if (atts != null && !ContainAttribute(atts, typeof(DisplayNameAttribute)))
            {
                var list = new List<Attribute>();
                String description = null;
                foreach (Attribute item in atts)
                {
                    if (item.GetType() == typeof(DescriptionAttribute))
                    {
                        description = (item as DescriptionAttribute).Description;
                        if (!String.IsNullOrEmpty(description)) break;
                    }
                    if (item.GetType() == typeof(BindColumnAttribute))
                    {
                        description = (item as BindColumnAttribute).Description;
                        if (!String.IsNullOrEmpty(description)) break;
                    }
                }

                if (!String.IsNullOrEmpty(description))
                {
                    list.Add(new DisplayNameAttribute(description));
                    atts = new AttributeCollection(list.ToArray());
                }
            }

            return atts;
        }

        String ICustomTypeDescriptor.GetClassName()
        {
            //return TypeDescriptor.GetClassName(this, true);
            return GetType().FullName;
        }

        String ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        Object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return Fix(GetType(), TypeDescriptor.GetProperties(this, attributes, true));
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return Fix(GetType(), TypeDescriptor.GetProperties(this, true));
        }

        Object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        internal static PropertyDescriptorCollection Fix(Type type, PropertyDescriptorCollection pdc)
        {
            if (pdc == null || pdc.Count < 1) return pdc;

            var factory = EntityFactory.CreateOperate(type);

            // 准备字段集合
            var dic = new Dictionary<String, FieldItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in factory.Fields)
            {
                dic.Add(item.Name, item);
            }

            var hasChanged = false;
            var list = new List<PropertyDescriptor>();
            foreach (PropertyDescriptor item in pdc)
            {
                // 显示名与属性名相同，并且没有DisplayName特性
                if (item.Name == item.DisplayName && !ContainAttribute(item.Attributes, typeof(DisplayNameAttribute)))
                {
                    // 添加一个特性
                    if (dic.TryGetValue(item.Name, out var fi) && !String.IsNullOrEmpty(fi.Description))
                    {
                        var dis = new DisplayNameAttribute(fi.Description);
                        list.Add(TypeDescriptor.CreateProperty(type, item, dis));
                        hasChanged = true;
                        continue;
                    }
                }
                list.Add(item);
            }
            if (hasChanged) pdc = new PropertyDescriptorCollection(list.ToArray());

            return pdc;
        }

        static Boolean ContainAttribute(AttributeCollection attributes, Type type)
        {
            if (attributes == null || attributes.Count < 1 || type == null) return false;

            foreach (Attribute item in attributes)
            {
                if (type.IsAssignableFrom(item.GetType())) return true;
            }
            return false;
        }
        #endregion

        #region IEditableObject 成员
        [NonSerialized]
        private EntityBase _bak;

        void IEditableObject.BeginEdit()
        {
            _bak = Clone() as EntityBase;
        }

        void IEditableObject.CancelEdit()
        {
            CopyFrom(_bak, false);

            _bak = null;
        }

        void IEditableObject.EndEdit()
        {
            //Update();
            Save();

            _bak = null;
        }
        #endregion
    }
}