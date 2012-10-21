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
            var writer = new BinaryWriterX();
            writer.Settings.UseObjRef = false;
            writer.Settings.EncodeInt = encodeInt;
            writer.WriteObject(obj);

            //Assert.AreNotEqual(writer.Stream.Length, 0);

            var bts1 = writer.Stream.ReadBytes();
            var bts2 = obj.GetStream(writer.Settings).ReadBytes();
            Assert.AreEqual(bts1.CompareTo(bts2), 0);
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
    }
}
