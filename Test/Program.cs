using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NewLife.CommonEntity;
using NewLife.Log;
using NewLife.Model;
using XCode;
using XCode.Accessors;
using System.Text;
using System.Reflection;
using System.Data.Common;
using NewLife.Reflection;
using XCode.DataAccessLayer;
using System.Collections.Generic;
using System.Data.OracleClient;
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
                Test2();
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

            DAL.ShowSQL = false;
            Console.WriteLine();
            Console.WriteLine("开始测试分页……");
            Int32 t = 2;
            Int32 p = (Int32)((t + count) / pagesize * pagesize);

            Console.WriteLine();
            Console.WriteLine("无排序");

            CodeTimer.TimeLine("首页", t, false, n => op.FindAll(null, null, null, 0, pagesize));
            CodeTimer.TimeLine("第三页", t, false, n => op.FindAll(null, null, null, pagesize * 2, pagesize));

            p = count / pagesize / 2;
            CodeTimer.TimeLine("1000页", t, false, n => op.FindAll(null, null, null, pagesize * p, pagesize));
            CodeTimer.TimeLine("1001页", t, false, n => op.FindAll(null, null, null, pagesize * p + 1, pagesize));

            p = (Int32)((t + count) / pagesize * pagesize);
            CodeTimer.TimeLine("尾页", t, false, n => op.FindAll(null, null, null, p, pagesize));
            CodeTimer.TimeLine("倒数第三页", t, false, n => op.FindAll(null, null, null, p - pagesize * 2, pagesize));

            Console.WriteLine();
            Console.WriteLine("升序");

            CodeTimer.TimeLine("首页", t, false, n => op.FindAll(null, order, null, 0, pagesize));
            CodeTimer.TimeLine("第三页", t, false, n => op.FindAll(null, order, null, pagesize * 2, pagesize));

            p = count / pagesize / 2;
            CodeTimer.TimeLine(p + "页", t, false, n => op.FindAll(null, order, null, pagesize * p, pagesize));
            CodeTimer.TimeLine((p + 1) + "页", t, false, n => op.FindAll(null, order, null, pagesize * p + 1, pagesize));

            p = (Int32)((t + count) / pagesize * pagesize);
            CodeTimer.TimeLine("尾页", t, false, n => op.FindAll(null, order, null, p, pagesize));
            CodeTimer.TimeLine("倒数第三页", t, false, n => op.FindAll(null, order, null, p - pagesize * 2, pagesize));

            Console.WriteLine();
            Console.WriteLine("降序");

            CodeTimer.TimeLine("首页", t, false, n => op.FindAll(null, order2, null, 0, pagesize));
            CodeTimer.TimeLine("第三页", t, false, n => op.FindAll(null, order2, null, pagesize * 2, pagesize));

            p = count / pagesize / 2;
            CodeTimer.TimeLine(p + "页", t, false, n => op.FindAll(null, order2, null, pagesize * p, pagesize));
            CodeTimer.TimeLine((p + 1) + "页", t, false, n => op.FindAll(null, order2, null, pagesize * p + 1, pagesize));

            p = (Int32)((t + count) / pagesize * pagesize);
            CodeTimer.TimeLine("尾页", t, false, n => op.FindAll(null, order2, null, p, pagesize));
            CodeTimer.TimeLine("倒数第三页", t, false, n => op.FindAll(null, order2, null, p - pagesize * 2, pagesize));
        }
    }
}