using NewLife.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using NewLife.Linq;

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
        public TestContext TestContext { get { return testContextInstance; } set { testContextInstance = value; } }

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

        /// <summary>ZipFile 的测试</summary>
        [TestMethod()]
        public void ZipTest()
        {
            var fileName = "test.zip".GetFullPath();
            if (File.Exists(fileName)) File.Delete(fileName);

            var zip = new ZipFile();
            zip.Comment = "新生命开发团队";
            zip.AddFile("NewLife.Core.pdb".GetFullPath());
            zip.AddFile("NewLife.Core.Test.pdb".GetFullPath());
            zip.Write(fileName);
            zip.Dispose();

            zip = new ZipFile(fileName);
            Assert.AreEqual("新生命开发团队", zip.Comment, "注释不一样");
            Assert.AreEqual(2, zip.Count, "文件个数不一样");
            Assert.AreEqual("NewLife.Core.pdb", zip[0].FileName);
            Assert.AreEqual("NewLife.Core.Test.pdb", zip[1].FileName);
            zip.Dispose();
        }

        /// <summary>
        ///AddDirectory 的测试
        ///</summary>
        [TestMethod()]
        public void AddDirectoryTest()
        {
            DirectoryTest(false);
        }

        void DirectoryTest(Boolean useDir = false)
        {
            var fileName = "test.zip".GetFullPath();
            if (File.Exists(fileName)) File.Delete(fileName);

            var dir = "Log".GetFullPath();
            var f2 = dir.CombinePath("SubDir").CombinePath("test.txt").EnsureDirectory();
            if (!File.Exists(f2)) File.WriteAllText(f2, DateTime.Now.ToString());

            var zip = new ZipFile();
            zip.Comment = "新生命开发团队";
            zip.AddFile("NewLife.Core.pdb".GetFullPath());
            zip.AddFile("NewLife.Core.Test.pdb".GetFullPath());
            zip.AddDirectory(dir);
            zip.AddDirectory(dir, "Log");
            zip.Write(fileName);

            // 文件个数
            var count = zip.Entries.Values.Count(e => !e.IsDirectory);
            zip.UseDirectory = useDir;
            if (useDir)
            {
                count = zip.Count;
            }
            else
            {
                Assert.AreEqual(zip.Count - 3, count, "目录个数不正确");
            }

            zip.Dispose();

            zip = new ZipFile(fileName);
            zip.UseDirectory = useDir;
            Assert.AreEqual("新生命开发团队", zip.Comment, "注释不一样");
            Assert.AreEqual(count, zip.Count, "文件个数不一样");
            Assert.AreEqual("NewLife.Core.pdb", zip[0].FileName);
            Assert.AreEqual("NewLife.Core.Test.pdb", zip[1].FileName);
            zip.Dispose();
        }

        /// <summary>
        ///UseDirectory 的测试
        ///</summary>
        [TestMethod()]
        public void UseDirectoryTest()
        {
            DirectoryTest(true);
        }
    }
}
