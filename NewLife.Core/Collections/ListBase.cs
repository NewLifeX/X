using System;
using System.Collections;
using System.Collections.Generic;

namespace NewLife.Collections
{
    /// <summary>泛型列表基类。主要提供一个重载实现自定义列表的基类实现。</summary>
    /// <typeparam name="T"></typeparam>
    public class ListBase<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
    {
        #region 属性
        private IList<T> _list;
        /// <summary>内部列表</summary>
        protected virtual IList<T> InnerList
        {
            get { return _list ?? (_list = new List<T>()); }
            set { _list = value; }
        }

        /// <summary>列表元素个数</summary>
        public virtual Int32 Count { get { return InnerList.Count; } }

        /// <summary>是否固定大小</summary>
        public virtual Boolean IsFixedSize
        {
            get
            {
                return false;
            }
        }

        /// <summary>是否只读</summary>
        public virtual Boolean IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>获取或设置指定索引处的元素。</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual T this[Int32 index] { get { return InnerList[index]; } set { InnerList[index] = value; } }
        #endregion

        #region 构造
        ///// <summary>
        ///// 初始化一个泛型列表
        ///// </summary>
        //protected ListBase()
        //{
        //}
        #endregion

        #region 方法
        /// <summary>添加</summary>
        /// <param name="value">数值</param>
        public virtual void Add(T value)
        {
            Insert(Count, value);
        }

        /// <summary>清空</summary>
        public virtual void Clear()
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                RemoveAt(i);
            }
        }

        /// <summary>是否包含指定元素</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public virtual Boolean Contains(T value)
        {
            return (IndexOf(value) != -1);
        }

        /// <summary>把元素复制到一个数组里面</summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public virtual void CopyTo(T[] array, Int32 index)
        {
            for (var i = 0; i < Count; i++)
            {
                array[index + i] = this[i];
            }
        }

        /// <summary>获取一个枚举器</summary>
        /// <returns></returns>
        public virtual IEnumerator<T> GetEnumerator()
        {
            //return new IListEnumerator<T>(this);
            //foreach (T item in InnerList)
            //{
            //    yield return item;
            //}
            return InnerList.GetEnumerator();
        }

        /// <summary>确定列表中特定项的索引。</summary>
        /// <param name="value">要在列表中定位的对象。</param>
        /// <returns></returns>
        public virtual Int32 IndexOf(T value)
        {
            //for (int i = 0; i < this.Count; i++)
            //{
            //    if (value.Equals(this[i]))
            //    {
            //        return i;
            //    }
            //}
            //return -1;
            return InnerList.IndexOf(value);
        }

        /// <summary>将一个项插入指定索引处的列表。</summary>
        /// <param name="index"></param>
        /// <param name="value">数值</param>
        public virtual void Insert(Int32 index, T value)
        {
            InnerList.Insert(index, value);
        }

        private static Boolean IsCompatibleType(Object value)
        {
            if (((value != null) || typeof(T).IsValueType) && !(value is T))
            {
                return false;
            }
            return true;
        }

        /// <summary>从列表中移除指定对象</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public virtual Boolean Remove(T value)
        {
            var index = IndexOf(value);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>移除指定索引处的列表项。</summary>
        /// <param name="index"></param>
        public virtual void RemoveAt(Int32 index)
        {
            InnerList.RemoveAt(index);
        }
        #endregion

        #region ICollection接口
        void ICollection.CopyTo(Array array, Int32 index)
        {
            for (var i = 0; i < Count; i++)
            {
                array.SetValue(this[i], index);
            }
        }

        Boolean ICollection.IsSynchronized
        {
            get
            {
                return IsReadOnly;
            }
        }

        Object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }
        #endregion

        #region IEnumerable接口
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            //return new IListEnumerator<T>(this);

            foreach (var item in InnerList)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            //return new IListEnumerator<T>(this);
            return GetEnumerator();
        }
        #endregion

        #region IList接口
        Int32 IList.Add(Object value)
        {
            if (!IsCompatibleType(value)) throw new ArgumentException();

            Add((T)value);
            return (Count - 1);
        }

        void IList.Clear()
        {
            Clear();
        }

        Boolean IList.Contains(Object value)
        {
            if (!IsCompatibleType(value)) return false;

            return Contains((T)value);
        }

        Int32 IList.IndexOf(Object value)
        {
            if (!IsCompatibleType(value)) return -1;

            return IndexOf((T)value);
        }

        void IList.Insert(Int32 index, Object value)
        {
            if (!IsCompatibleType(value)) throw new ArgumentException();

            Insert(index, (T)value);
        }

        void IList.Remove(Object value)
        {
            if (!IsCompatibleType(value)) throw new ArgumentException();

            Remove((T)value);
        }

        Object IList.this[Int32 index]
        {
            get
            {
                return this[index];
            }
            set
            {
                if (!IsCompatibleType(value)) throw new ArgumentException();

                this[index] = (T)value;
            }
        }
        #endregion

        #region 泛型列表枚举器
        ///// <summary>
        ///// 泛型列表枚举器
        ///// </summary>
        ///// <typeparam name="TItem"></typeparam>
        //[StructLayout(LayoutKind.Sequential)]
        //public struct IListEnumerator<TItem> : IEnumerator<TItem>, IDisposable, IEnumerator
        //{
        //    private IList<TItem> sequence;

        //    /// <summary>
        //    /// 使用泛型列表实例化一个枚举器
        //    /// </summary>
        //    /// <param name="sequence"></param>
        //    public IListEnumerator(IList<TItem> sequence)
        //    {
        //        this.sequence = sequence;
        //        this.index = 0;
        //        this.current = default(TItem);
        //    }

        //    /// <summary>
        //    /// 销毁
        //    /// </summary>
        //    public void Dispose()
        //    {
        //        sequence = null;
        //    }

        //    private TItem current;
        //    /// <summary>
        //    /// 获取集合中的当前元素。
        //    /// </summary>
        //    public TItem Current
        //    {
        //        get
        //        {
        //            return this.current;
        //        }
        //    }

        //    private int index;
        //    /// <summary>
        //    /// 获取集合中的当前元素。
        //    /// </summary>
        //    object IEnumerator.Current
        //    {
        //        get
        //        {
        //            if (this.index == 0)
        //            {
        //                throw new InvalidOperationException("枚举未开始！");
        //            }
        //            if (this.index > this.sequence.Count)
        //            {
        //                throw new InvalidOperationException("枚举已结束！");
        //            }
        //            return this.current;
        //        }
        //    }

        //    /// <summary>
        //    /// 将枚举数推进到集合的下一个元素。
        //    /// </summary>
        //    /// <returns></returns>
        //    public bool MoveNext()
        //    {
        //        if (this.index < this.sequence.Count)
        //        {
        //            this.current = this.sequence[this.index];
        //            this.index++;
        //            return true;
        //        }
        //        this.current = default(TItem);
        //        return false;
        //    }

        //    /// <summary>
        //    /// 将枚举数设置为其初始位置，该位置位于集合中第一个元素之前。
        //    /// </summary>
        //    void IEnumerator.Reset()
        //    {
        //        this.index = 0;
        //        this.current = default(TItem);
        //    }
        //}
        #endregion
    }
}