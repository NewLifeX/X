using System;

namespace NewLife.Collections
{
    /// <summary>单向链表节点</summary>
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

        /// <summary>初始化</summary>
        public SingleListNode() { }

        /// <summary>使用一个对象初始化一个节点</summary>
        /// <param name="item"></param>
        public SingleListNode(T item) : this(item, null) { }

        /// <summary>使用一个对象和下一个节点初始化一个节点</summary>
        /// <param name="item"></param>
        /// <param name="next"></param>
        public SingleListNode(T item, SingleListNode<T> next)
        {
            Item = item;
            Next = next;
        }

        /// <summary>在单向链表中查找指定项</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Contain(T item)
        {
            for (SingleListNode<T> node = this; node != null; node = node.Next)
            {
                if (Object.Equals(node.Item, item)) return true;
            }
            return false;
        }

        /// <summary>在单向链表中移除指定项</summary>
        /// <param name="item">指定项</param>
        /// <returns></returns>
        public Boolean Remove(T item)
        {
            // 当前项
            if (Object.Equals(Item, item)) return true;

            // 下一项
            for (SingleListNode<T> node = this; node.Next != null; node = node.Next)
            {
                if (Object.Equals(node.Next.Item, item))
                {
                    node.Next = node.Next.Next;
                    return true;
                }
            }
            return false;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var item = Item;
            if (item != null)
                return "" + item;
            else
                return base.ToString();
        }
    }
}