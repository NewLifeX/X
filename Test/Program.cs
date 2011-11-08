using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NewLife.CommonEntity;
using NewLife.Log;
using XCode;
using XCode.DataAccessLayer;
using XCode.Test;
using System.Data;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                    Test3();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void Test1()
        {
            Administrator admin = Administrator.Login("admin", "admin");
            Int32 id = admin.ID;
            admin = Administrator.Find(new String[] { Administrator._.ID, Administrator._.Name }, new Object[] { id, "admin" });
            admin = Administrator.Find(new String[] { Administrator._.ID, Administrator._.Name }, new String[] { id.ToString(), "admin" });

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, admin);
            ms.Position = 0;
            bf = new BinaryFormatter();
            IAdministrator admin2 = bf.Deserialize(ms) as IAdministrator;
            Console.WriteLine(admin2);
        }

        static void Test2()
        {
            DAL dal = DAL.Create("db");
            IDataTable table = null;
            foreach (IDataTable item in DAL.Create("db").Tables)
            {
                table = item;
                Console.WriteLine(item.Name);
            }
            //foreach (IDataColumn item in table.Columns)
            //{
            //    Console.WriteLine(item.Name);
            //}

            IEntityOperate op = DAL.Create("db").CreateOperate(table.Name);
            Int32 count = op.Count;
            Console.WriteLine(op.Count);

            Int32 pagesize = 10;
            String order = table.PrimaryKeys[0].Alias + " Asc";
            String order2 = table.PrimaryKeys[0].Alias + " Desc";
            String selects = table.Columns[5].Name;
            //selects = table.PrimaryKeys[0].Name;

            DAL.ShowSQL = false;
            Int32 t = 2;
            Int32 p = (Int32)((t + count) / pagesize * pagesize);

            SelectBuilder builder = new SelectBuilder();
            builder.Table = table.Name;
            builder.Key = table.PrimaryKeys[0].Name;

            Console.WriteLine();
            Console.WriteLine("选择主键：");
            selects = table.PrimaryKeys[0].Name;
            TestPageSplit("无排序", builder, op, selects, null, t);
            TestPageSplit("升序", builder, op, selects, order, t);
            TestPageSplit("降序", builder, op, selects, order2, t);

            Console.WriteLine();
            Console.WriteLine("选择非主键：");
            selects = table.Columns[5].Name;
            TestPageSplit("无排序", builder, op, selects, null, t);
            TestPageSplit("升序", builder, op, selects, order, t);
            TestPageSplit("降序", builder, op, selects, order2, t);

            Console.WriteLine();
            Console.WriteLine("选择所有：");
            selects = null;
            TestPageSplit("无排序", builder, op, selects, null, t);
            TestPageSplit("升序", builder, op, selects, order, t);
            TestPageSplit("降序", builder, op, selects, order2, t);
        }

        static void TestPageSplit(String title, SelectBuilder builder, IEntityOperate op, String selects, String order, Int32 t)
        {
            Int32 pagesize = 10;
            Int32 p = 0;
            Int32 count = op.Count;
            Boolean needTimeOne = true;

            builder.Column = selects;
            builder.OrderBy = order;

            Console.WriteLine();
            CodeTimer.ShowHeader();
            Console.WriteLine(title);

            Console.WriteLine(MSPageSplit.PageSplit(builder, 0, pagesize, false));
            CodeTimer.TimeLine("首页", t, n => op.FindAll(null, order, selects, 0, pagesize), needTimeOne);
            Console.WriteLine(MSPageSplit.PageSplit(builder, pagesize * 2, pagesize, false));
            CodeTimer.TimeLine("第三页", t, n => op.FindAll(null, order, selects, pagesize * 2, pagesize), needTimeOne);

            p = count / pagesize / 2;
            Console.WriteLine(MSPageSplit.PageSplit(builder, pagesize * p, pagesize, false));
            CodeTimer.TimeLine(p + "页", t, n => op.FindAll(null, order, selects, pagesize * p, pagesize), needTimeOne);
            Console.WriteLine(MSPageSplit.PageSplit(builder, pagesize * p + 1, pagesize, false));
            CodeTimer.TimeLine((p + 1) + "页", t, n => op.FindAll(null, order, selects, pagesize * p + 1, pagesize), needTimeOne);

            p = (Int32)((t + count) / pagesize * pagesize);
            Console.WriteLine(MSPageSplit.PageSplit(builder, p, pagesize, false));
            CodeTimer.TimeLine("尾页", t, n => op.FindAll(null, order, selects, p, pagesize), needTimeOne);
            Console.WriteLine(MSPageSplit.PageSplit(builder, p - pagesize * 2, pagesize, false));
            CodeTimer.TimeLine("倒数第三页", t, n => op.FindAll(null, order, selects, p - pagesize * 2, pagesize), needTimeOne);
        }

        static void Test3()
        {
            Int32 n = EntityTest.Meta.Count;
            Console.WriteLine(n);

            EntityList<EntityTest> list = EntityTest.FindAll();
            DataTable dt = list.ToDataTable();
            Console.WriteLine(dt);

            dt = Administrator.FindAll().ToDataTable();
        }
    }
}