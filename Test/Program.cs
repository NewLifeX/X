using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NewLife.CommonEntity;
using NewLife.Log;
using XCode.DataAccessLayer;
using XCode.Test;
using XCode;
using NewLife.Reflection;
using System.Linq;
using System.Collections;
using NewLife;
using System.Reflection;
using NewLife.IO;

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
            Administrator admin = Administrator.Find("name", "admin");
            Console.WriteLine(admin);
            admin = Administrator.FindByKey(1);
            Console.WriteLine(admin);
            admin = Administrator.Find("displayname", "管理员");
            Console.WriteLine(admin);
        }

        static IEnumerable GetTest(Int32 max)
        {
            for (int i = 0; i < max; i++)
            {
                yield return i;
            }
        }
    }
}