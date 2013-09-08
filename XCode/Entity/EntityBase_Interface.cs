using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using XCode.Configuration;

namespace XCode
{
    public partial class EntityBase : ICustomTypeDescriptor, IEditableObject, IEnumerable<IEntityEntry>
    {
        #region INotifyPropertyChanged接口
        /// <summary>如果实体来自数据库，在给数据属性赋相同值时，不改变脏数据，其它情况均改变脏数据</summary>
        [NonSerialized]
        internal protected Boolean _IsFromDatabase;

        /// <summary>属性改变。重载时记得调用基类的该方法，以设置脏数据属性，否则数据将无法Update到数据库。</summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="newValue">新属性值</param>
        /// <returns>是否允许改变</returns>
        protected virtual Boolean OnPropertyChanging(String fieldName, Object newValue)
        {
            //#if !NET20SP0
            //            if (_PropertyChanging != null) _PropertyChanging(this, new PropertyChangingEventArgs(fieldName));
            //#endif
            // 如果数据没有改变，不应该影响脏数据
            if (_IsFromDatabase && Object.Equals(this[fieldName], newValue))
            {
                return false;
            }
            else
            {
                Dirtys[fieldName] = true;
                return true;
            }
        }

        /// <summary>属性改变。重载时记得调用基类的该方法，以设置脏数据属性，否则数据将无法Update到数据库。</summary>
        /// <param name="fieldName">字段名</param>
        protected virtual void OnPropertyChanged(String fieldName)
        {
            //#if !NET20SP0
            //            if (_PropertyChanged != null) _PropertyChanged(this, new PropertyChangedEventArgs(fieldName));
            //#endif
        }
        #endregion

        #region ICustomTypeDescriptor 成员
        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            // 重载。从DescriptionAttribute和BindColumnAttribute中获取备注，创建DisplayNameAttribute特性
            AttributeCollection atts = TypeDescriptor.GetAttributes(this, true);

            if (atts != null && !ContainAttribute(atts, typeof(DisplayNameAttribute)))
            {
                List<Attribute> list = new List<Attribute>();
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

        string ICustomTypeDescriptor.GetClassName()
        {
            //return TypeDescriptor.GetClassName(this, true);
            return this.GetType().FullName;
        }

        string ICustomTypeDescriptor.GetComponentName()
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

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
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
            return Fix(this.GetType(), TypeDescriptor.GetProperties(this, attributes, true));
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return Fix(this.GetType(), TypeDescriptor.GetProperties(this, true));
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        internal static PropertyDescriptorCollection Fix(Type type, PropertyDescriptorCollection pdc)
        {
            if (pdc == null || pdc.Count < 1) return pdc;

            IEntityOperate factory = EntityFactory.CreateOperate(type);

            // 准备字段集合
            Dictionary<String, FieldItem> dic = new Dictionary<string, FieldItem>(StringComparer.OrdinalIgnoreCase);
            //factory.Fields.ForEach(item => dic.Add(item.Name, item));
            foreach (FieldItem item in factory.Fields)
            {
                dic.Add(item.Name, item);
            }

            Boolean hasChanged = false;
            List<PropertyDescriptor> list = new List<PropertyDescriptor>();
            foreach (PropertyDescriptor item in pdc)
            {
                // 显示名与属性名相同，并且没有DisplayName特性
                if (item.Name == item.DisplayName && !ContainAttribute(item.Attributes, typeof(DisplayNameAttribute)))
                {
                    // 添加一个特性
                    FieldItem fi = null;
                    if (dic.TryGetValue(item.Name, out fi) && !String.IsNullOrEmpty(fi.Description))
                    {
                        DisplayNameAttribute dis = new DisplayNameAttribute(fi.Description);
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

        #region IEnumerable成员
        IEnumerator<IEntityEntry> IEnumerable<IEntityEntry>.GetEnumerator()
        {
            var op = EntityFactory.CreateOperate(this.GetType());
            foreach (var item in op.AllFields)
            {
                yield return new EntityEntry { Field = item, Entity = this };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() { return (this as IEnumerable<IEntityEntry>).GetEnumerator(); }
        #endregion
    }
}