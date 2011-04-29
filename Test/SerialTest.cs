using System;
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
            //BinaryTest();
            XmlTest();

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
            writer.Settings.DateTimeFormat = SerialSettings.DateTimeFormats.Seconds;
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

            DoTest<JsonWriter, JsonReader>(writer);
        }

        static void DoTest<TWriter, TReader>(TWriter writer)
            where TWriter : IWriter, new()
            where TReader : IReader, new()
        {
            Administrator entity = GetDemo();

            writer.WriteObject(entity, null, null);

            Byte[] buffer = writer.ToArray();
            //Console.WriteLine(BitConverter.ToString(buffer));

            TReader reader = GetReader<TWriter, TReader>(writer);

            Object obj = null;
            reader.ReadObject(typeof(Admin), ref obj, null);
            Administrator admin = obj as Admin;
            Console.WriteLine(admin != null);
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

            Department dp = new Department();
            dp.ID = 1;
            dp.Name = "部门一";

            Department dp2 = new Department();
            dp2.ID = 2;
            dp2.Name = "部门二";

            entity.DP1 = dp;
            entity.DP2 = dp2;
            entity.DP3 = dp;

            entity.DPS = new Department[] { dp, dp2, dp };
            //entity.DPS2 = new Department[][] { new Department[] { dp, dp2, dp }, new Department[] { dp2, dp2, dp } };
            //entity.DPS3 = new Department[2, 2] { { dp, dp2 }, { dp2, dp } };

            entity.LPS = new List<Department>(entity.DPS);

            entity.PPS = new Dictionary<string, Department>();
            entity.PPS.Add("aa", dp);
            entity.PPS.Add("bb", dp2);

            entity.SPS = new SortedList<string, Department>(entity.PPS);

            return entity;
        }

        [Serializable]
        class Admin : Administrator
        {
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

        /// <summary>   
        /// 支持XML序列化的泛型 Dictionary   
        /// </summary>   
        /// <typeparam name="TKey"></typeparam>   
        /// <typeparam name="TValue"></typeparam>   
        [XmlRoot("SerializableDictionary")]
        public class SerializableDictionary<TKey, TValue>
            : Dictionary<TKey, TValue>, IXmlSerializable
        {

            #region 构造函数
            public SerializableDictionary()
                : base()
            {
            }
            public SerializableDictionary(IDictionary<TKey, TValue> dictionary)
                : base(dictionary)
            {
            }

            public SerializableDictionary(IEqualityComparer<TKey> comparer)
                : base(comparer)
            {
            }

            public SerializableDictionary(int capacity)
                : base(capacity)
            {
            }
            public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer)
                : base(capacity, comparer)
            {
            }
            protected SerializableDictionary(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
            #endregion
            #region IXmlSerializable Members
            public XmlSchema GetSchema()
            {
                return null;
            }
            /// <summary>   
            /// 从对象的 XML 表示形式生成该对象   
            /// </summary>   
            /// <param name="reader"></param>   
            public void ReadXml(XmlReader reader)
            {
                XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
                XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
                bool wasEmpty = reader.IsEmptyElement;
                reader.Read();
                if (wasEmpty)
                    return;
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    reader.ReadStartElement("item");
                    TKey key = (TKey)keySerializer.Deserialize(reader);
                    reader.ReadEndElement();
                    reader.ReadStartElement("value");
                    TValue value = (TValue)valueSerializer.Deserialize(reader);
                    reader.ReadEndElement();
                    this.Add(key, value);
                    reader.ReadEndElement();
                    reader.MoveToContent();
                }
                //临时办法，将来解决
                if (reader.NodeType != XmlNodeType.None)
                    reader.ReadEndElement();
            }

            /**/
            /// <summary>   
            /// 将对象转换为其 XML 表示形式   
            /// </summary>   
            /// <param name="writer"></param>   
            public void WriteXml(System.Xml.XmlWriter writer)
            {
                XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
                XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
                foreach (TKey key in this.Keys)
                {
                    writer.WriteStartElement("item");
                    writer.WriteStartElement("key");
                    keySerializer.Serialize(writer, key);
                    writer.WriteEndElement();
                    writer.WriteStartElement("value");
                    TValue value = this[key];
                    valueSerializer.Serialize(writer, value);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
            }
            #endregion
        }
    }
}