using System;
using System.Collections.Generic;
using System.Runtime;
using NewLife.Reflection;
namespace System.Linq
{
    internal abstract class EnumerableSorter<TElement>
    {
        internal abstract void ComputeKeys(TElement[] elements, int count);
        internal abstract int CompareKeys(int index1, int index2);
        internal int[] Sort(TElement[] elements, int count)
        {
            this.ComputeKeys(elements, count);
            int[] array = new int[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = i;
            }
            this.QuickSort(array, 0, count - 1);
            return array;
        }
        private void QuickSort(int[] map, int left, int right)
        {
            do
            {
                int num = left;
                int num2 = right;
                int index = map[num + (num2 - num >> 1)];
                do
                {
                    if (num < map.Length)
                    {
                        if (this.CompareKeys(index, map[num]) > 0)
                        {
                            num++;
                            continue;
                        }
                    }
                    while (num2 >= 0 && this.CompareKeys(index, map[num2]) < 0)
                    {
                        num2--;
                    }
                    if (num > num2)
                    {
                        break;
                    }
                    if (num < num2)
                    {
                        int num3 = map[num];
                        map[num] = map[num2];
                        map[num2] = num3;
                    }
                    num++;
                    num2--;
                }
                while (num <= num2);
                if (num2 - left <= right - num)
                {
                    if (left < num2)
                    {
                        this.QuickSort(map, left, num2);
                    }
                    left = num;
                }
                else
                {
                    if (num < right)
                    {
                        this.QuickSort(map, num, right);
                    }
                    right = num2;
                }
            }
            while (left < right);
        }
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        protected EnumerableSorter()
        {
        }
    }

    internal class EnumerableSorter<TElement, TKey> : EnumerableSorter<TElement>
    {
        internal Func<TElement, TKey> keySelector;
        internal IComparer<TKey> comparer;
        internal bool descending;
        internal EnumerableSorter<TElement> next;
        internal TKey[] keys;
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        internal EnumerableSorter(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, EnumerableSorter<TElement> next)
        {
            this.keySelector = keySelector;
            this.comparer = comparer;
            this.descending = descending;
            this.next = next;
        }
        internal override void ComputeKeys(TElement[] elements, int count)
        {
            this.keys = new TKey[count];
            for (int i = 0; i < count; i++)
            {
                this.keys[i] = this.keySelector(elements[i]);
            }
            if (this.next != null)
            {
                this.next.ComputeKeys(elements, count);
            }
        }
        internal override int CompareKeys(int index1, int index2)
        {
            int num = this.comparer.Compare(this.keys[index1], this.keys[index2]);
            if (num == 0)
            {
                if (this.next == null)
                {
                    return index1 - index2;
                }
                return this.next.CompareKeys(index1, index2);
            }
            else
            {
                if (!this.descending)
                {
                    return num;
                }
                return -num;
            }
        }
    }
}