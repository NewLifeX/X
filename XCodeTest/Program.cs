using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;
using System.Collections.Specialized;
using System.Collections;

namespace XCodeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);
            while (true)
            {
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

                Console.WriteLine("OK!");
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
            Performance.Start();
        }

        static void Test2()
        {
            List<String> list = new List<String>();
            Hashtable ht = new Hashtable();
            Dictionary<String, Boolean> dic = new Dictionary<String, Boolean>();
            StringDictionary sdic = new StringDictionary();
            SortedList<String, Boolean> slist = new SortedList<string, bool>();
            ArrayList alist = new ArrayList();

            Random rnd = new Random((Int32)DateTime.Now.Ticks);
            String key = null;
            Int32 index = rnd.Next(0, 200);
            Console.WriteLine("位置：{0}", index);

            for (int i = 0; i < 200; i++)
            {
                Byte[] data = new Byte[rnd.Next(3, 12)];
                rnd.NextBytes(data);
                String str = BitConverter.ToString(data).Replace("-", null);
                str = i + str;

                list.Add(str);
                ht.Add(str, true);
                dic.Add(str, true);
                sdic.Add(str, str);
                slist.Add(str, true);
                alist.Add(str);

                if (i == index) key = str;
            }
            String[] arr = list.ToArray();

            Console.WriteLine("查找：{0}", key);

            Int32 count = 20000000;

            CodeTimer.WriteLine("Hashtable: {0}", count, delegate { ht.ContainsKey(key); });
            CodeTimer.WriteLine("Dictionary: {0}", count, delegate { dic.ContainsKey(key); });
            CodeTimer.WriteLine("StringDictionary: {0}", count, delegate { sdic.ContainsKey(key); });
            CodeTimer.WriteLine("SortedList: {0}", count, delegate { slist.ContainsKey(key); });
            CodeTimer.WriteLine("ArrayList: {0}", count, delegate { alist.Contains(key); });
            CodeTimer.WriteLine("Array: {0}", count, delegate { Array.IndexOf(arr, key); });
            CodeTimer.WriteLine("List: {0}", count, delegate { list.Contains(key); });
        }
    }
}
