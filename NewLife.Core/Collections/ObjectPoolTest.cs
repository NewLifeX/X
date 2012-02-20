using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;
using System.Collections;
using NewLife.Reflection;

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
            var pool3 = new ObjectPool<T>();
            pool3.Stock = new LockStack<T>();

            Int32 max = 10000;
            pool1.Max = pool2.Max = pool3.Max = max;

            // 各准备对象
            Console.WriteLine("准备对象池：{0}", max);
            for (int i = 0; i < max; i++)
            {
                var e = new T();
                pool1.Push(e);
                pool2.Push(e);
                pool3.Push(e);
            }

            Int32 times = 10000000;

            // 先借后还，各一半
            //Action<String, ObjectPool<T>> test = (name, pool) =>
            //{
            //    var list = new List<T>(max);
            //    Boolean isPop = true;
            //    CodeTimer.TimeLine(name, times, index =>
            //    {
            //        if (isPop && pool.StockCount <= 0)
            //            isPop = false;
            //        else if (!isPop && list.Count <= 0)
            //            isPop = true;

            //        if (isPop)
            //            list.Add(pool.Pop());
            //        else
            //        {
            //            var p = list.Count - 1;
            //            var e = list[list.Count - 1];
            //            list.RemoveAt(p);
            //            pool.Push(e);
            //        }
            //    });
            //    // 清账
            //    foreach (var item in list) pool.Push(item);
            //};
            Test("SafeStack", pool1, max, times);
            Test("InterlockedStack", pool2, max, times);
            Test("LockStack", pool3, max, times);

            var rnd = new Random((Int32)DateTime.Now.Ticks);
            // 准备一个随机序列
            var rs = new Boolean[100];
            for (int i = 0; i < rs.Length; i++)
            {
                rs[i] = rnd.Next(0, 2) == 0;
            }

            //Action<String, ObjectPool<T>> test2 = (name, pool) =>
            //{
            //    var list = new List<T>(max);
            //    Int32 pcount1 = 0;
            //    Int32 pcount2 = 0;
            //    var ri = 0;
            //    CodeTimer.TimeLine(name, times, index =>
            //    {
            //        if (ri >= rs.Length) ri = 0;
            //        if (list.Count < 1 || list.Count < max && rs[ri++])
            //        {
            //            list.Add(pool.Pop());
            //            pcount1++;
            //        }
            //        else
            //        {
            //            var p = list.Count - 1;
            //            var e = list[list.Count - 1];
            //            list.RemoveAt(p);
            //            pool.Push(e);
            //            pcount2++;
            //        }
            //    });
            //    foreach (var item in list) pool.Push(item);

            //    Console.WriteLine("{2} 借：{0,8:n0} 还：{1,8:n0}", pcount1, pcount2, name);
            //};
            Console.WriteLine();
            Test2("SafeStack 随机", pool1, max, times, rs);
            Test2("Interlocked 随机", pool2, max, times, rs);
            Test2("LockStack 随机", pool3, max, times, rs);

            //Console.WriteLine("SafeStack  随机借：{0,8:n0} 还：{1,8:n0}", pcount1, pcount2);
            //Console.WriteLine("Interlocked随机借：{0,8:n0} 还：{1,8:n0}", pcount3, pcount4);
        }

        static void Test(String name, ObjectPool<T> pool, Int32 max, Int32 times)
        {
            var list = new List<T>(max);
            Boolean isPop = true;
            CodeTimer.TimeLine(name, times, index =>
            {
                if (isPop && pool.StockCount <= 0)
                    isPop = false;
                else if (!isPop && list.Count <= 0)
                    isPop = true;

                if (isPop)
                    list.Add(pool.Pop());
                else
                {
                    var p = list.Count - 1;
                    var e = list[list.Count - 1];
                    list.RemoveAt(p);
                    pool.Push(e);
                }
            });
            // 清账
            foreach (var item in list) pool.Push(item);
        }

        static void Test2(String name, ObjectPool<T> pool, Int32 max, Int32 times, Boolean[] rs)
        {
            var list = new List<T>(max);
            Int32 pcount1 = 0;
            Int32 pcount2 = 0;
            var ri = 0;
            CodeTimer.TimeLine(name, times, index =>
            {
                if (ri >= rs.Length) ri = 0;
                if (list.Count < 1 || list.Count < max && rs[ri++])
                {
                    list.Add(pool.Pop());
                    pcount1++;
                }
                else
                {
                    var p = list.Count - 1;
                    var e = list[list.Count - 1];
                    list.RemoveAt(p);
                    pool.Push(e);
                    pcount2++;
                }
            });
            foreach (var item in list) pool.Push(item);

            Console.WriteLine("{2} 借：{0,8:n0} 还：{1,8:n0}", pcount1, pcount2, name);
        }
    }
}