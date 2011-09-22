using System;
using System.Collections.Generic;
using System.Runtime;
namespace System.Linq
{
    internal class Set<TElement>
    {
        internal struct Slot
        {
            internal int hashCode;
            internal TElement value;
            internal int next;
        }
        private int[] buckets;
        private Set<TElement>.Slot[] slots;
        private int count;
        private int freeList;
        private IEqualityComparer<TElement> comparer;
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public Set() : this(null) { }
        public Set(IEqualityComparer<TElement> comparer)
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<TElement>.Default;
            }
            this.comparer = comparer;
            this.buckets = new int[7];
            this.slots = new Set<TElement>.Slot[7];
            this.freeList = -1;
        }
        public bool Add(TElement value)
        {
            return !this.Find(value, true);
        }
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public bool Contains(TElement value)
        {
            return this.Find(value, false);
        }
        public bool Remove(TElement value)
        {
            int num = this.InternalGetHashCode(value);
            int num2 = num % this.buckets.Length;
            int num3 = -1;
            for (int i = this.buckets[num2] - 1; i >= 0; i = this.slots[i].next)
            {
                if (this.slots[i].hashCode == num && this.comparer.Equals(this.slots[i].value, value))
                {
                    if (num3 < 0)
                    {
                        this.buckets[num2] = this.slots[i].next + 1;
                    }
                    else
                    {
                        this.slots[num3].next = this.slots[i].next;
                    }
                    this.slots[i].hashCode = -1;
                    this.slots[i].value = default(TElement);
                    this.slots[i].next = this.freeList;
                    this.freeList = i;
                    return true;
                }
                num3 = i;
            }
            return false;
        }
        private bool Find(TElement value, bool add)
        {
            int num = this.InternalGetHashCode(value);
            for (int i = this.buckets[num % this.buckets.Length] - 1; i >= 0; i = this.slots[i].next)
            {
                if (this.slots[i].hashCode == num && this.comparer.Equals(this.slots[i].value, value))
                {
                    return true;
                }
            }
            if (add)
            {
                int num2;
                if (this.freeList >= 0)
                {
                    num2 = this.freeList;
                    this.freeList = this.slots[num2].next;
                }
                else
                {
                    if (this.count == this.slots.Length)
                    {
                        this.Resize();
                    }
                    num2 = this.count;
                    this.count++;
                }
                int num3 = num % this.buckets.Length;
                this.slots[num2].hashCode = num;
                this.slots[num2].value = value;
                this.slots[num2].next = this.buckets[num3] - 1;
                this.buckets[num3] = num2 + 1;
            }
            return false;
        }
        private void Resize()
        {
            int num = checked(this.count * 2 + 1);
            int[] array = new int[num];
            Set<TElement>.Slot[] array2 = new Set<TElement>.Slot[num];
            Array.Copy(this.slots, 0, array2, 0, this.count);
            for (int i = 0; i < this.count; i++)
            {
                int num2 = array2[i].hashCode % num;
                array2[i].next = array[num2] - 1;
                array[num2] = i + 1;
            }
            this.buckets = array;
            this.slots = array2;
        }
        internal int InternalGetHashCode(TElement value)
        {
            if (value != null)
            {
                return this.comparer.GetHashCode(value) & 2147483647;
            }
            return 0;
        }
    }
}