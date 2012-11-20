using NewLife.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections;

namespace NewLife.Core.Test
{


    /// <summary>
    ///这是 ZipFileTest 的测试类，旨在
    ///包含所有 ZipFileTest 单元测试
    ///</summary>
    [TestClass()]
    public class ZipFileTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///获取或设置测试上下文，上下文提供
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
        //编写测试时，还可使用以下特性:
        //
        //使用 ClassInitialize 在运行类中的第一个测试前先运行代码
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //使用 ClassCleanup 在运行完类中的所有测试后再运行代码
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //使用 TestInitialize 在运行每个测试前先运行代码
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //使用 TestCleanup 在运行完每个测试后运行代码
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///ZipFile 构造函数 的测试
        ///</summary>
        [TestMethod()]
        public void ZipFileConstructorTest()
        {
            var fileName = "test.zip";
            var target = new ZipFile();
            target.Comment = "新生命开发团队";
            target.AddFile("NewLife.Core.pdb");
            target.AddFile("NewLife.Core.Test.pdb");
            target.Write(fileName);
            target.Dispose();

            target = new ZipFile(fileName);
            
        }

        /// <summary>
        ///ZipFile 构造函数 的测试
        ///</summary>
        [TestMethod()]
        public void ZipFileConstructorTest1()
        {
            Stream stream = null; // TODO: 初始化为适当的值
            Encoding encoding = null; // TODO: 初始化为适当的值
            ZipFile target = new ZipFile(stream, encoding);
            Assert.Inconclusive("TODO: 实现用来验证目标的代码");
        }

        /// <summary>
        ///ZipFile 构造函数 的测试
        ///</summary>
        [TestMethod()]
        public void ZipFileConstructorTest2()
        {
            ZipFile target = new ZipFile();
            Assert.Inconclusive("TODO: 实现用来验证目标的代码");
        }

        /// <summary>
        ///ZipFile 构造函数 的测试
        ///</summary>
        [TestMethod()]
        public void ZipFileConstructorTest3()
        {
            string fileName = string.Empty; // TODO: 初始化为适当的值
            ZipFile target = new ZipFile(fileName);
            Assert.Inconclusive("TODO: 实现用来验证目标的代码");
        }

        /// <summary>
        ///AddDirectory 的测试
        ///</summary>
        [TestMethod()]
        public void AddDirectoryTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            string dirName = string.Empty; // TODO: 初始化为适当的值
            string entryName = string.Empty; // TODO: 初始化为适当的值
            Nullable<bool> stored = new Nullable<bool>(); // TODO: 初始化为适当的值
            target.AddDirectory(dirName, entryName, stored);
            Assert.Inconclusive("无法验证不返回值的方法。");
        }

        /// <summary>
        ///AddFile 的测试
        ///</summary>
        [TestMethod()]
        public void AddFileTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            string fileName = string.Empty; // TODO: 初始化为适当的值
            string entryName = string.Empty; // TODO: 初始化为适当的值
            Nullable<bool> stored = new Nullable<bool>(); // TODO: 初始化为适当的值
            ZipEntry expected = null; // TODO: 初始化为适当的值
            ZipEntry actual;
            actual = target.AddFile(fileName, entryName, stored);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///CompressDirectory 的测试
        ///</summary>
        [TestMethod()]
        public void CompressDirectoryTest()
        {
            string dirName = string.Empty; // TODO: 初始化为适当的值
            string outputName = string.Empty; // TODO: 初始化为适当的值
            ZipFile.CompressDirectory(dirName, outputName);
            Assert.Inconclusive("无法验证不返回值的方法。");
        }

        /// <summary>
        ///CompressFile 的测试
        ///</summary>
        [TestMethod()]
        public void CompressFileTest()
        {
            string fileName = string.Empty; // TODO: 初始化为适当的值
            string outputName = string.Empty; // TODO: 初始化为适当的值
            ZipFile.CompressFile(fileName, outputName);
            Assert.Inconclusive("无法验证不返回值的方法。");
        }

        /// <summary>
        ///CreateReader 的测试
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NewLife.Core.dll")]
        public void CreateReaderTest()
        {
            // 为“Microsoft.VisualStudio.TestTools.TypesAndSymbols.Assembly”创建专用访问器失败
            Assert.Inconclusive("为“Microsoft.VisualStudio.TestTools.TypesAndSymbols.Assembly”创建专用访问器失败");
        }

        /// <summary>
        ///CreateWriter 的测试
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NewLife.Core.dll")]
        public void CreateWriterTest()
        {
            // 为“Microsoft.VisualStudio.TestTools.TypesAndSymbols.Assembly”创建专用访问器失败
            Assert.Inconclusive("为“Microsoft.VisualStudio.TestTools.TypesAndSymbols.Assembly”创建专用访问器失败");
        }

        /// <summary>
        ///DosDateTimeToFileTime 的测试
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NewLife.Core.dll")]
        public void DosDateTimeToFileTimeTest()
        {
            // 为“Microsoft.VisualStudio.TestTools.TypesAndSymbols.Assembly”创建专用访问器失败
            Assert.Inconclusive("为“Microsoft.VisualStudio.TestTools.TypesAndSymbols.Assembly”创建专用访问器失败");
        }

        /// <summary>
        ///Extract 的测试
        ///</summary>
        [TestMethod()]
        public void ExtractTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            string outputPath = string.Empty; // TODO: 初始化为适当的值
            bool overrideExisting = false; // TODO: 初始化为适当的值
            target.Extract(outputPath, overrideExisting);
            Assert.Inconclusive("无法验证不返回值的方法。");
        }

        /// <summary>
        ///Extract 的测试
        ///</summary>
        [TestMethod()]
        public void ExtractTest1()
        {
            string fileName = string.Empty; // TODO: 初始化为适当的值
            string outputPath = string.Empty; // TODO: 初始化为适当的值
            bool overrideExisting = false; // TODO: 初始化为适当的值
            ZipFile.Extract(fileName, outputPath, overrideExisting);
            Assert.Inconclusive("无法验证不返回值的方法。");
        }

        /// <summary>
        ///FileTimeToDosDateTime 的测试
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NewLife.Core.dll")]
        public void FileTimeToDosDateTimeTest()
        {
            // 为“Microsoft.VisualStudio.TestTools.TypesAndSymbols.Assembly”创建专用访问器失败
            Assert.Inconclusive("为“Microsoft.VisualStudio.TestTools.TypesAndSymbols.Assembly”创建专用访问器失败");
        }

        /// <summary>
        ///OnDispose 的测试
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NewLife.Core.dll")]
        public void OnDisposeTest()
        {
            // 为“Microsoft.VisualStudio.TestTools.TypesAndSymbols.Assembly”创建专用访问器失败
            Assert.Inconclusive("为“Microsoft.VisualStudio.TestTools.TypesAndSymbols.Assembly”创建专用访问器失败");
        }

        /// <summary>
        ///Read 的测试
        ///</summary>
        [TestMethod()]
        public void ReadTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            Stream stream = null; // TODO: 初始化为适当的值
            Nullable<bool> embedFileData = new Nullable<bool>(); // TODO: 初始化为适当的值
            target.Read(stream, embedFileData);
            Assert.Inconclusive("无法验证不返回值的方法。");
        }

        /// <summary>
        ///System.Collections.Generic.IEnumerable<NewLife.Compression.ZipEntry>.GetEnumerator 的测试
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NewLife.Core.dll")]
        public void GetEnumeratorTest()
        {
            IEnumerable<ZipEntry> target = new ZipFile(); // TODO: 初始化为适当的值
            IEnumerator<ZipEntry> expected = null; // TODO: 初始化为适当的值
            IEnumerator<ZipEntry> actual;
            actual = target.GetEnumerator();
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///System.Collections.IEnumerable.GetEnumerator 的测试
        ///</summary>
        [TestMethod()]
        [DeploymentItem("NewLife.Core.dll")]
        public void GetEnumeratorTest1()
        {
            IEnumerable target = new ZipFile(); // TODO: 初始化为适当的值
            IEnumerator expected = null; // TODO: 初始化为适当的值
            IEnumerator actual;
            actual = target.GetEnumerator();
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///ToString 的测试
        ///</summary>
        [TestMethod()]
        public void ToStringTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            string expected = string.Empty; // TODO: 初始化为适当的值
            string actual;
            actual = target.ToString();
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///Write 的测试
        ///</summary>
        [TestMethod()]
        public void WriteTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            Stream stream = null; // TODO: 初始化为适当的值
            target.Write(stream);
            Assert.Inconclusive("无法验证不返回值的方法。");
        }

        /// <summary>
        ///Write 的测试
        ///</summary>
        [TestMethod()]
        public void WriteTest1()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            string fileName = string.Empty; // TODO: 初始化为适当的值
            target.Write(fileName);
            Assert.Inconclusive("无法验证不返回值的方法。");
        }

        /// <summary>
        ///Comment 的测试
        ///</summary>
        [TestMethod()]
        public void CommentTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            string expected = string.Empty; // TODO: 初始化为适当的值
            string actual;
            target.Comment = expected;
            actual = target.Comment;
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///Count 的测试
        ///</summary>
        [TestMethod()]
        public void CountTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            int actual;
            actual = target.Count;
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///Encoding 的测试
        ///</summary>
        [TestMethod()]
        public void EncodingTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            Encoding expected = null; // TODO: 初始化为适当的值
            Encoding actual;
            target.Encoding = expected;
            actual = target.Encoding;
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///Entries 的测试
        ///</summary>
        [TestMethod()]
        public void EntriesTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            Dictionary<string, ZipEntry> actual;
            actual = target.Entries;
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///Item 的测试
        ///</summary>
        [TestMethod()]
        public void ItemTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            int index = 0; // TODO: 初始化为适当的值
            ZipEntry actual;
            actual = target[index];
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///Item 的测试
        ///</summary>
        [TestMethod()]
        public void ItemTest1()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            string fileName = string.Empty; // TODO: 初始化为适当的值
            ZipEntry actual;
            actual = target[fileName];
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///Name 的测试
        ///</summary>
        [TestMethod()]
        public void NameTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            string expected = string.Empty; // TODO: 初始化为适当的值
            string actual;
            target.Name = expected;
            actual = target.Name;
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("验证此测试方法的正确性。");
        }

        /// <summary>
        ///UseDirectory 的测试
        ///</summary>
        [TestMethod()]
        public void UseDirectoryTest()
        {
            ZipFile target = new ZipFile(); // TODO: 初始化为适当的值
            bool expected = false; // TODO: 初始化为适当的值
            bool actual;
            target.UseDirectory = expected;
            actual = target.UseDirectory;
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("验证此测试方法的正确性。");
        }
    }
}
