using System;
using System.ComponentModel;
using System.Threading;

namespace NewLife.Collections
{
    /// <summary>双链表节点</summary>
    /// <typeparam name="T"></typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LinkNode<T>
    {
        #region 属性
        /// <summary>数值</summary>
        public T Value { get; set; }

        private LinkNode<T> _Prev;
        /// <summary>前一个</summary>
        public LinkNode<T> Prev { get => _Prev; set => _Prev = value; }

        private LinkNode<T> _Next;
        /// <summary>下一个</summary>
        public LinkNode<T> Next { get => _Next; set => _Next = value; }
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
            //if (Prev != null) Prev.Next = Next;
            //if (Next != null) Next.Prev = Prev;
            
            while (true)
            {
                var p = _Prev;
                var n = _Next;

                // 可能别的线程已经清空

                // 尝试替换。原子锁只考虑Next，因为Prev用得很少
                if (Interlocked.CompareExchange(ref p._Next, n, this) == this)
                {
                    n._Prev = p;
                    break;
                }

                // 替换失败，等一会
                Thread.Sleep(1);
            }

            _Prev = null;
            _Next = null;
        }
        #endregion
    }
}