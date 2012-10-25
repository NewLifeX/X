using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Serialization;
using System.IO;

namespace NewLife.Core.Test.Serialization
{
    /// <summary>
    /// BinaryWriterTest 的摘要说明
    /// </summary>
    [TestClass]
    public class BinaryTest
    {
        public BinaryTest()
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

        void TestWriter(Obj obj, Boolean encodeInt)
        {
            try
            {
                // 二进制序列化写入
                var writer = new BinaryWriterX();
                writer.Settings.UseObjRef = false;
                writer.Settings.EncodeInt = encodeInt;
                writer.WriteObject(obj);

                var bts1 = writer.Stream.ReadBytes();

                // 对象本应有的数据流
                var bts2 = obj.GetStream(writer.Settings).ReadBytes();

                var n = bts1.CompareTo(bts2);
                Assert.AreEqual(0, n, "二进制写入器得到的数据与标准不符！");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + " " + ex.TargetSite);
            }
        }

        [TestMethod]
        public void TestWriter()
        {
            var obj = SimpleObj.Create();
            TestWriter(obj, false);
            TestWriter(obj, true);
        }

        [TestMethod]
        public void TestWriteArray()
        {
            var obj = new ArrayObj();
            TestWriter(obj, false);
            TestWriter(obj, true);
        }

        [TestMethod]
        public void TestWriteList()
        {
            var obj = new ListObj();
            TestWriter(obj, false);
            TestWriter(obj, true);
        }

        [TestMethod]
        public void TestWriteDictionary()
        {
            var obj = new DictionaryObj();
            TestWriter(obj, false);
            TestWriter(obj, true);
        }

        [TestMethod]
        public void TestExtend()
        {
            try
            {
                var obj = ExtendObj.Create();
                for (int i = 0; i < 100; i++)
                {
                    TestWriter(obj, false);
                    TestWriter(obj, true);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + " " + ex.TargetSite);
            }
        }

        void TestReader(Obj obj, Boolean encodeInt)
        {
            var reader = new BinaryReaderX();
            reader.Settings.UseObjRef = false;
            reader.Settings.EncodeInt = encodeInt;

            // 获取对象的数据流，作为二进制读取器的数据源
            reader.Stream = obj.GetStream(reader.Settings);
            // 读取一个跟原始对象类型一致的对象
            var obj2 = reader.ReadObject(obj.GetType());

            Assert.IsNotNull(obj2, "二进制读取器无法读取标准数据！");

            var b = obj.CompareTo(obj2 as Obj);
            Assert.IsTrue(b, "序列化后对象不一致！");
        }

        [TestMethod]
        public void TestReader()
        {
            var obj = SimpleObj.Create();
            TestReader(obj, false);
            TestReader(obj, true);
        }

        [TestMethod]
        public void TestReadArray()
        {
            var obj = new ArrayObj();
            TestReader(obj, false);
            TestReader(obj, true);
        }

        [TestMethod]
        public void TestReadList()
        {
            var obj = new ListObj();
            TestReader(obj, false);
            TestReader(obj, true);
        }

        [TestMethod]
        public void TestReadDictionary()
        {
            var obj = new DictionaryObj();
            TestReader(obj, false);
            TestReader(obj, true);
        }
    }
}
