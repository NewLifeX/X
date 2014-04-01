using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using NewLife.Threading;

#if !NET4
namespace System.Collections.Concurrent
{
    /// <summary>并行队列</summary>
    /// <remarks>
    /// 非常巧妙的设计，用数据段组成链表，然后数据段内部是一个数组，既满足了并发冲突要求，又满足了性能要求。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentQueue<T> : IEnumerable<T>, ICollection, IEnumerable
    {
        #region 属性
        /// <summary>链表头部</summary>
        private Segment _head;

        /// <summary>链表尾部</summary>
        private Segment _tail;

        /// <summary>数据段大小</summary>
        private const int SEGMENT_SIZE = 0x20;

        /// <summary>当前并行数</summary>
        [NonSerialized]
        internal int _Takers;

        private Int32 _Count;
        /// <summary>元素个数</summary>
        public int Count { get { return _Count; } }
        //{
        //    get
        //    {
        //        Segment segment;
        //        Segment segment2;
        //        int num;
        //        int num2;
        //        GetHeadTailPositions(out segment, out segment2, out num, out num2);
        //        if (segment == segment2) return num2 - num + 1;
        //        int num3 = SEGMENT_SIZE - num;
        //        num3 += SEGMENT_SIZE * (((int)(segment2.m_index - segment.m_index)) - 1);
        //        return num3 + (num2 + 1);
        //    }
        //}

        /// <summary>是否空</summary>
        public bool IsEmpty { get { return _Count == 0; } }
        //{
        //    get
        //    {
        //        var head = m_head;
        //        if (head.IsEmpty)
        //        {
        //            if (head.Next == null) return true;

        //            var wait = new SpinWait();
        //            while (head.IsEmpty)
        //            {
        //                if (head.Next == null) return true;
        //                wait.SpinOnce();
        //                head = m_head;
        //            }
        //        }
        //        return false;
        //    }
        //}
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ConcurrentQueue()
        {
            _head = _tail = new Segment(0, this);
        }

        /// <summary>使用集合初始化</summary>
        /// <param name="collection"></param>
        public ConcurrentQueue(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            var segment = new Segment(0, this);
            _head = segment;
            int num = 0;
            foreach (var local in collection)
            {
                segment.UnsafeAdd(local);
                num++;
                _Count++;

                // 超过大小，数组增长
                if (num >= SEGMENT_SIZE)
                {
                    segment = segment.UnsafeGrow();
                    num = 0;
                }
            }
            _tail = segment;
        }
        #endregion

        #region 核心方法
        /// <summary>进入队列</summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            var wait = new SpinWait();
            while (!_tail.TryAppend(item))
            {
                wait.SpinOnce();
            }

            Interlocked.Increment(ref _Count);
        }

        /// <summary>尝试出列</summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryDequeue(out T result)
        {
            while (!IsEmpty)
            {
                if (_head.TryRemove(out result))
                {
                    Interlocked.Decrement(ref _Count);
                    return true;
                }
            }
            result = default(T);
            return false;
        }

        /// <summary>尝试获取一个，不出列</summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryPeek(out T result)
        {
            Interlocked.Increment(ref _Takers);
            while (!IsEmpty)
            {
                if (_head.TryPeek(out result))
                {
                    Interlocked.Decrement(ref _Takers);
                    return true;
                }
            }
            result = default(T);
            Interlocked.Decrement(ref _Takers);
            return false;
        }
        #endregion

        #region 辅助方法
        /// <summary>转为数组</summary>
        /// <returns></returns>
        public T[] ToArray() { return ToList().ToArray(); }

        private List<T> ToList()
        {
            Interlocked.Increment(ref _Takers);
            var list = new List<T>();
            try
            {
                Segment seg1;
                Segment seg2;
                int num;
                int num2;
                GetHeadTailPositions(out seg1, out seg2, out num, out num2);
                if (seg1 == seg2)
                {
                    seg1.AddToList(list, num, num2);
                    return list;
                }
                seg1.AddToList(list, num, SEGMENT_SIZE - 1);
                for (var seg = seg1.Next; seg != seg2; seg = seg.Next)
                {
                    seg.AddToList(list, 0, SEGMENT_SIZE - 1);
                }
                seg2.AddToList(list, 0, num2);
            }
            finally
            {
                Interlocked.Decrement(ref _Takers);
            }
            return list;
        }

        private void GetHeadTailPositions(out Segment head, out Segment tail, out int headLow, out int tailHigh)
        {
            head = _head;
            tail = _tail;
            headLow = head.Low;
            tailHigh = tail.High;

            var wait = new SpinWait();
            while (head != _head || tail != _tail || headLow != head.Low || tailHigh != tail.High || head.m_index > tail.m_index)
            {
                wait.SpinOnce();
                head = _head;
                tail = _tail;
                headLow = head.Low;
                tailHigh = tail.High;
            }
        }
        #endregion

        #region 内部
        [StructLayout(LayoutKind.Sequential)]
        internal struct VolatileBool
        {
            public volatile bool m_value;

            public VolatileBool(bool value) { m_value = value; }
        }

        /// <summary>数据段。每个段有一个数组，用于保存数据</summary>
        private class Segment
        {
            internal volatile T[] m_array;
            private int m_high;
            private int m_low;
            internal readonly long m_index;

            private volatile Segment m_next;
            private volatile ConcurrentQueue<T> m_source;
            internal volatile VolatileBool[] m_state;

            internal Segment(long index, ConcurrentQueue<T> source)
            {
                m_array = new T[SEGMENT_SIZE];
                m_state = new VolatileBool[SEGMENT_SIZE];
                m_high = -1;
                m_index = index;
                m_source = source;
            }

            internal void AddToList(List<T> list, int start, int end)
            {
                for (int i = start; i <= end; i++)
                {
                    var wait = new SpinWait();
                    while (!m_state[i].m_value)
                    {
                        wait.SpinOnce();
                    }
                    list.Add(m_array[i]);
                }
            }

            internal void Grow()
            {
                var segment = new Segment(m_index + 1, m_source);
                m_next = segment;
                m_source._tail = m_next;
            }

            internal bool TryAppend(T value)
            {
                if (m_high >= SEGMENT_SIZE - 1) return false;
                int index = SEGMENT_SIZE;

                index = Interlocked.Increment(ref m_high);
                if (index <= SEGMENT_SIZE - 1)
                {
                    m_array[index] = value;
                    m_state[index].m_value = true;
                }
                if (index == SEGMENT_SIZE - 1) Grow();

                return index <= SEGMENT_SIZE - 1;
            }

            internal bool TryPeek(out T result)
            {
                result = default(T);
                int low = Low;
                if (low > High) return false;

                var wait = new SpinWait();
                while (!m_state[low].m_value)
                {
                    wait.SpinOnce();
                }
                result = m_array[low];
                return true;
            }

            internal bool TryRemove(out T result)
            {
                var wait = new SpinWait();
                int low = Low;
                for (int i = High; low <= i; i = High)
                {
                    if (Interlocked.CompareExchange(ref m_low, low + 1, low) == low)
                    {
                        var wait2 = new SpinWait();
                        while (!m_state[low].m_value)
                        {
                            wait2.SpinOnce();
                        }
                        result = m_array[low];
                        if (m_source._Takers <= 0) m_array[low] = default(T);
                        if (low + 1 >= SEGMENT_SIZE)
                        {
                            wait2 = new SpinWait();
                            while (m_next == null)
                            {
                                wait2.SpinOnce();
                            }
                            m_source._head = m_next;
                        }
                        return true;
                    }
                    wait.SpinOnce();
                    low = Low;
                }
                result = default(T);
                return false;
            }

            /// <summary>不安全添加</summary>
            /// <param name="value"></param>
            internal void UnsafeAdd(T value)
            {
                m_high++;
                m_array[m_high] = value;
                m_state[m_high].m_value = true;
            }

            /// <summary>不安全增长</summary>
            /// <returns></returns>
            internal Segment UnsafeGrow()
            {
                var segment = new Segment(m_index + 1, m_source);
                m_next = segment;
                return segment;
            }

            internal int High { get { return Math.Min(m_high, SEGMENT_SIZE - 1); } }

            internal bool IsEmpty { get { return Low > High; } }

            internal int Low { get { return Math.Min(m_low, SEGMENT_SIZE); } }

            internal Segment Next { get { return m_next; } }
        }
        #endregion

        #region IEnumerable 成员
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            Segment segment;
            Segment segment2;
            int num;
            int num2;
            Interlocked.Increment(ref _Takers);
            GetHeadTailPositions(out segment, out segment2, out num, out num2);

            for (var seg = _head; seg != null; seg = seg.Next)
            {
                for (int i = seg.Low; i < seg.High; i++)
                {
                    T rs = default(T);
                    if (seg.TryPeek(out rs)) yield return rs;
                }
            }

            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() { return (this as IEnumerable<T>).GetEnumerator(); }
        #endregion

        #region ICollection 成员
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null) throw new ArgumentNullException("array");
            //this.ToList().CopyTo(array, index);
        }

        bool ICollection.IsSynchronized { get { return false; } }

        object ICollection.SyncRoot { get { throw new NotSupportedException(); } }
        #endregion
    }
}
#endif