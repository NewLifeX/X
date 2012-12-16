using System;
using System.Text;

namespace NewLife.Collections
{
    /// <summary>三个一组</summary>
    [Serializable]
    public class Triplet
    {
        /// <summary>第一个</summary>
        public object First;

        /// <summary>第二个</summary>
        public object Second;

        /// <summary>第三个</summary>
        public object Third;

        /// <summary>初始化</summary>
        public Triplet()
        {
        }

        /// <summary>初始化</summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Triplet(object x, object y)
        {
            First = x;
            Second = y;
        }

        /// <summary>初始化</summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Triplet(object x, object y, object z)
        {
            First = x;
            Second = y;
            Third = z;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            //var builder = new StringBuilder();
            //builder.Append('[');
            //if (First != null) builder.Append(First.ToString());
            //builder.Append(", ");
            //if (Second != null) builder.Append(Second.ToString());
            //builder.Append(", ");
            //if (Third != null) builder.Append(Third.ToString());
            //builder.Append(']');
            //return builder.ToString();

            return String.Format("[{0}, {1}, {2}]", First, Second, Third);
        }
    }

    /// <summary>泛型三个一组</summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    /// <typeparam name="TThird"></typeparam>
    [Serializable]
    public class Triplet<TFirst, TSecond, TThird>
    {
        /// <summary>第一个</summary>
        public TFirst First;

        /// <summary>第二个</summary>
        public TSecond Second;

        /// <summary>第三个</summary>
        public TThird Third;

        /// <summary>初始化</summary>
        public Triplet() { }
        /// <summary>初始化</summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Triplet(TFirst x, TSecond y)
        {
            First = x;
            Second = y;
        }

        /// <summary>初始化</summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Triplet(TFirst x, TSecond y, TThird z)
        {
            First = x;
            Second = y;
            Third = z;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            //var builder = new StringBuilder();
            //builder.Append('[');
            //if (First != null) builder.Append(First.ToString());
            //builder.Append(", ");
            //if (Second != null) builder.Append(Second.ToString());
            //builder.Append(", ");
            //if (Third != null) builder.Append(Third.ToString());
            //builder.Append(']');
            //return builder.ToString();

            return String.Format("[{0}, {1}, {2}]", First, Second, Third);
        }
    }
}