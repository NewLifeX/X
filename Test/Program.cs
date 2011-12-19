using System;
using System.Diagnostics;
using NewLife.Log;
using NewLife.Compression;
using XCode.DataAccessLayer;
using NewLife.Serialization;
using System.IO;
using NewLife.Net.Protocols.DNS;

namespace Test
{
    internal class Program
    {
        private static void Main(string[] args)
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
                Test1();
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

        private static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        private static void Test1()
        {
            //ZipFile.CompressDirectory("db", "db.zip");
            //ZipFile.Extract("db_20111219162114.zip", null);

            //var eop = DAL.Create("Common").CreateOperate("Log");

            String file = "qq.bin";
            BinaryReaderX reader = new BinaryReaderX();
            reader.Settings.IsLittleEndian = false;
            reader.Settings.UseObjRef = false;
            reader.Debug = true;
            reader.Stream = File.OpenRead(file);
            reader.EnableTraceStream();

            DNS_A header = reader.ReadObject<DNS_A>();
            Console.WriteLine(header);
        }
    }
}