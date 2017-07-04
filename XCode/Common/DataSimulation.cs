using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Security;

namespace XCode.Common
{
    public class DataSimulation<T> : DataSimulation where T : Entity<T>, new()
    {
        /// <summary>实例化</summary>
        public DataSimulation() { Factory = Entity<T>.Meta.Factory; }
    }

    /// <summary>数据模拟</summary>
    public class DataSimulation
    {
        #region 属性
        /// <summary>实体工厂</summary>
        public IEntityOperate Factory { get; set; }

        /// <summary>事务提交的批大小</summary>
        public Int32 BatchSize { get; set; } = 1000;

        /// <summary>并发线程数</summary>
        public Int32 Threads { get; set; } = 1;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public DataSimulation()
        {
            //Threads = Environment.ProcessorCount * 3 / 4;
            //if (Threads < 1) Threads = 1;
        }
        #endregion


        #region 方法
        public void Run(Int32 count)
        {
            var fact = Factory;
            // 关闭SQL日志
            //XCode.Setting.Current.ShowSQL = false;
            //fact.Session.Dal.Session.ShowSQL = false;
            fact.Session.Dal.Db.ShowSQL = false;

            // 预热数据表
            WriteLog("{0} 已有数据：{1:n0}", fact.TableName, fact.Count);

            // 准备数据
            var list = new List<IEntity>();
            WriteLog("正在准备数据：");
            var cpu = Threads;
            Parallel.For(0, cpu, n =>
            {
                var k = 0;
                for (int i = n; i < count; i += cpu, k++)
                {
                    if (k % BatchSize == 0) Console.Write(".");

                    var e = fact.Create();
                    foreach (var item in fact.Fields)
                    {
                        if (item.IsIdentity) continue;

                        if (item.Type == typeof(Int32))
                            e.SetItem(item.Name, Rand.Next());
                        else if (item.Type == typeof(String))
                            e.SetItem(item.Name, Rand.NextString(8));
                        else if (item.Type == typeof(DateTime))
                            e.SetItem(item.Name, DateTime.Now.AddSeconds(Rand.Next(-10000, 10000)));
                    }
                    lock (list)
                    {
                        list.Add(e);
                    }
                }
            });
            Console.WriteLine();
            WriteLog("数据准备完毕！");

            var sw = new Stopwatch();
            sw.Start();

            WriteLog("正在准备写入：");
            Parallel.For(0, cpu, n =>
            {
                var k = 0;
                EntityTransaction tr = null;
                for (int i = n; i < list.Count; i += cpu, k++)
                {
                    if (k % BatchSize == 0)
                    {
                        Console.Write(".");
                        if (tr != null) tr.Commit();

                        tr = fact.CreateTrans();
                    }

                    //list[i].SaveWithoutValid();
                    list[i].Insert();
                }
                if (tr != null) tr.Commit();
            });

            sw.Stop();
            WriteLog("数据写入完毕！");
            var ms = sw.ElapsedMilliseconds;
            WriteLog("{2}插入{3:n0}行数据，耗时：{0:n0}ms 速度：{1:n0}tps", ms, list.Count * 1000L / ms, fact.Session.Dal.DbType, list.Count);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}