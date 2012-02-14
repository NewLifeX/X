using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;

namespace NewLife.Collections
{
    /// <summary>对象池测试</summary>
    /// <typeparam name="T"></typeparam>
    public static class ObjectPoolTest<T> where T : new()
    {
        /// <summary>开始</summary>
        public static void Start()
        {
            var pool1 = new ObjectPool<T>();
            pool1.Stock = new SafeStack<T>();
            var pool2 = new ObjectPool<T>();
            pool2.Stock = new InterlockedStack<T>();

            Int32 max = 100000;
            pool1.Max = pool2.Max = max;

            // 各准备对象
            for (int i = 0; i < max; i++)
            {
                var e = new T();
                pool1.Push(e);
                pool2.Push(e);
            }

            var rnd = new Random((Int32)DateTime.Now.Ticks);
            var list = new List<T>(max);

            Int32 times = 10000000;

            // 先借后还，各一半
            Boolean isPop = true;
            CodeTimer.TimeLine("SafeStack", times, index =>
            {
                if (isPop && pool1.StockCount <= 0)
                    isPop = false;
                else if (!isPop && list.Count <= 0)
                    isPop = true;

                if (isPop)
                    list.Add(pool1.Pop());
                else
                {
                    var p = list.Count - 1;
                    var e = list[list.Count - 1];
                    list.RemoveAt(p);
                    pool1.Push(e);
                }
            });
            // 清账
            foreach (var item in list) pool1.Push(item);

            list.Clear();
            isPop = true;
            CodeTimer.TimeLine("InterlockedStack", times, index =>
            {
                if (isPop && pool2.StockCount <= 0)
                    isPop = false;
                else if (!isPop && list.Count <= 0)
                    isPop = true;

                if (isPop)
                    list.Add(pool2.Pop());
                else
                {
                    var p = list.Count - 1;
                    var e = list[list.Count - 1];
                    list.RemoveAt(p);
                    pool2.Push(e);
                }
            });
            foreach (var item in list) pool2.Push(item);

            list.Clear();
            Int32 pcount1 = 0;
            Int32 pcount2 = 0;
            CodeTimer.TimeLine("SafeStack 随机", times, index =>
            {
                if (list.Count < 1 || list.Count < max && rnd.Next(0, 2) == 0)
                {
                    list.Add(pool1.Pop());
                    pcount1++;
                }
                else
                {
                    var p = list.Count - 1;
                    var e = list[list.Count - 1];
                    list.RemoveAt(p);
                    pool1.Push(e);
                    pcount2++;
                }
            });
            foreach (var item in list) pool1.Push(item);

            list.Clear();
            Int32 pcount3 = 0;
            Int32 pcount4 = 0;
            CodeTimer.TimeLine("Interlocked 随机", times, index =>
            {
                if (list.Count < 1 || list.Count < max && rnd.Next(0, 2) == 0)
                {
                    list.Add(pool2.Pop());
                    pcount3++;
                }
                else
                {
                    var p = list.Count - 1;
                    var e = list[list.Count - 1];
                    list.RemoveAt(p);
                    pool2.Push(e);
                    pcount4++;
                }
            });
            foreach (var item in list) pool2.Push(item);

            Console.WriteLine("SafeStack  随机借：{0,8:n0} 还：{1,8:n0}", pcount1, pcount2);
            Console.WriteLine("Interlocked随机借：{0,8:n0} 还：{1,8:n0}", pcount3, pcount4);
        }
    }
}