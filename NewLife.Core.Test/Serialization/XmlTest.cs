using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Serialization;
using NewLife.Xml;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace NewLife.Core.Test.Serialization
{
    /// <summary>Xml测试</summary>
    [TestClass]
    public class XmlTest
    {
        public XmlTest()
        {
            //
            //TODO: 在此处添加构造函数逻辑
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///获取或设置测试上下文，该上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region 附加测试特性
        //
        // 编写测试时，可以使用以下附加特性:
        //
        // 在运行类中的第一个测试之前使用 ClassInitialize 运行代码
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // 在类中的所有测试都已运行之后使用 ClassCleanup 运行代码
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // 在运行每个测试之前，使用 TestInitialize 来运行代码
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // 在每个测试运行完之后，使用 TestCleanup 来运行代码
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        void TestWriter(Obj obj, Boolean hasNull = true)
        {
            var set = new XmlReaderWriterSettings();
            set.UseObjRef = true;
            for (int i = 0; i < 36; i++)
            {
                // 如果对象成员有空值存在，就必须使用对象引用
                if (!hasNull) set.UseObjRef = (i & 0x01) == 0;
                set.UseTypeFullName = (i >> 1 & 0x01) == 0;
                set.Encoding = (i >> 2 & 0x01) == 0 ? Encoding.Default : Encoding.UTF8;
                set.MemberAsAttribute = (i >> 3 & 0x01) == 1;
                set.SizeFormat = (i >> 4 & 0x01) == 1 ? TypeCode.Int32 : TypeCode.UInt32;
                set.IgnoreDefault = (i >> 5 & 0x01) == 1;

                TestWriter(obj, set);
            }
        }

        void TestWriter(Obj obj, XmlReaderWriterSettings set)
        {
            try
            {
                var writer = new XmlWriterX();
                writer.Settings = set;
                writer.WriteObject(obj);

                writer.Flush();

                var ms = writer.Stream;
                Assert.IsFalse(ms.Length == 0, "写入失败");

                ms.Position = 0;
                var xml = set.Encoding.GetString(ms.ReadBytes());
                //var xml2 = obj.ToXml(set.Encoding, "", "", true);
                ms.Position = 0;

                // 序列化为特性后不好比较
                if (set.MemberAsAttribute) return;
                // Address不好序列化
                //if (obj.GetType().Name.StartsWith("Extend")) return;
                // Xml太短，可能是成员为空，不好序列化
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                if (doc.DocumentElement.InnerXml.Length < 15) return;

                //var obj2 = ms.ToXmlEntity(obj.GetType(), set.Encoding);
                var obj2 = xml.ToXmlEntity(obj.GetType());

                Assert.IsNotNull(obj2, "Xml无法反序列化！");

                if (!set.UseObjRef)
                {
                    var b = obj.CompareTo(obj2 as Obj);
                    Assert.IsTrue(b, "序列化后对象不一致！");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + " " + ex.TargetSite);
            }
        }

        [TestMethod]
        public void XmlTestWriter()
        {
            var obj = SimpleObj.Create();
            TestWriter(obj, false);
        }

        [TestMethod]
        public void XmlTestWriteArray()
        {
            var obj = ArrayObj.Create();
            TestWriter(obj);

            obj.Objs = null;
            TestWriter(obj);
        }

        [TestMethod]
        public void XmlTestWriteList()
        {
            var obj = new ListObj();
            TestWriter(obj);

            obj.Objs = null;
            TestWriter(obj);
        }

        [TestMethod]
        public void XmlTestWriteDictionary()
        {
            var obj = new DictionaryObj();
            TestWriter(obj);

            obj.Objs = null;
            TestWriter(obj);
        }

        [TestMethod]
        public void XmlTestWriteExtend()
        {
            try
            {
                var obj = new ExtendObj();
                TestWriter(obj);

                for (int i = 0; i < 10; i++)
                {
                    obj = ExtendObj.Create();
                    TestWriter(obj);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + " " + ex.TargetSite);
            }
        }

        [TestMethod]
        public void XmlTestWriteAbstract()
        {
            var obj = new AbstractObj();
            TestWriter(obj);

            obj.Value = SimpleObj.Create();
            TestWriter(obj);
        }

        void TestReader(Obj obj, Boolean hasNull = true)
        {
            var set = new XmlReaderWriterSettings();
            set.UseObjRef = true;
            for (int i = 0; i < 36; i++)
            {
                // 如果对象成员有空值存在，就必须使用对象引用
                if (!hasNull) set.UseObjRef = (i & 0x01) == 0;
                set.UseTypeFullName = (i >> 1 & 0x01) == 0;
                set.Encoding = (i >> 2 & 0x01) == 0 ? Encoding.Default : Encoding.UTF8;
                set.MemberAsAttribute = (i >> 3 & 0x01) == 1;
                set.SizeFormat = (i >> 4 & 0x01) == 1 ? TypeCode.Int32 : TypeCode.UInt32;
                set.IgnoreDefault = (i >> 5 & 0x01) == 1;

                TestReader(obj, set);
            }
        }

        void TestReader(Obj obj, XmlReaderWriterSettings set)
        {
            try
            {
                var reader = new XmlReaderX();
                reader.Settings = set;

                var xml = obj.ToXml(set.Encoding, null, null, true);
                xml = xml.Trim();

                //var ms = new MemoryStream();
                //obj.ToXml(ms, set.Encoding, null, null, true);
                //ms.Position = 0;
                //var data = ms.ToArray();
                //var chars = new Char[data.Length + 1];
                //var m = 0;
                //var n = 0;
                //var f = false;

                //set.Encoding.GetDecoder().Convert(data, 0, data.Length, chars, 0, chars.Length, false, out m, out n, out f);

                // 序列化为特性后不好比较
                if (set.MemberAsAttribute) return;
                // Address不好序列化
                //if (obj.GetType().Name.StartsWith("Extend")) return;
                // Xml太短，可能是成员为空，不好序列化
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                if (doc.DocumentElement.InnerXml.Length < 15) return;

                // 获取对象的数据流，作为读取器的数据源
                obj.ToXml(reader.Stream, set.Encoding, null, null, true);
                reader.Stream.Position = 0;
                // 读取一个跟原始对象类型一致的对象
                var obj2 = reader.ReadObject(obj.GetType());

                Assert.IsNotNull(obj2, "二进制读取器无法读取标准数据！");

                var b = obj.CompareTo(obj2 as Obj);
                Assert.IsTrue(b, "序列化后对象不一致！");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + " " + ex.TargetSite);
            }
        }

        [TestMethod]
        public void XmlTestReader()
        {
            var obj = SimpleObj.Create();
            TestReader(obj, false);
        }

        [TestMethod]
        public void XmlTestReadArray()
        {
            var obj = ArrayObj.Create();
            TestReader(obj);

            obj.Objs = null;
            TestReader(obj);
        }

        [TestMethod]
        public void XmlTestReadList()
        {
            var obj = new ListObj();
            TestReader(obj);

            obj.Objs = null;
            TestReader(obj);
        }

        [TestMethod]
        public void XmlTestReadDictionary()
        {
            var obj = new DictionaryObj();
            TestReader(obj);

            obj.Objs = null;
            TestReader(obj);
        }

        [TestMethod]
        public void XmlTestReadExtend()
        {
            try
            {
                var obj = new ExtendObj();
                TestReader(obj);

                for (int i = 0; i < 10; i++)
                {
                    obj = ExtendObj.Create();
                    TestReader(obj);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + " " + ex.TargetSite);
            }
        }

        [TestMethod]
        public void XmlTestReadAbstract()
        {
            var obj = new AbstractObj();
            TestReader(obj);

            obj.Value = SimpleObj.Create();
            TestReader(obj);
        }
    }
}