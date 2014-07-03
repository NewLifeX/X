using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Collections;

namespace XCode
{
    /// <summary>实体列表视图</summary>
    /// <typeparam name="T">实体类</typeparam>
    partial class EntityListView<T> : ListBase<T>, ITypedList, IBindingList, IBindingListView, ICancelAddNew where T : IEntity
    {
        #region 重载
        /// <summary>初始化</summary>
        public EntityListView()
        {
            // 使用实体列表作为内部列表，便于提供排序等功能
            InnerList = new EntityList<T>();
        }

        /// <summary>实体列表</summary>
        /// <param name="list"></param>
        public EntityListView(IList<T> list)
        {
            InnerList = list;
        }

        /// <summary>已重载。新增元素时，触发事件改变</summary>
        /// <param name="index"></param>
        /// <param name="value">数值</param>
        public override void Insert(int index, T value)
        {
            base.Insert(index, value);

            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, index));
        }

        /// <summary>已重载。从列表中删除项时，同时从数据库中删除实体</summary>
        /// <param name="index"></param>
        public override void RemoveAt(int index)
        {
            T entity = this[index];
            entity.Delete();

            base.RemoveAt(index);

            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
        }
        #endregion

        #region ITypedList接口
        PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            var type = typeof(T);
            // 调用TypeDescriptor获取属性
            var pdc = TypeDescriptor.GetProperties(type);
            if (pdc == null || pdc.Count <= 0) return pdc;

            return EntityBase.Fix(type, pdc);
        }

        string ITypedList.GetListName(PropertyDescriptor[] listAccessors) { return null; }
        #endregion

        #region IBindingList接口
        #region 属性
        Boolean _AllowEdit = true;
        /// <summary>获取是否可更新列表中的项。</summary>
        public Boolean AllowEdit
        {
            get { return _AllowEdit; }
            set { if (_AllowEdit != value) { _AllowEdit = value; OnListChanged(ResetEventArgs); }; }
        }

        Boolean _AllowNew = true;
        /// <summary>获取是否可以使用 System.ComponentModel.IBindingList.AddNew() 向列表中添加项。</summary>
        public Boolean AllowNew
        {
            get { return _AllowNew; }
            set { if (_AllowNew != value) { _AllowNew = value; OnListChanged(ResetEventArgs); }; }
        }

        Boolean _AllowRemove = true;
        /// <summary>获取是否可以使用 System.Collections.IList.Remove(System.Object) 或 System.Collections.IList.RemoveAt(System.Int32)从列表中移除项。</summary>
        public Boolean AllowRemove
        {
            get { return _AllowRemove; }
            set { if (_AllowRemove != value) { _AllowRemove = value; OnListChanged(ResetEventArgs); }; }
        }

        Boolean _IsSorted = false;
        /// <summary>获取是否对列表中的项进行排序。</summary>
        public Boolean IsSorted
        {
            get { return _IsSorted; }
            set { if (_IsSorted != value) { _IsSorted = value; OnListChanged(ResetEventArgs); }; }
        }

        ListSortDirection _SortDirection;
        /// <summary>获取排序的方向。</summary>
        public ListSortDirection SortDirection
        {
            get { return _SortDirection; }
            set { if (_SortDirection != value) { _SortDirection = value; OnListChanged(ResetEventArgs); }; }
        }

        PropertyDescriptor _SortProperty;
        /// <summary>获取正在用于排序的 System.ComponentModel.PropertyDescriptor。</summary>
        public PropertyDescriptor SortProperty
        {
            get { return _SortProperty; }
            set { if (_SortProperty != value) { _SortProperty = value; OnListChanged(ResetEventArgs); }; }
        }

        Boolean IBindingList.SupportsChangeNotification { get { return true; } }

        Boolean IBindingList.SupportsSearching { get { return true; } }

        Boolean IBindingList.SupportsSorting { get { return true; } }
        #endregion

        #region 事件
        /// <summary>列表改变事件</summary>
        public event ListChangedEventHandler ListChanged;

        static ListChangedEventArgs ResetEventArgs = new ListChangedEventArgs(ListChangedType.Reset, -1);

        void OnListChanged(ListChangedEventArgs e)
        {
            if (ListChanged != null) ListChanged(this, e);
        }
        #endregion

        #region 方法
        void IBindingList.AddIndex(PropertyDescriptor property) { }

        object IBindingList.AddNew()
        {
            T entity = (T)Factory.Create();
            base.Add(entity);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, base.IndexOf(entity)));
            return entity;
        }

        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            var list = InnerList as EntityList<T>;
            if (list == null || list.Count < 1) return;

            list.Sort(property.Name, direction == ListSortDirection.Descending);

            IsSorted = true;
            SortProperty = property;
            SortDirection = direction;

            OnListChanged(ResetEventArgs);
        }

        int IBindingList.Find(PropertyDescriptor property, object key)
        {
            var list = InnerList as EntityList<T>;
            if (list == null || list.Count < 1) return -1;

            return list.FindIndex(item => Object.Equals(item[property.Name], key));
        }

        void IBindingList.RemoveIndex(PropertyDescriptor property) { }

        void IBindingList.RemoveSort()
        {
            var list = InnerList as EntityList<T>;
            if (list == null || list.Count < 1) return;

            var fi = Factory.Fields[0];
            var isDesc = false;
            foreach (var item in Factory.Fields)
            {
                if (item.IsIdentity)
                {
                    fi = item;
                    isDesc = true;
                    break;
                }
                else if (item.PrimaryKey)
                {
                    fi = item;
                    isDesc = false;
                    break;
                }
            }
            list.Sort(Factory.Fields[0].Name, isDesc);

            IsSorted = false;
            SortProperty = null;
            SortDirection = ListSortDirection.Ascending;

            OnListChanged(ResetEventArgs);
        }
        #endregion
        #endregion

        #region IBindingListView接口
        void IBindingListView.ApplySort(ListSortDescriptionCollection sorts)
        {
            if (sorts == null || sorts.Count < 1) return;

            var list = InnerList as EntityList<T>;
            if (list == null || list.Count < 1) return;

            var ns = new List<string>();
            var ds = new List<Boolean>();
            foreach (ListSortDescription item in sorts)
            {
                ns.Add(item.PropertyDescriptor.Name);
                ds.Add(item.SortDirection == ListSortDirection.Descending);
            }

            list.Sort(ns.ToArray(), ds.ToArray());

            SortDescriptions = sorts;

            OnListChanged(ResetEventArgs);
        }

        string _Filter;
        string IBindingListView.Filter { get { return _Filter; } set { _Filter = value; } }

        void IBindingListView.RemoveFilter() { _Filter = ""; }

        ListSortDescriptionCollection _SortDescriptions;
        ListSortDescriptionCollection IBindingListView.SortDescriptions { get { return _SortDescriptions; } }
        /// <summary>获取当前应用于数据源的排序说明的集合。</summary>
        ListSortDescriptionCollection SortDescriptions
        {
            get { return _SortDescriptions; }
            set { if (_SortDescriptions != value) { _SortDescriptions = value; OnListChanged(ResetEventArgs); }; }
        }

        Boolean IBindingListView.SupportsAdvancedSorting { get { return true; } }

        Boolean IBindingListView.SupportsFiltering { get { return false; } }
        #endregion

        #region ICancelAddNew 成员
        void ICancelAddNew.CancelNew(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= Count) return;

            RemoveAt(itemIndex);
        }

        void ICancelAddNew.EndNew(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= Count) return;

            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, itemIndex));
        }
        #endregion

        #region 辅助函数
        /// <summary>真正的实体类型。有些场合为了需要会使用IEntity。</summary>
        Type EntityType
        {
            get
            {
                var type = typeof(T);
                if (!type.IsInterface) return type;

                if (Count > 0) return this[0].GetType();

                return type;
            }
        }

        /// <summary>实体操作者</summary>
        IEntityOperate Factory
        {
            get
            {
                var type = EntityType;
                if (type.IsInterface) return null;

                return EntityFactory.CreateOperate(type);
            }
        }
        #endregion
    }
}