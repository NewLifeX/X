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

            TestByNumber();

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
                History.Meta.ConnName = connName;

                // 每库建立4张表。这一步不是必须的，首次读写数据时也会创建
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
    }
}