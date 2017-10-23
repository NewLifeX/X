using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NewLife.Collections
{
    /// <summary>双向链表</summary>
    /// <typeparam name="T"></typeparam>
    public class LinkList<T> : ICollection<T>
    {
        #region 属性
        /// <summary>头部</summary>
        public LinkNode<T> Head { get; private set; }

        /// <summary>尾部</summary>
        public LinkNode<T> Tail { get; private set; }

        private Int32 _Count;
        /// <summary>元素个数</summary>
        public Int32 Count { get => _Count; private set => _Count = value; }

        Boolean ICollection<T>.IsReadOnly => false;
        #endregion

        #region 方法
        /// <summary>添加项</summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            var node = new LinkNode<T>(item);

            if (Head == null)
                Head = Tail = node;
            else
                node.InsertAfter(Tail);

            Interlocked.Increment(ref _Count);
        }

        /// <summary>删除项</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Remove(T item)
        {
            for (var node = Head; node != null; node = node.Next)
            {
                if (Object.Equals(node.Value, item))
                {
                    node.Remove();

                    Interlocked.Decrement(ref _Count);

                    return true;
                }
            }

            return false;
        }

        /// <summary>清空链表</summary>
        public void Clear()
        {
            Head = Tail = null;
            _Count = 0;
        }

        /// <summary>是否包含</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Contains(T item)
        {
            for (var node = Head; node != null; node = node.Next)
            {
                if (Object.Equals(node.Value, item)) return true;
            }

            return false;
        }

        /// <summary>拷贝到数组</summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, Int32 arrayIndex)
        {
            var k = 0;
            for (var node = Head; node != null; node = node.Next, k++)
            {
                array[arrayIndex] = node.Value;
            }
        }

        /// <summary>枚举</summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (var node = Head; node != null; node = node.Next)
            {
                yield return node.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        #endregion
    }
}