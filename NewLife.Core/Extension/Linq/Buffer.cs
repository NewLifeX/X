#if !NET4
using System;
using System.Collections.Generic;

namespace System.Linq
{
    internal struct Buffer<TElement>
    {
        internal TElement[] items;
        internal int count;
        internal Buffer(IEnumerable<TElement> source)
        {
            TElement[] array = null;
            int num = 0;
            ICollection<TElement> collection = source as ICollection<TElement>;
            if (collection != null)
            {
                num = collection.Count;
                if (num > 0)
                {
                    array = new TElement[num];
                    collection.CopyTo(array, 0);
                }
            }
            else
            {
                foreach (TElement current in source)
                {
                    if (array == null)
                    {
                        array = new TElement[4];
                    }
                    else
                    {
                        if (array.Length == num)
                        {
                            TElement[] array2 = new TElement[checked(num * 2)];
                            Array.Copy(array, 0, array2, 0, num);
                            array = array2;
                        }
                    }
                    array[num] = current;
                    num++;
                }
            }
            this.items = array;
            this.count = num;
        }
        internal TElement[] ToArray()
        {
            if (this.count == 0)
            {
                return new TElement[0];
            }
            if (this.items.Length == this.count)
            {
                return this.items;
            }
            TElement[] array = new TElement[this.count];
            Array.Copy(this.items, 0, array, 0, this.count);
            return array;
        }
    }
}
#endif