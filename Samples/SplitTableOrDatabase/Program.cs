using System;
using System.Collections.Generic;
using NewLife.Log;
using NewLife.Security;
using STOD.Entity;
using XCode;
using XCode.DataAccessLayer;

namespace SplitTableOrDatabase
{
    class Program
    {
        static void Main(String[] args)
        {
            XTrace.UseConsole();

            //TestByNumber();
            //TestByDate();
            SearchByDate();

            Console.WriteLine("OK!");
            Console.ReadLine();
        }

        static void TestByNumber()
        {
            XTrace.WriteLine("按数字分表分库");

            // 预先准备好各个库的连接字符串，动态增加，也可以在配置文件写好
            for (var i = 0; i < 4; i++)
            {
                var connName = $"HDB_{i + 1}";
                DAL.AddConnStr(connName, $"data source=numberData\\{connName}.db", null, "sqlite");

                // 每库建立4张表。这一步不是必须的，首次读写数据时也会创建
                //History.Meta.ConnName = connName;
                //for (var j = 0; j < 4; j++)
                //{
                //    History.Meta.TableName = $"History_{j + 1}";

                //    // 初始化数据表
                //    History.Meta.Session.InitData();
                //}
            }

            //!!! 写入数据测试

            // 4个库
            for (var i = 0; i < 4; i++)
            {
                var connName = $"HDB_{i + 1}";
                History.Meta.ConnName = connName;

                // 每库4张表
                for (var j = 0; j < 4; j++)
                {
                    History.Meta.TableName = $"History_{j + 1}";

                    // 插入一批数据
                    var list = new List<History>();
                    for (var n = 0; n < 1000; n++)
                    {
                        var entity = new History
                        {
                            Category = "交易",
                            Action = "转账",
                            CreateUserID = 1234,
                            CreateTime = DateTime.Now,
                            Remark = $"[{Rand.NextString(6)}]向[{Rand.NextString(6)}]转账[￥{Rand.Next(1_000_000) / 100d}]"
                        };

                        list.Add(entity);
                    }

                    // 批量插入。两种写法等价
                    //list.BatchInsert();
                    list.Insert(true);
                }
            }
        }

        static void TestByDate()
        {
            XTrace.WriteLine("按时间分表分库，每月一个库，每天一张表");

            // 预先准备好各个库的连接字符串，动态增加，也可以在配置文件写好
            var start = DateTime.Today;
            for (var i = 0; i < 12; i++)
            {
                var dt = new DateTime(start.Year, i + 1, 1);
                var connName = $"HDB_{dt:yyMM}";
                DAL.AddConnStr(connName, $"data source=timeData\\{connName}.db", null, "sqlite");
            }

            // 每月一个库，每天一张表
            start = new DateTime(start.Year, 1, 1);
            for (var i = 0; i < 365; i++)
            {
                var dt = start.AddDays(i);
                History.Meta.ConnName = $"HDB_{dt:yyMM}";
                History.Meta.TableName = $"History_{dt:yyMMdd}";

                // 插入一批数据
                var list = new List<History>();
                for (var n = 0; n < 1000; n++)
                {
                    var entity = new History
                    {
                        Category = "交易",
                        Action = "转账",
                        CreateUserID = 1234,
                        CreateTime = DateTime.Now,
                        Remark = $"[{Rand.NextString(6)}]向[{Rand.NextString(6)}]转账[￥{Rand.Next(1_000_000) / 100d}]"
                    };

                    list.Add(entity);
                }

                // 批量插入。两种写法等价
                //list.BatchInsert();
                list.Insert(true);
            }
        }

        static void SearchByDate()
        {
            // 预先准备好各个库的连接字符串，动态增加，也可以在配置文件写好
            var start = DateTime.Today;
            for (var i = 0; i < 12; i++)
            {
                var dt = new DateTime(start.Year, i + 1, 1);
                var connName = $"HDB_{dt:yyMM}";
                DAL.AddConnStr(connName, $"data source=timeData\\{connName}.db", null, "sqlite");
            }

            // 随机日期。批量操作
            start = new DateTime(start.Year, 1, 1);
            {
                var dt = start.AddDays(Rand.Next(0, 365));
                XTrace.WriteLine("查询日期：{0}", dt);

                History.Meta.ConnName = $"HDB_{dt:yyMM}";
                History.Meta.TableName = $"History_{dt:yyMMdd}";

                var list = History.FindAll();
                XTrace.WriteLine("数据：{0}", list.Count);
            }

            // 随机日期。个例操作
            start = new DateTime(start.Year, 1, 1);
            {
                var dt = start.AddDays(Rand.Next(0, 365));
                XTrace.WriteLine("查询日期：{0}", dt);
                var list = History.Meta.ProcessWithSplit(
                    $"HDB_{dt:yyMM}",
                    $"History_{dt:yyMMdd}",
                    () => History.FindAll());

                XTrace.WriteLine("数据：{0}", list.Count);
            }
        }
    }
}