using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;

namespace NewLife.Serialization
{
    public static class JsonTest
    {
        public static void Start()
        {
            // 为所有Json宿主创建实例
            var hosts = typeof(IJsonHost).GetAllSubclasses().Select(e => e.CreateInstance() as IJsonHost).ToArray();

            var obj = Create();
            //var json = hosts[2].Write(obj);
            Console.Clear();
            //Console.WriteLine(JsonHelper.Format(json));
            //Thread.Sleep(1000);
            //Console.Clear();

            var json = "";
            Console.WriteLine("Json序列化大小");
            foreach (var item in hosts)
            {
                json = item.Write(obj, true);
                Console.WriteLine("{0}\t{1:n0}", item.GetType().Name, json.GetBytes().Length);
                Console.WriteLine(json);

                item.Read(json, obj.GetType());
            }

            CodeTimer.ShowHeader("Json序列化性能测试");
            foreach (var item in hosts)
            {
                CodeTimer.TimeLine(item.GetType().Name, 100000, n => { item.Write(obj); });
            }

            Console.WriteLine();
            CodeTimer.ShowHeader("Json反序列化性能测试");
            foreach (var item in hosts)
            {
                CodeTimer.TimeLine(item.GetType().Name, 100000, n => { item.Read(json, obj.GetType()); });
            }
        }

        static JsObject Create()
        {
            var obj = new JsObject();

            obj.ID = Rand.Next();
            obj.Name = "新生命团队，学无先后达者为师";
            obj.Enable = Rand.Next(2) > 0;
            obj.Guid = Guid.NewGuid();
            obj.Time = DateTime.Now;
            //obj.Data = Rand.NextBytes(16);

            var n = Rand.Next(2, 10);
            obj.Points = new Double[n];
            for (int i = 0; i < n; i++)
            {
                obj.Points[i] = (Double)Rand.Next() / 10000;
            }

            obj.Items = new List<String>();
            n = Rand.Next(2, 6);
            for (int i = 0; i < n; i++)
            {
                obj.Items.Add(Rand.NextString(32));
            }

            obj.Container = new Dictionary<String, String>();
            n = Rand.Next(2, 6);
            for (int i = 0; i < n; i++)
            {
                obj.Container.Add("元素" + (i + 1), Rand.NextString(32));
            }

            return obj;
        }
    }

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