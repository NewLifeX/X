using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Reflection;
using XCode.Configuration;

namespace XCode
{
    public partial class EntityList<T> : IListSource, ITypedList, IBindingList, IBindingListView
    {
        #region IListSource接口
        bool IListSource.ContainsListCollection
        {
            get { return false; }
        }

        IList IListSource.GetList()
        {
            // 如果是接口，创建新的集合，否则返回自身
            if (!typeof(T).IsInterface) return this;

            if (Count < 1) return null;

            return ToArray(null);
        }
        #endregion

        #region 复制
        IList ToArray(Type type)
        {
            if (Count < 1) return null;

            // 元素类型
            if (type == null) type = this[0].GetType();
            // 泛型
            type = typeof(EntityList<>).MakeGenericType(type);

            // 初始化集合，实际上是创建了一个真正的实体类型
            IList list = TypeX.CreateInstance(type) as IList;
            for (int i = 0; i < Count; i++)
            {
                list.Add(this[i]);
            }

            return list;
        }
        #endregion

        #region ITypedList接口
        PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            Type type = typeof(T);
            // 如果是接口，使用第一个元素的类型
            if (type.IsInterface)
            {
                if (Count > 0) type = this[0].GetType();
            }
            // 调用TypeDescriptor获取属性
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(type);
            if (pdc == null || pdc.Count <= 0) return pdc;

            // 准备实体操作者
            IEntityOperate factory = EntityFactory.CreateOperate(type);
            if (factory == null) return pdc;

            // 准备字段集合
            Dictionary<String, FieldItem> dic = new Dictionary<string, FieldItem>();
            //factory.Fields.ForEach(item => dic.Add(item.Name, item));
            foreach (FieldItem item in factory.Fields)
            {
                dic.Add(item.Name, item);
            }

            List<PropertyDescriptor> list = new List<PropertyDescriptor>();
            foreach (PropertyDescriptor item in pdc)
            {
                // 显示名与属性名相同，并且没有DisplayName特性
                if (item.Name == item.DisplayName && !ContainAttribute(item.Attributes, typeof(DisplayNameAttribute)))
                {
                    // 添加一个特性
                    FieldItem fi = null;
                    if (dic.TryGetValue(item.Name, out fi) && !String.IsNullOrEmpty(fi.DisplayName))
                    {
                        DisplayNameAttribute dis = new DisplayNameAttribute(fi.DisplayName);
                        list.Add(TypeDescriptor.CreateProperty(type, item, dis));
                        continue;
                    }
                }
                list.Add(item);
            }
            pdc = new PropertyDescriptorCollection(list.ToArray());

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

        string ITypedList.GetListName(PropertyDescriptor[] listAccessors)
        {
            return null;
        }

        //class MyPropertyDescriptor : PropertyDescriptor
        //{
        //    #region 重载
        //    PropertyDescriptor pd;

        //    public MyPropertyDescriptor(PropertyDescriptor p)
        //        : base(p)
        //    {
        //        pd = p;
        //        Fix();
        //    }

        //    public override bool CanResetValue(object component)
        //    {
        //        return pd.CanResetValue(component);
        //    }

        //    public override Type ComponentType
        //    {
        //        get { return pd.ComponentType; }
        //    }

        //    public override object GetValue(object component)
        //    {
        //        return pd.GetValue(component);
        //    }

        //    public override bool IsReadOnly
        //    {
        //        get { return pd.IsReadOnly; }
        //    }

        //    public override Type PropertyType
        //    {
        //        get { return pd.PropertyType; }
        //    }

        //    public override void ResetValue(object component)
        //    {
        //        pd.ResetValue(component);
        //    }

        //    public override void SetValue(object component, object value)
        //    {
        //        pd.SetValue(component, value);
        //    }

        //    public override bool ShouldSerializeValue(object component)
        //    {
        //        return pd.ShouldSerializeValue(component);
        //    }
        //    #endregion

        //    #region 改写
        //    private String _Category;
        //    /// <summary>类别</summary>
        //    public override String Category
        //    {
        //        get { return _Category ?? base.Category; }
        //        //set { _Category = value; }
        //    }

        //    private String _DisplayName;
        //    /// <summary>显示名</summary>
        //    public override String DisplayName
        //    {
        //        get { return _DisplayName ?? base.DisplayName; }
        //        //set { _DisplayName = value; }
        //    }

        //    static DescriptionAttribute emptyDes = new DescriptionAttribute();
        //    static DisplayNameAttribute emptyDis = new DisplayNameAttribute();
        //    static BindColumnAttribute emptyBind = new BindColumnAttribute();

        //    void Fix()
        //    {
        //        BindColumnAttribute bc = pd.Attributes[typeof(BindColumnAttribute)] as BindColumnAttribute;

        //        // 显示名和属性名相同、没有DisplayName特性、有Description特性
        //        if (pd.DisplayName == pd.Name && !pd.Attributes.Contains(emptyDis))
        //        {
        //            DescriptionAttribute des = pd.Attributes[typeof(DescriptionAttribute)] as DescriptionAttribute;
        //            if (des != null)
        //            {
        //                if (!String.IsNullOrEmpty(bc.Description)) _DisplayName = des.Description;
        //            }
        //            if (pd.DisplayName == pd.Name && bc != null)
        //            {
        //                if (!String.IsNullOrEmpty(bc.Description)) _DisplayName = bc.Description;
        //            }
        //        }
        //    }
        //    #endregion
        //}
        #endregion

        #region IBindingList接口
        #region 属性
        Boolean _AllowEdit = true;
        /// <summary>获取是否可更新列表中的项。</summary>
        bool IBindingList.AllowEdit { get { return _AllowEdit; } }
        bool AllowEdit
        {
            get { return _AllowEdit; }
            set { if (_AllowEdit != value) { _AllowEdit = value; OnListChanged(ResetEventArgs); }; }
        }

        Boolean _AllowNew = true;
        /// <summary>获取是否可以使用 System.ComponentModel.IBindingList.AddNew() 向列表中添加项。</summary>
        bool IBindingList.AllowNew { get { return _AllowNew; } }
        bool AllowNew
        {
            get { return _AllowNew; }
            set { if (_AllowNew != value) { _AllowNew = value; OnListChanged(ResetEventArgs); }; }
        }

        Boolean _AllowRemove = true;
        /// <summary>获取是否可以使用 System.Collections.IList.Remove(System.Object) 或 System.Collections.IList.RemoveAt(System.Int32)从列表中移除项。</summary>
        bool IBindingList.AllowRemove { get { return _AllowRemove; } }
        bool AllowDelete
        {
            get { return _AllowRemove; }
            set { if (_AllowRemove != value) { _AllowRemove = value; OnListChanged(ResetEventArgs); }; }
        }

        Boolean _IsSorted = true;
        /// <summary>获取是否对列表中的项进行排序。</summary>
        bool IBindingList.IsSorted { get { return _IsSorted; } }
        bool IsSorted
        {
            get { return _IsSorted; }
            set { if (_IsSorted != value) { _IsSorted = value; OnListChanged(ResetEventArgs); }; }
        }

        ListSortDirection IBindingList.SortDirection
        {
            //TODO 未实现
            get { throw new NotImplementedException(); }
        }

        PropertyDescriptor IBindingList.SortProperty
        {
            //TODO 未实现
            get { throw new NotImplementedException(); }
        }

        bool IBindingList.SupportsChangeNotification
        {
            get { return true; }
        }

        bool IBindingList.SupportsSearching
        {
            get { return true; }
        }

        bool IBindingList.SupportsSorting
        {
            get { return true; }
        }
        #endregion

        #region 事件
        event ListChangedEventHandler _ListChanged;
        event ListChangedEventHandler IBindingList.ListChanged
        {
            add { _ListChanged += value; }
            remove { _ListChanged -= value; }
        }

        static ListChangedEventArgs ResetEventArgs = new ListChangedEventArgs(ListChangedType.Reset, -1);

        void OnListChanged(ListChangedEventArgs e)
        {
            //DataColumn dataColumn = null;
            //string propName = null;
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemMoved:
                case ListChangedType.ItemChanged:
                    if (0 <= e.NewIndex)
                    {
                        //DataRow row = this.GetRow(e.NewIndex);
                        //if (row.HasPropertyChanged)
                        //{
                        //    dataColumn = row.LastChangedColumn;
                        //    propName = (dataColumn != null) ? dataColumn.ColumnName : string.Empty;
                        //}
                        //row.ResetLastChangedColumn();
                    }
                    break;
            }
            if (_ListChanged != null)
            {
                //if ((dataColumn != null) && (e.NewIndex == e.OldIndex))
                //{
                //    //ListChangedEventArgs args = new ListChangedEventArgs(e.ListChangedType, e.NewIndex, new DataColumnPropertyDescriptor(dataColumn));
                //    //_ListChanged(this, args);
                //}
                //else
                //{
                //    _ListChanged(this, e);
                //}
            }
            //if (propName != null)
            //{
            //    this[e.NewIndex].RaisePropertyChangedEvent(propName);
            //}
        }
        #endregion

        #region 方法
        void IBindingList.AddIndex(PropertyDescriptor property)
        {
            //TODO 未实现
            throw new NotImplementedException();
        }

        object IBindingList.AddNew()
        {
            //TODO 未实现
            throw new NotImplementedException();
        }

        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            //TODO 未实现
            throw new NotImplementedException();
        }

        int IBindingList.Find(PropertyDescriptor property, object key)
        {
            //TODO 未实现
            throw new NotImplementedException();
        }

        void IBindingList.RemoveIndex(PropertyDescriptor property)
        {
            //TODO 未实现
            throw new NotImplementedException();
        }

        void IBindingList.RemoveSort()
        {
            //TODO 未实现
            throw new NotImplementedException();
        }
        #endregion
        #endregion

        #region IBindingListView接口
        void IBindingListView.ApplySort(ListSortDescriptionCollection sorts)
        {
            //TODO 未实现
            throw new NotImplementedException();
        }

        string _Filter;
        string IBindingListView.Filter
        {
            get { return _Filter; }
            set { _Filter = value; }
        }

        void IBindingListView.RemoveFilter()
        {
            _Filter = "";
        }

        ListSortDescriptionCollection IBindingListView.SortDescriptions
        {
            //TODO 未实现
            get { throw new NotImplementedException(); }
        }

        bool IBindingListView.SupportsAdvancedSorting
        {
            get { return true; }
        }

        bool IBindingListView.SupportsFiltering
        {
            get { return true; }
        }
        #endregion
    }
}