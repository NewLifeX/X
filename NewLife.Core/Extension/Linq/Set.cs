#if !NET4
using System;
using System.Collections.Generic;
using System.Runtime;

namespace NewLife.Linq
{
    internal class Set<TElement>
    {
        internal struct Slot
        {
            internal int hashCode;
            internal TElement value;
            internal int next;
        }

        private int[] _buckets = new int[7];
        private Slot[] _slots = new Slot[7];
        private int _count;
        private int _freeList = -1;
        private IEqualityComparer<TElement> _comparer;

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public Set() : this(null) { }

        public Set(IEqualityComparer<TElement> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TElement>.Default;

            _comparer = comparer;
        }

        public bool Add(TElement value)
        {
            return !Find(value, true);
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public bool Contains(TElement value)
        {
            return Find(value, false);
        }

        public bool Remove(TElement value)
        {
            int hash = InternalGetHashCode(value);
            int num2 = hash % _buckets.Length;
            int pri = -1;
            for (int i = _buckets[num2] - 1; i >= 0; i = _slots[i].next)
            {
                if (_slots[i].hashCode == hash && _comparer.Equals(_slots[i].value, value))
                {
                    if (pri < 0)
                        _buckets[num2] = _slots[i].next + 1;
                    else
                        _slots[pri].next = _slots[i].next;

                    _slots[i].hashCode = -1;
                    _slots[i].value = default(TElement);
                    _slots[i].next = _freeList;
                    _freeList = i;
                    return true;
                }
                pri = i;
            }
            return false;
        }

        private bool Find(TElement value, bool add)
        {
            int hash = InternalGetHashCode(value);
            for (int i = _buckets[hash % _buckets.Length] - 1; i >= 0; i = _slots[i].next)
            {
                if (_slots[i].hashCode == hash && _comparer.Equals(_slots[i].value, value)) return true;
            }
            if (add)
            {
                int n;
                if (_freeList >= 0)
                {
                    n = _freeList;
                    _freeList = _slots[n].next;
                }
                else
                {
                    if (_count == _slots.Length) Resize();

                    n = _count;
                    _count++;
                }
                int num3 = hash % _buckets.Length;
                _slots[n].hashCode = hash;
                _slots[n].value = value;
                _slots[n].next = _buckets[num3] - 1;
                _buckets[num3] = n + 1;
            }
            return false;
        }

        private void Resize()
        {
            int num = checked(_count * 2 + 1);
            int[] array = new int[num];
            Slot[] array2 = new Slot[num];
            Array.Copy(_slots, 0, array2, 0, _count);
            for (int i = 0; i < _count; i++)
            {
                int num2 = array2[i].hashCode % num;
                array2[i].next = array[num2] - 1;
                array[num2] = i + 1;
            }
            _buckets = array;
            _slots = array2;
        }

        internal int InternalGetHashCode(TElement value)
        {
            if (value != null) return _comparer.GetHashCode(value) & 0x7FFFFFFF;

            return 0;
        }
    }
}
#endif