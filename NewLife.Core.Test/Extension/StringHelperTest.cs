using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NewLife.Core.Test.Extension
{
    /// <summary>StringHelperTest 的摘要说明</summary>
    [TestClass]
    public class StringHelperTest
    {
        public StringHelperTest() { }

        private TestContext testContextInstance;

        /// <summary>
        ///获取或设置测试上下文，该上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext { get { return testContextInstance; } set { testContextInstance = value; } }

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

        [TestMethod]
        public void TestLeft()
        {
            var str = "nnhy就是大石头";

            Assert.AreEqual(str.Left(5), "nnhy就", "Left出错");
            Assert.AreEqual(str.LeftBinary(5), "nnhy", "LeftBinary出错");
            Assert.AreEqual(str.LeftBinary(5, false), "nnhy就", "LeftBinary出错（strict=false）");
        }

        [TestMethod]
        public void TestRight()
        {
            var str = "nnhy就是大石头nnhy";

            Assert.AreEqual(str.Right(5), "头nnhy", "Right出错");
            Assert.AreEqual(str.RightBinary(5), "nnhy", "RightBinary出错");
            Assert.AreEqual(str.RightBinary(5, false), "头nnhy", "RightBinary出错（strict=false）");
        }

        [TestMethod]
        public void TestCut()
        {
            var str = "nnhy就是大石头";

            Assert.AreEqual(str.Cut(5, null), "nnhy就", "Cut出错");
            Assert.AreEqual(str.CutBinary(5, null), "nnhy", "CutBinary出错");
            Assert.AreEqual(str.CutBinary(5, null, false), "nnhy就", "CutBinary出错（strict=false）");
        }
    }
}
