using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;

#if DEBUG
namespace NewLife.Serialization
{
    public static class JsonTest
    {
        public static void Start()
        {
            // 提升进程优先级
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;

            // 为所有Json宿主创建实例
            var hosts = typeof(IJsonHost).GetAllSubclasses().Select(e => e.CreateInstance() as IJsonHost).ToArray();
            Console.Clear();

            for (int i = 0; i < 2; i++)
            {
                var obj = Create(i > 0);

                var json = "";
                Console.WriteLine("{0} Json序列化大小", i == 0 ? "普通" : "高级");
                foreach (var item in hosts)
                {
                    json = item.Write(obj, true);
                    Console.WriteLine("{0}\t大小：{1:n0}", item.GetType().Name, json.GetBytes().Length);
                    Console.WriteLine(json);

                    item.Read(json, obj.GetType());
                }
                {
                    var bn = new BinaryFormatter();
                    var ms = new MemoryStream();
                    bn.Serialize(ms, obj);
                    Console.WriteLine("{0}\t大小：{1:n0}", bn.GetType().Name, ms.Length);
                }
                {
                    var bn = new Binary();
                    bn.Write(obj);
                    Console.WriteLine("{0}\t大小：{1:n0}", bn.GetType().Name, bn.Stream.Length);
                }

                CodeTimer.ShowHeader("Json序列化性能测试");
                foreach (var item in hosts)
                {
                    CodeTimer.TimeLine(item.GetType().Name, 100000, n => { item.Write(obj); });
                }
                CodeTimer.TimeLine("BinaryFormatter", 100000, n =>
                {
                    var bn = new BinaryFormatter();
                    var ms = new MemoryStream();
                    bn.Serialize(ms, obj);
                });
                CodeTimer.TimeLine("Binary", 100000, n =>
                {
                    var bn = new Binary();
                    bn.Write(obj);
                });

                Console.WriteLine();
                CodeTimer.ShowHeader("Json反序列化性能测试");
                foreach (var item in hosts)
                {
                    CodeTimer.TimeLine(item.GetType().Name, 100000, n => { item.Read(json, obj.GetType()); });
                }
                CodeTimer.TimeLine("JsonParser", 100000, n =>
                {
                    var jp = new JsonParser(json);
                    jp.Decode();
                });
            }
        }

        static JsObject Create(Boolean ext)
        {
            var obj = new JsObject();

            obj.ID = Rand.Next();
            obj.Name = "新生命团队，学无先后达者为师";
            obj.Enable = Rand.Next(2) > 0;
            obj.Guid = Guid.NewGuid();
            obj.Time = DateTime.Now;
            //obj.Data = Rand.NextBytes(16);

            if (ext)
            {
                var n = Rand.Next(2, 10);
                obj.Points = new Double[n];
                for (int i = 0; i < n; i++)
                {
                    obj.Points[i] = (Double)Rand.Next() / 10000;
                }

                obj.Items = new List<String>();
                n = Rand.Next(2, 10);
                for (int i = 0; i < n; i++)
                {
                    obj.Items.Add(Rand.NextString(32));
                }

                obj.Container = new Dictionary<String, String>();
                n = Rand.Next(2, 10);
                for (int i = 0; i < n; i++)
                {
                    obj.Container.Add("元素" + (i + 1), Rand.NextString(32));
                }
            }

            return obj;
        }
    }

    [Serializable]
    class JsObject
    {
        public Int32 ID { get; set; }

        public String Name { get; set; }

        public Boolean Enable { get; set; }

        public Guid Guid { get; set; }

        public DateTime Time { get; set; }

        public Byte[] Data { get; set; }

        public Double[] Points { get; set; }

        public List<String> Items { get; set; }

        public Dictionary<String, String> Container { get; set; }
    }
}
#endif