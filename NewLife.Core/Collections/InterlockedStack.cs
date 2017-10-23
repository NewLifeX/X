using System;
using System.Threading;

namespace NewLife.Collections
{
    /// <summary>原子栈</summary>
    public class InterlockedStack
    {
        private class Node
        {
            public Object Object;

            public Node Next;

            public Node(Object o)
            {
                Object = o;
                Next = null;
            }
        }

        private Object _head;

        private Int32 _count;
        public Int32 Count { get => _count; }

        public InterlockedStack()
        {
            _head = null;
        }

        public void Push(Object o)
        {
            var node = new Node(o);
            Object head;
            do
            {
                head = _head;
                node.Next = (Node)head;
            }
            while (Interlocked.CompareExchange(ref _head, node, head) != head);
            Interlocked.Increment(ref _count);
        }

        public Object Pop()
        {
            Object head;
            while (true)
            {
                head = _head;
                if (head == null) break;

                Object next = ((Node)head).Next;
                if (Interlocked.CompareExchange(ref _head, next, head) == head)
                {
                    Interlocked.Decrement(ref _count);
                    return ((Node)head).Object;
                }
            }
            return null;
        }
    }
}