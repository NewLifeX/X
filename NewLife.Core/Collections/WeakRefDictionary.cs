using System;
using System.Collections.Generic;

namespace NewLife.Collections
{
    class WeakRefDictionary<TKey, TValue>
    {
        private class NullObject { }
        private static Type NullObj = typeof(NullObject);
        private readonly Dictionary<TKey, WeakReference> inner = new Dictionary<TKey, WeakReference>();

        public int Count
        {
            get
            {
                CleanAbandonedItems();
                return inner.Count;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue result;
                if (TryGet(key, out result)) return result;

                throw new KeyNotFoundException();
            }
        }

        public void Add(TKey key, TValue value)
        {
            TValue tValue;
            if (TryGet(key, out tValue)) throw new ArgumentException("key", "该键值已经存在！");

            inner.Add(key, new WeakReference(EncodeNullObject(value)));
        }

        private void CleanAbandonedItems()
        {
            List<TKey> list = new List<TKey>();
            foreach (KeyValuePair<TKey, WeakReference> item in inner)
            {
                if (item.Value.Target == null) list.Add(item.Key);
            }

            foreach (TKey item in list)
            {
                inner.Remove(item);
            }
        }

        public bool ContainsKey(TKey key)
        {
            TValue tValue;
            return TryGet(key, out tValue);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, WeakReference> item in inner)
            {
                Object obj = item.Value.Target;
                if (obj != null) yield return new KeyValuePair<TKey, TValue>(item.Key, DecodeNullObject<TValue>(obj));
            }
        }

        public bool Remove(TKey key)
        {
            return inner.Remove(key);
        }

        public bool TryGet(TKey key, out TValue value)
        {
            value = default(TValue);
            WeakReference weakReference;
            if (!inner.TryGetValue(key, out weakReference)) return false;

            object target = weakReference.Target;
            if (target == null)
            {
                inner.Remove(key);
                return false;
            }

            value = DecodeNullObject<TValue>(target);
            return true;
        }

        private static TObject DecodeNullObject<TObject>(object innerValue)
        {
            if (innerValue == NullObj) return default(TObject);

            return (TObject)innerValue;
        }

        private static object EncodeNullObject(object value)
        {
            if (value == null) return NullObj;

            return value;
        }
    }
}