using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Core.Test.Serialization;
using NewLife.Exceptions;
using NewLife.Messaging;
using NewLife.Serialization;

namespace NewLife.Core.Test.Messaging
{
    /// <summary>
    /// MessageTest 的摘要说明
    /// </summary>
    [TestClass]
    public class MessageTest
    {
        public MessageTest()
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

        void MsgTest<T>(T msg) where T : Message
        {
            //foreach (RWKinds kind in Enum.GetValues(typeof(RWKinds)))
            //{
            //    MsgTest<T>(msg, kind);
            //}

            MsgTest<T>(msg, RWKinds.Binary);
            //MsgTest<T>(msg, RWKinds.Xml);
            //MsgTest<T>(msg, RWKinds.Json);
        }

        void MsgTest<T>(T msg, RWKinds kind) where T : Message
        {
            try
            {
                var ms = msg.GetStream(kind);
                var msg2 = Message.Read(ms, kind);

                Assert.IsTrue(Obj.Compare(msg, msg2), "消息序列化前后的值不一致！");
                Assert.AreEqual(ms.Length, ms.Position, "数据没有读完！");
            }
            catch (Exception ex)
            {
                Assert.Fail(kind + " " + ex.Message + " " + ex.TargetSite);
            }
        }

        [TestMethod]
        public void NullTest()
        {
            var msg = new NullMessage();

            var sm = msg.GetStream();
            Assert.AreEqual(1, sm.Length, "Null消息序列化失败！");
            var b = sm.ReadByte();
            Assert.AreEqual((Int32)MessageKind.Null, b, "Null消息序列化失败！");

            sm.Position = 0;
            var msg2 = Message.Read<NullMessage>(sm);

            Assert.IsTrue(Obj.Compare(msg, msg2), "实体消息序列化前后的值不一致！");
        }

        [TestMethod]
        public void DataTest()
        {
            var rnd = new Random((Int32)DateTime.Now.Ticks);
            var buf = new Byte[rnd.Next(1024)];
            rnd.NextBytes(buf);

            var msg = new DataMessage();
            msg.Data = buf;
            MsgTest<DataMessage>(msg);

            // 不带数据
            msg = new DataMessage();
            MsgTest<DataMessage>(msg);

            // 0长度
            msg = new DataMessage();
            msg.Data = new Byte[0];
            MsgTest<DataMessage>(msg);
        }

        [TestMethod]
        public void StringTest()
        {
            var msg = new StringMessage();
            msg.Value = "NewLife";
            MsgTest<StringMessage>(msg);

            // 中文
            msg = new StringMessage();
            msg.Value = "新生命开发团队";
            MsgTest<StringMessage>(msg);

            // 0长度
            msg = new StringMessage();
            msg.Value = "";
            MsgTest<StringMessage>(msg);

            // 空
            msg = new StringMessage();
            msg.Value = null;
            MsgTest<StringMessage>(msg);
        }

        [TestMethod]
        public void ReadWriteEntityTest()
        {
            var msg = new EntityMessage();

            //msg.Value = Guid.NewGuid();
            msg.Value = SimpleObj.Create();
            MsgTest<EntityMessage>(msg);

            // 为空
            msg = new EntityMessage();
            msg.Value = null;
            MsgTest<EntityMessage>(msg);
        }

        [TestMethod]
        public void ExceptionTest()
        {
            var msg = new ExceptionMessage();
            msg.Value = new XException("用户异常！");
            MsgTest<ExceptionMessage>(msg);

            // 为空
            msg = new ExceptionMessage();
            msg.Value = null;
            MsgTest<ExceptionMessage>(msg);
        }

        [TestMethod]
        public void CompressionTest()
        {
            var msg = new CompressionMessage();
            msg.Message = new EntityMessage { Value = SimpleObj.Create() };
            MsgTest<CompressionMessage>(msg);

            // 为空
            msg = new CompressionMessage();
            msg.Message = null;
            MsgTest<CompressionMessage>(msg);
        }

        [TestMethod]
        public void MethodTest()
        {
            var msg = new MethodMessage();
            msg.Method = this.GetType().GetMethod("MethodTest");
            MsgTest<MethodMessage>(msg);

            // 为空
            msg = new MethodMessage();
            msg.Method = null;
            MsgTest<MethodMessage>(msg);
        }

        [TestMethod]
        public void ChannelTest()
        {
            var msg = new ChannelMessage();
            msg.Message = new EntityMessage { Value = SimpleObj.Create() };
            msg.Channel = 123;
            MsgTest<ChannelMessage>(msg);

            // 为空
            msg = new ChannelMessage();
            msg.Message = null;
            msg.Channel = 88;
            MsgTest<ChannelMessage>(msg);
        }
    }
}