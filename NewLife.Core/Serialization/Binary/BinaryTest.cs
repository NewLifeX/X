using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;


#if DEBUG
namespace NewLife.Serialization
{
    /// <summary>Binary测试</summary>
    public static class BinaryTest
    {
        /// <summary>开始测试</summary>
        public static void Start()
        {
            // 提升进程优先级
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;

            // 为所有Binary宿主创建实例
            Console.Clear();

            var ks = new String[] { "普通", "高级", "名值普通", "名值高级" };
            for (int i = 0; i < 4; i++)
            {
                var ext = i == 1 || i == 3;
                var unm = i == 2 || i == 3;
                var obj = Create(ext);

                Byte[] buf = null;
                Byte[] bts = null;
                Console.WriteLine("{0} 序列化", ks[i]);
                {
                    var bn = new Binary();
                    //bn.Log = XTrace.Log;
                    //bn.EnableTrace();
                    if (!ext) SetExt(bn);
                    if (unm) bn.AddHandler<BinaryPair>();
                    bn.Write(obj);

                    buf = bn.GetBytes();
                    Console.Write("{0}\t大小：{1:n0}\t", bn.GetType().Name, bn.Stream.Length);
                    bn.Stream = new MemoryStream(buf);
                    //bn.EnableTrace();
                    bn.Read(obj.GetType());
                }
                {
                    var bn = new BinaryFormatter();
                    var ms = new MemoryStream();
                    bn.Serialize(ms, obj);

                    bts = ms.ToArray();
                    Console.WriteLine("{0}\t大小：{1:n0}", bn.GetType().Name, ms.Length);
                }

                CodeTimer.ShowHeader("序列化");
                CodeTimer.TimeLine("Binary", 100000, n =>
                {
                    var bn = new Binary();
                    if (!ext) SetExt(bn);
                    if (unm) bn.AddHandler<BinaryPair>();
                    bn.Write(obj);
                });
                CodeTimer.TimeLine("BinaryFormatter", 100000, n =>
                {
                    var bn = new BinaryFormatter();
                    var ms = new MemoryStream();
                    bn.Serialize(ms, obj);
                });

                Console.WriteLine();
                CodeTimer.ShowHeader("反序列化");
                CodeTimer.TimeLine("Binary", 100000, n =>
                {
                    var bn = new Binary();
                    if (!ext) SetExt(bn);
                    if (unm) bn.AddHandler<BinaryPair>();
                    bn.Stream = new MemoryStream(buf);
                    bn.Read(obj.GetType());
                });
                CodeTimer.TimeLine("BinaryFormatter", 100000, n =>
                {
                    var bn = new BinaryFormatter();
                    var ms = new MemoryStream(bts);
                    bn.Deserialize(ms);
                });

                Console.WriteLine();
            }
        }

        static BinObject Create(Boolean ext)
        {
            var obj = new BinObject();

            obj.ID = Rand.Next();
            obj.Name = "新生命团队，学无先后达者为师";
            obj.Enable = Rand.Next(2) > 0;
            obj.Guid = Guid.NewGuid();
            obj.Time = DateTime.Now;
            obj.Data = Rand.NextBytes(16);

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

                // 自引用对象
                obj.Self = obj;

                obj.Bin = Create(false);
                obj.Bin.Name = "内部对象";
            }

            return obj;
        }

        static void SetExt(Binary bn)
        {
            var ims = bn.IgnoreMembers;
            ims.Add("Points");
            ims.Add("Items");
            ims.Add("Container");
            ims.Add("Self");
            ims.Add("Bin");
        }
    }

    [Serializable]
    class BinObject
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

        public BinObject Self { get; set; }

        public BinObject Bin { get; set; }
    }
}
#endif