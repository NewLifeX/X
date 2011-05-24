using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using NewLife.CommonEntity;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Xml;
using System.Text;
using System.IO;
using System.Data;

namespace Test
{
    /// <summary>
    /// 序列化测试
    /// </summary>
    public static class SerialTest
    {
        /// <summary>
        /// 开始
        /// </summary>
        public static void Start()
        {
            //Type type = typeof(List<String>);
            //Console.WriteLine(type);
            //Type[] ts = type.GetInterfaces();
            //Console.WriteLine(ts != null);
            //foreach (Type item in ts)
            //{
            //    if (typeof(IEnumerable<>) == item.GetGenericTypeDefinition()) Console.WriteLine(item);
            //}

            //OldBinaryTest();
            BinaryTest();
            //XmlTest();
            //JsonTest();

            //foreach (ConsoleColor item in Enum.GetValues(typeof(ConsoleColor)))
            //{
            //    Console.ForegroundColor = item;
            //    Console.WriteLine("Test");
            //}
        }

        static void OldBinaryTest()
        {
            TraceStream ts = new TraceStream();
            ts.UseConsole = true;

            BinaryFormatter bf = new BinaryFormatter();

            Administrator entity = GetDemo();

            bf.Serialize(ts, entity);

            Byte[] buffer = ts.ToArray();
            Console.WriteLine(BitConverter.ToString(buffer));

            bf = new BinaryFormatter();
            ts.Position = 0;
            entity = bf.Deserialize(ts) as Admin;
            Console.WriteLine(entity != null);
        }

        /// <summary>
        /// 二进制序列化测试
        /// </summary>
        public static void BinaryTest()
        {
            BinaryWriterX writer = GetWriter<BinaryWriterX>();
            writer.Settings.DateTimeFormat = ReaderWriterSetting.DateTimeFormats.Seconds;
            //writer.Settings.IgnoreName = false;
            //writer.Settings.IgnoreType = false;
            writer.Settings.SplitComplexType = true;

            DoTest<BinaryWriterX, BinaryReaderX>(writer);
        }

        /// <summary>
        /// Xml序列化测试
        /// </summary>
        public static void XmlTest()
        {
            XmlWriterX writer = GetWriter<XmlWriterX>();
            writer.Settings.MemberAsAttribute = false;
            writer.Settings.IgnoreDefault = false;

            DoTest<XmlWriterX, XmlReaderX>(writer);

            #region 测试
            //XmlReaderSettings settings = new XmlReaderSettings();
            //settings.IgnoreWhitespace = true;
            //writer.Stream.Position = 0;
            //XmlReader xr = XmlReader.Create(writer.Stream, settings);
            //while (xr.Read())
            //{
            //    Console.WriteLine("{0}, {1}={2}", xr.NodeType, xr.Name, xr.Value);
            //}
            #endregion
        }

        /// <summary>
        /// Json测试
        /// </summary>
        public static void JsonTest()
        {
            JsonWriter writer = GetWriter<JsonWriter>();
            writer.Settings.JsEncodeUnicode = false;
            //writer.Settings.JsDateTimeFormat = false;
            writer.Settings.JsMultiline = true;
            writer.Settings.JsDateTimeFormat = JsDateTimeFormats.DotnetDateTick;


            DoTest<JsonWriter, JsonReader>(writer);
        }

        static void DoTest<TWriter, TReader>(TWriter writer)
            where TWriter : IWriter, new()
            where TReader : IReader, new()
        {
            Administrator entity = GetDemo();

            //writer.WriteObject(entity, null, null);
            writer.WriteObject(entity, entity.GetType(), null);

            Byte[] buffer = writer.ToArray();
            //Console.WriteLine(BitConverter.ToString(buffer));
            Console.WriteLine("写入完成！");
            //String str = Encoding.UTF8.GetString(buffer);
            //TraceStream ts = new TraceStream();
            //buffer = Encoding.UTF8.GetBytes(str);
            //ts.Write(buffer, 0, buffer.Length);
            //writer.Stream = ts;
            TReader reader = GetReader<TWriter, TReader>(writer);

            Object obj = null;
            reader.ReadObject(typeof(Admin), ref obj, null);
            Administrator admin = obj as Admin;
            Console.WriteLine(admin != null);
            if (admin != null)
            {
                Console.WriteLine("读取完成！");

                TWriter writer2 = GetWriter<TWriter>();
                writer2.Settings = reader.Settings;
                writer2.WriteObject(admin, null, null);

                Byte[] buffer2 = writer2.ToArray();
                Console.WriteLine("校验结果：{0}", CompareByteArray(buffer, buffer2));
            }
        }

        static T GetWriter<T>() where T : IWriter, new()
        {
            TraceStream ts = new TraceStream();

            T writer = new T();
            writer.Stream = ts;

            writer.Settings.AutoFlush = true;

            return writer;
        }

        static TReader GetReader<TWriter, TReader>(IWriter writer)
            where TWriter : IWriter, new()
            where TReader : IReader, new()
        {
            TReader reader = new TReader();
            reader.Stream = writer.Stream;
            reader.Stream.Position = 0;

            reader.Settings = writer.Settings;

            return reader;
        }

        static Administrator GetDemo()
        {
            Admin entity = new Admin();
            entity.ID = 123;
            entity.Name = "nnhy";
            entity.DisplayName = "大石头";
            entity.Logins = 65535;
            entity.LastLogin = DateTime.Now;
            entity.SSOUserID = 555;
            entity.float1 = 1.3F;
            entity.Double1 = 1.34;
            entity.Decimal1 = 12.3M;
            entity.Byte1 = 12;
            Byte[] b = { 0, 2, 35 };
            entity.Byte2 = b;
            Char[] a = { 'a', 'b', 'c' };
            entity.Char1 = a;

            entity.Color = ConsoleColor.Red;
            entity.Color2 = (ColorEnum)10;

            Department dp = new Department();
            dp.ID = 1;
            dp.Name = "部门一";

            Department dp2 = new Department();
            dp2.ID = 2;
            dp2.Name = "部门二";

            Hashtable ht = new Hashtable();
            ht.Add("cc", "哈希1");
            ht.Add("dd", dp);
            entity.Hashtable1 = ht;

            entity.Obj = dp2;

            entity.DP1 = dp;
            entity.DP2 = dp2;
            entity.DP3 = dp;

            entity.DPS = new Department[] { dp, dp2, dp };
            entity.DPS2 = new Department[][] { new Department[] { dp, dp2, dp }, new Department[] { dp2, dp2, dp } };
            //entity.DPS3 = new Department[2, 2] { { dp, dp2 }, { dp2, dp } };

            entity.LPS = new List<Department>(entity.DPS);

            entity.PPS = new Dictionary<string, Department>();
            entity.PPS.Add("aa", dp);
            entity.PPS.Add("bb", dp2);

            entity.SPS = new SortedList<string, Department>(entity.PPS);


            DataTable dt = new DataTable("TestTable");
            DataColumn dc1 = new DataColumn("Name", typeof(String));
            DataColumn dc2 = new DataColumn("Grade", typeof(Int32));

            dt.Columns.AddRange(new DataColumn[] { dc1, dc2 });

            DataRow dr1 = dt.NewRow();
            dr1[0] = "hehe"; dr1[1] = 1111;

            DataRow dr2 = dt.NewRow();
            dr2[0] = "呵呵"; dr2[1] = 2222;

            dt.Rows.Add(dr1); dt.Rows.Add(dr2);
            dt.AcceptChanges();
            //entity.Table1 = dt;

            entity.AdminB = entity;
            entity.IAdmin = entity;

            List<IAdministrator> list = new List<IAdministrator>();
            list.Add(entity);
            list.Add(entity);
            list.Add(entity);
            entity.IAdmins = list;

            return entity;
        }

        static Int32 CompareByteArray(Byte[] buffer1, Byte[] buffer2)
        {
            if (buffer1 == buffer2) return 0;

            for (int i = 0; i < buffer1.Length; i++)
            {
                if (i >= buffer2.Length) return 1;

                Int32 n = buffer1[i].CompareTo(buffer2[i]);
                if (n != 0) return n;
            }

            if (buffer1.Length == buffer2.Length) return 0;

            return -1;
        }

        [Serializable]
        abstract class AdminBase : Administrator { }

        [Serializable]
        class Admin : AdminBase
        {
            private float _float1;
            /// <summary>单精度</summary>
            public float float1
            {
                get { return _float1; }
                set { _float1 = value; }
            }

            private Double _Double1;
            /// <summary>双精度</summary>
            public Double Double1
            {
                get { return _Double1; }
                set { _Double1 = value; }
            }

            private Decimal _Decimal1;
            /// <summary>属性说明</summary>
            public Decimal Decimal1
            {
                get { return _Decimal1; }
                set { _Decimal1 = value; }
            }

            private Byte _Byte1;
            /// <summary>字节</summary>
            public Byte Byte1
            {
                get { return _Byte1; }
                set { _Byte1 = value; }
            }

            private Byte[] _Byte2;
            /// <summary>字节数组</summary>
            public Byte[] Byte2
            {
                get { return _Byte2; }
                set { _Byte2 = value; }
            }

            private Char[] _Char1;
            /// <summary>字符数组</summary>
            public Char[] Char1
            {
                get { return _Char1; }
                set { _Char1 = value; }
            }

            private ConsoleColor _Color;
            /// <summary>颜色</summary>
            public ConsoleColor Color
            {
                get { return _Color; }
                set { _Color = value; }
            }

            private ColorEnum _Color2;
            /// <summary>颜色</summary>
            public ColorEnum Color2
            {
                get { return _Color2; }
                set { _Color2 = value; }
            }

            private Department _DP1;
            /// <summary>属性说明</summary>
            public Department DP1
            {
                get { return _DP1; }
                set { _DP1 = value; }
            }

            private Department _DP2;
            /// <summary>属性说明</summary>
            public Department DP2
            {
                get { return _DP2; }
                set { _DP2 = value; }
            }

            private Department _DP3;
            /// <summary>属性说明</summary>
            public Department DP3
            {
                get { return _DP3; }
                set { _DP3 = value; }
            }

            private Department[] _DPS;
            /// <summary>属性说明</summary>
            public Department[] DPS
            {
                get { return _DPS; }
                set { _DPS = value; }
            }

            private Department[][] _DPS2;
            /// <summary>属性说明</summary>
            public Department[][] DPS2
            {
                get { return _DPS2; }
                set { _DPS2 = value; }
            }

            private Department[,] _DPS3;
            /// <summary>属性说明</summary>
            public Department[,] DPS3
            {
                get { return _DPS3; }
                set { _DPS3 = value; }
            }

            private List<Department> _LPS;
            /// <summary>属性说明</summary>
            public List<Department> LPS
            {
                get { return _LPS; }
                set { _LPS = value; }
            }

            private Dictionary<String, Department> _PPS;
            /// <summary>字典</summary>
            public Dictionary<String, Department> PPS
            {
                get { return _PPS; }
                set { _PPS = value; }
            }

            private SortedList<String, Department> _SPS;
            /// <summary>属性说明</summary>
            public SortedList<String, Department> SPS
            {
                get { return _SPS; }
                set { _SPS = value; }
            }

            private Hashtable _Hashtable1;
            /// <summary>哈希表</summary>
            public Hashtable Hashtable1
            {
                get { return _Hashtable1; }
                set { _Hashtable1 = value; }
            }

            private Object _Obj;
            /// <summary>属性说明</summary>
            public Object Obj
            {
                get { return _Obj; }
                set { _Obj = value; }
            }

            private DataTable _Table1;
            /// <summary>属性说明</summary>
            public DataTable Table1
            {
                get { return _Table1; }
                set { _Table1 = value; }
            }

            private Object[] _Objs;
            /// <summary>属性说明</summary>
            public Object[] Objs
            {
                get
                {
                    if (_Objs == null)
                        _Objs = new Object[] { 1, "string", true };
                    return _Objs;
                }
                set { _Objs = value; }
            }

            private AdminBase _AdminB;
            /// <summary>抽象类型</summary>
            public AdminBase AdminB
            {
                get { return _AdminB; }
                set { _AdminB = value; }
            }

            private IAdministrator _IAdmin;
            /// <summary>接口类型</summary>
            public IAdministrator IAdmin
            {
                get { return _IAdmin; }
                set { _IAdmin = value; }
            }

            private IList<IAdministrator> _IAdmins;
            /// <summary>接口集合</summary>
            public IList<IAdministrator> IAdmins
            {
                get { return _IAdmins; }
                set { _IAdmins = value; }
            }
        }

        [Serializable]
        class Department
        {
            private Int32 _ID;
            /// <summary>属性说明</summary>
            public Int32 ID
            {
                get { return _ID; }
                set { _ID = value; }
            }

            private String _Name;
            /// <summary>属性说明</summary>
            public String Name
            {
                get { return _Name; }
                set { _Name = value; }
            }

            public override string ToString()
            {
                return String.Format("{0}, {1}", ID, Name);
            }
        }

        [Flags]
        enum ColorEnum
        {
            red = 2,
            yellow = 4,
            green = 8
        }
    }
}