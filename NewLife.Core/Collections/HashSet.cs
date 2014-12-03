using NewLife;

namespace System.Collections.Generic
{
#if !NET4
    /// <summary>哈希集合。内部采用泛型字典实现，如若在.Net 4.0环境，可直接使用.Net 4.0的HashSet。</summary>
    /// <typeparam name="T"></typeparam>
    public class HashSet<T> : ICollection<T>
    {
        #region 初始化
        Dictionary<T, T> _dic;

        /// <summary>实例化一个哈希集合</summary>
        public HashSet() : this(null, null) { }

        /// <summary>指定比较接口实例化一个哈希集合</summary>
        /// <param name="comparer"></param>
        public HashSet(IEqualityComparer<T> comparer) : this(null, comparer) { }

        /// <summary>指定来源数据实例化一个哈希集合</summary>
        /// <param name="data"></param>
        public HashSet(IEnumerable data) : this(data, null) { }

        /// <summary>指定来源数据和比较接口实例化一个哈希集合</summary>
        /// <param name="data"></param>
        /// <param name="comparer"></param>
        public HashSet(IEnumerable data, IEqualityComparer<T> comparer)
        {
            if (comparer == null)
                _dic = new Dictionary<T, T>();
            else
                _dic = new Dictionary<T, T>(comparer);

            if (data != null)
            {
                foreach (T item in data)
                {
                    //_dic.Add(item, item);
                    _dic[item] = item;
                }
            }
        }
        #endregion

        #region ICollection<T> 成员
        /// <summary>将指定的键和值添加到集合中。</summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            if (IsReadOnly) throw new XException("不能改变只读集合！");

            _dic.Add(item, item);
        }

        /// <summary>从集合中移除所有的键和值。</summary>
        public void Clear()
        {
            if (IsReadOnly) throw new XException("不能改变只读集合！");

            _dic.Clear();
        }

        /// <summary>确定集合是否包含指定的元素。</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            if (item == null) return false;
            return _dic.ContainsKey(item);
        }

        /// <summary>从特定的 System.Array 索引开始，将集合的元素复制到一个 System.Array 中。</summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _dic.Keys.CopyTo(array, arrayIndex);
        }

        /// <summary>从集合中移除特定对象的第一个匹配项。</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            if (IsReadOnly) throw new XException("不能改变只读集合！");

            return _dic.Remove(item);
        }

        /// <summary>获取集合中包含的元素数。</summary>
        public int Count
        {
            get { return _dic.Count; }
        }

        private bool _IsReadOnly;
        /// <summary>获取一个值，该值指示集合是否为只读。</summary>
        public bool IsReadOnly
        {
            get { return _IsReadOnly; }
            set { _IsReadOnly = value; }
        }
        #endregion

        #region IEnumerable<T> 成员
        /// <summary>返回一个循环访问集合的枚举数。</summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _dic.Keys.GetEnumerator();
        }
        #endregion

        #region IEnumerable 成员
        /// <summary>返回一个循环访问集合的枚举数。</summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            //return _dic.Keys.GetEnumerator();
            return GetEnumerator();
        }
        #endregion
    }
#endif
}