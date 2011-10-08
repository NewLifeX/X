//using System;
//using System.Collections;
//using System.Collections.Generic;

//namespace XCode.Common
//{
//    /// <summary>
//    /// 哈希集合
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    class HashSet<T> : ICollection<T>
//    {
//        #region 初始化
//        Dictionary<T, T> _dic;

//        public HashSet() : this(null, null) { }

//        public HashSet(IEqualityComparer<T> comparer) : this(comparer, null) { }

//        public HashSet(IEnumerable data) : this(null, data) { }

//        public HashSet(IEqualityComparer<T> comparer, IEnumerable data)
//        {
//            if (comparer == null)
//                _dic = new Dictionary<T, T>();
//            else
//                _dic = new Dictionary<T, T>(comparer);

//            if (data != null)
//            {
//                foreach (T item in data)
//                {
//                    _dic.Add(item, item);
//                }
//            }
//        }
//        #endregion

//        #region ICollection<T> 成员
//        public void Add(T item)
//        {
//            _dic.Add(item, item);
//        }

//        public void Clear()
//        {
//            _dic.Clear();
//        }

//        public bool Contains(T item)
//        {
//            return _dic.ContainsKey(item);
//        }

//        public void CopyTo(T[] array, int arrayIndex)
//        {
//            _dic.Keys.CopyTo(array, arrayIndex);
//        }

//        public int Count
//        {
//            get { return _dic.Count; }
//        }

//        public bool IsReadOnly
//        {
//            get { throw new NotImplementedException(); }
//        }

//        public bool Remove(T item)
//        {
//            return _dic.Remove(item);
//        }
//        #endregion

//        #region IEnumerable<T> 成员
//        public IEnumerator<T> GetEnumerator()
//        {
//            return _dic.Keys.GetEnumerator();
//        }
//        #endregion

//        #region IEnumerable 成员
//        IEnumerator IEnumerable.GetEnumerator()
//        {
//            //return _dic.Keys.GetEnumerator();
//            return GetEnumerator();
//        }
//        #endregion
//    }
//}