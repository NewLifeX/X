using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Collections
{
    /// <summary>
    /// 单向链表节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class SingleListNode<T>
    {
        private T _Item;
        /// <summary>元素</summary>
        public T Item
        {
            get { return _Item; }
            set { _Item = value; }
        }

        private SingleListNode<T> _Next;
        /// <summary>下一个节点</summary>
        public SingleListNode<T> Next
        {
            get { return _Next; }
            set { _Next = value; }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public SingleListNode() { }

        /// <summary>
        /// 使用一个对象初始化一个节点
        /// </summary>
        /// <param name="item"></param>
        public SingleListNode(T item) : this(item, null) { }

        /// <summary>
        /// 使用一个对象和下一个节点初始化一个节点
        /// </summary>
        /// <param name="item"></param>
        /// <param name="next"></param>
        public SingleListNode(T item, SingleListNode<T> next)
        {
            Item = item;
            Next = next;
        }
    }
}
