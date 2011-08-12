using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NewLife.CommonEntity;
using NewLife.Log;
using XCode.DataAccessLayer;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            ////Console.WindowWidth /= 2;
            ////Console.WindowHeight /= 2;

            //Console.WindowWidth = 80;
            //Console.WindowHeight = 20;

            ////Console.BufferWidth /= 10;
            ////Console.BufferHeight /= 10;
            //Console.BufferWidth = 160;
            //Console.BufferHeight = 500;

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
                    //ThreadPoolTest.Main2(args);
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
            //Console.ReadKey();
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
            ////FieldItem fi = Administrator.Meta.Fields[1];
            ////IField fd = Administrator.Meta.GetField("Name");
            ////EntityField fd = new EntityField(fi);
            //EntityField fd = Administrator.Meta.GetField("Name");
            //String str = fd.Equal("nnhy");
            //Console.WriteLine(str);
            //str = fd == "nnhy";
            //Console.WriteLine(str);

            DAL dal = Administrator.Meta.DBO;
            Console.Clear();
            Console.WriteLine("数据库：{0}", dal.DbType);

            String sql = Administrator.Test();
            Console.WriteLine(sql);
        }
    }
}