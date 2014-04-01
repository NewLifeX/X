using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#if !NET4
namespace System.Collections.Concurrent
{
    /// <summary>并行字典</summary>
    /// <remarks>
    /// 主要通过建立拷贝字典以及只读的键集合和值集合实现线程安全。
    /// </remarks>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IDictionary, ICollection, IEnumerable
    {
        #region 属性
        private IDictionary<TKey, TValue> _Dic;

        private IDictionary<TKey, TValue> _Backup;
        /// <summary>备份字典</summary>
        private IDictionary<TKey, TValue> Backup
        {
            get
            {
                if (_Backup == null)
                {
                    lock (_Dic)
                    {
                        // 建立备份字典
                        IEqualityComparer<TKey> comparer = null;
                        if (_Dic is Dictionary<TKey, TValue>) comparer = (_Dic as Dictionary<TKey, TValue>).Comparer;
                        _Backup = new Dictionary<TKey, TValue>(_Dic, comparer);
                    }
                }
                return _Backup;
            }
            set
            {
                _Backup = null;
                _Keys = null;
                _Values = null;
            }
        }

        /// <summary>元素个数</summary>
        public Int32 Count { get { return Backup.Count; } }

        /// <summary>是否空</summary>
        public Boolean IsEmpty { get { return Backup.Count == 0; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ConcurrentDictionary() { _Dic = new Dictionary<TKey, TValue>(); }

        /// <summary>使用比较器实例化</summary>
        /// <param name="comparer"></param>
        public ConcurrentDictionary(IEqualityComparer<TKey> comparer) { _Dic = new Dictionary<TKey, TValue>(comparer); }

        /// <summary>使用字典实例化</summary>
        /// <param name="collection"></param>
        public ConcurrentDictionary(IDictionary<TKey, TValue> collection) { _Dic = collection; }
        #endregion

        #region 索引
        /// <summary>索引器</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                TValue local;
                if (!TryGetValue(key, out local)) throw new KeyNotFoundException();
                return local;
            }
            set
            {
                if (key == null) throw new ArgumentNullException("key");

                this.TryAdd(key, value);
            }
        }

        private ReadOnlyCollection<TKey> _Keys;
        /// <summary>只读键集合</summary>
        /// <remarks>返回键集合的拷贝，线程安全，但是外部不要修改数据，否则需要等待下次更新字典时才能更新数据</remarks>
        public ICollection<TKey> Keys { get { return _Keys ?? (_Keys = new ReadOnlyCollection<TKey>(Backup.Keys.ToList())); } }

        private ReadOnlyCollection<TValue> _Values;
        /// <summary>只读值集合</summary>
        /// <remarks>返回值集合的拷贝，线程安全，但是外部不要修改数据，否则需要等待下次更新字典时才能更新数据</remarks>
        public ICollection<TValue> Values { get { return _Values ?? (_Values = new ReadOnlyCollection<TValue>(Backup.Values.ToList())); } }
        #endregion

        #region 方法
        /// <summary>清空</summary>
        public void Clear()
        {
            if (IsEmpty) return;

            lock (_Dic)
            {
                _Dic.Clear();
                Backup = null;
            }
        }

        /// <summary>是否包含</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean ContainsKey(TKey key)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (IsEmpty) return false;

            return Backup.ContainsKey(key);
        }

        /// <summary>尝试获取</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            if (IsEmpty) return false;
            if (!Backup.ContainsKey(key)) return false;

            //lock (_Dic)
            //{
            //    return _Dic.TryGetValue(key, out value);
            //}
            return Backup.TryGetValue(key, out value);
        }

        /// <summary>添加元素</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void Add(TKey key, TValue value)
        {
            lock (_Dic)
            {
                _Dic.Add(key, value);
                Backup = null;
            }
        }

        /// <summary>删除</summary>
        /// <param name="key"></param>
        public Boolean Remove(TKey key)
        {
            if (!Keys.Contains(key)) return false;

            lock (_Dic)
            {
                _Dic.Remove(key);
                Backup = null;
            }

            return true;
        }

        /// <summary>尝试添加</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryAdd(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException("key");

            Add(key, value);

            return true;
        }
        #endregion

        #region IDictionary 成员
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) { Add(item.Key, item.Value); }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) { return Backup.Contains(item); }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { Backup.CopyTo(array, arrayIndex); }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly { get { return false; } }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) { return Remove(item.Key); }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() { return Backup.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return Backup.GetEnumerator(); }
        #endregion

        #region ICollection 成员
        void ICollection.CopyTo(Array array, int index) { (Backup as ICollection).CopyTo(array, index); }

        bool ICollection.IsSynchronized { get { return false; } }

        object ICollection.SyncRoot { get { throw new NotSupportedException(); } }
        #endregion

        #region IDictionary成员
        void IDictionary.Add(object key, object value) { Add((TKey)key, (TValue)value); }

        bool IDictionary.Contains(object key) { return (Backup as IDictionary).Contains(key); }

        IDictionaryEnumerator IDictionary.GetEnumerator() { return (Backup as IDictionary).GetEnumerator(); }

        bool IDictionary.IsFixedSize { get { return false; } }

        bool IDictionary.IsReadOnly { get { return false; } }

        ICollection IDictionary.Keys { get { return (Backup as IDictionary).Keys; } }

        void IDictionary.Remove(object key) { (Backup as IDictionary).Remove(key); }

        ICollection IDictionary.Values { get { return (Backup as IDictionary).Values; } }

        object IDictionary.this[object key] { get { return (Backup as IDictionary)[key]; } set { (Backup as IDictionary)[key] = value; } }
        #endregion
    }
}
#endif