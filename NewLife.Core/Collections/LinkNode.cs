using System;

namespace NewLife.Collections
{
    /// <summary>双链表节点</summary>
    /// <typeparam name="T"></typeparam>
    public class LinkNode<T>
    {
        #region 属性
        /// <summary>数值</summary>
        public T Value { get; set; }

        /// <summary>前一个</summary>
        public LinkNode<T> Prev { get; set; }

        /// <summary>下一个</summary>
        public LinkNode<T> Next { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化一个双链表节点</summary>
        public LinkNode() { }

        /// <summary>实例化一个双链表节点</summary>
        /// <param name="value"></param>
        public LinkNode(T value) { Value = value; }
        #endregion

        #region 方法
        /// <summary>在指定节点之后插入</summary>
        /// <param name="after"></param>
        public void InsertAfter(LinkNode<T> after)
        {
            Prev = after ?? throw new ArgumentNullException(nameof(after));
            Next = after.Next;

            after.Next = this;
            if (Next != null) Next.Prev = this;
        }

        /// <summary>在指定节点之前插入</summary>
        /// <param name="before"></param>
        public void InsertBefore(LinkNode<T> before)
        {
            Next = before ?? throw new ArgumentNullException(nameof(before));
            Prev = before.Prev;

            before.Prev = this;
            if (Prev != null) Prev.Next = this;
        }

        /// <summary>移除节点</summary>
        public void Remove()
        {
            if (Prev != null) Prev.Next = Next;
            if (Next != null) Next.Prev = Prev;

            Prev = this;
            Next = this;
        }
        #endregion
    }
}