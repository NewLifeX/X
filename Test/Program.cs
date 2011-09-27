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
            FileSource.ReleaseFolder(typeof(DAL).Assembly, "XCode", "", false, e => { Console.WriteLine(e); return null; });

            IEnumerable ie = GetTest(0);
            foreach (Int32 item in ie)
            {
                Console.WriteLine(item);
            }

            AssemblyX asm = AssemblyX.Create(typeof(DAL).Assembly);
            Int32 n = 0;
            foreach (Type item in asm.Types)
            {
                //Console.WriteLine("{0,4} {1} {2} {3}", ++n, item.IsPublic ? " " : "P", item.IsNested ? "N" : " ", item.FullName);
                Console.WriteLine(++n);
                Console.WriteLine(item.FullName);

                TypeX tx = TypeX.Create(item);
                Console.WriteLine(tx.FullName);
                Console.WriteLine(tx.DocName);
                if (item.IsGenericType)
                {
                    MemberInfo[] mis = item.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (MemberInfo mi in mis)
                    {
                        //MemberInfoX mix = mi as MemberInfoX;
                        MemberInfoX mix = MemberInfoX.Create(mi);
                        if (mix != null) Console.WriteLine(mix.DocName);
                    }
                }
            }
            "".EqualIgnoreCase("");
            //EntityTest.Test();

            //String xml = EntityTest.Meta.DBO.Export();
            //Console.WriteLine(xml);
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