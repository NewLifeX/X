using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Core.Test.Serialization;
using NewLife.Exceptions;
using NewLife.Messaging;
using NewLife.Serialization;
using System.Collections.Generic;

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

            // 长度1024占2字节
            var n = 1;
            if (buf.Length > 127) n++;
            Assert.AreEqual(1 + n + buf.Length, msg.GetStream().Length, "Data消息序列化失败！");

            // 不带数据
            msg = new DataMessage();
            MsgTest<DataMessage>(msg);

            Assert.AreEqual(1 + 1, msg.GetStream().Length, "Data消息序列化失败！");

            // 0长度
            msg = new DataMessage();
            msg.Data = new Byte[0];
            MsgTest<DataMessage>(msg);

            Assert.AreEqual(1 + 1, msg.GetStream().Length, "Data消息序列化失败！");
        }

        [TestMethod]
        public void StringTest()
        {
            var msg = new StringMessage();
            msg.Value = "NewLife";
            MsgTest<StringMessage>(msg);

            // 长度占1字节
            Assert.AreEqual(1 + 1 + msg.Value.Length, msg.GetStream().Length, "String消息序列化失败！");

            // 中文
            msg = new StringMessage();
            msg.Value = "新生命开发团队";
            MsgTest<StringMessage>(msg);

            Assert.AreEqual(1 + 1 + msg.Value.Length * 3, msg.GetStream().Length, "String消息序列化失败！");

            // 0长度
            msg = new StringMessage();
            msg.Value = "";
            MsgTest<StringMessage>(msg);

            Assert.AreEqual(1 + 1, msg.GetStream().Length, "String消息序列化失败！");

            // 空
            msg = new StringMessage();
            msg.Value = null;
            MsgTest<StringMessage>(msg);

            Assert.AreEqual(1 + 1, msg.GetStream().Length, "String消息序列化失败！");
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

            // 1个字节的对象引用
            Assert.AreEqual(1 + 1, msg.GetStream().Length, "Entity消息序列化失败！");
        }

        [TestMethod]
        public void ReadWriteEntitiesTest()
        {
            var msg = new EntitiesMessage();

            var rnd = new Random((Int32)DateTime.Now.Ticks);
            var count = rnd.Next(1, 10);
            var list = new List<SimpleObj>();
            SimpleObj obj = null;
            for (int i = 0; i < count; i++)
            {
                // 随机对象引用
                if (rnd.Next(1, 10) > 3) obj = SimpleObj.Create();
                list.Add(obj);
            }
            msg.Values = list;
            MsgTest<EntitiesMessage>(msg);

            msg = Message.Read<EntitiesMessage>(msg.GetStream());
            Assert.AreEqual(count, msg.Values == null ? 0 : msg.Values.Count, "EntitiesMessage序列化后实体个数不想等");

            // 为空
            msg = new EntitiesMessage();
            msg.Values = null;
            MsgTest<EntitiesMessage>(msg);

            // 1个字节的对象引用
            Assert.AreEqual(1 + 1, msg.GetStream().Length, "Entities消息序列化失败！");
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

            Assert.AreEqual(1 + 1, msg.GetStream().Length, "Data消息序列化失败！");
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

            // 一个字节为对象引用
            Assert.AreEqual(1 + 1, msg.GetStream().Length, "Compression消息序列化失败！");
        }

        [TestMethod]
        public void MethodTest()
        {
            var msg = new MethodMessage();
            msg.Method = this.GetType().GetMethod("MethodTest2");
            //msg.Parameters = new Object[] { 123, 46 };
            MsgTest<MethodMessage>(msg);

            // 两个字符串之前都有一个长度，后面那个1是参数长度
            Assert.AreEqual(1 +
                1 + msg.Method.Name.Length +
                1 + msg.Method.DeclaringType.FullName.Length +
                1, msg.GetStream().Length, "Method消息序列化失败！");

            // 为空
            msg = new MethodMessage();
            msg.Method = null;
            MsgTest<MethodMessage>(msg);

            Assert.AreEqual(1 + 1 + 1 + 1, msg.GetStream().Length, "Method消息序列化失败！");
        }

        public void MethodTest2(Int32 p1, String p2) { }

        [TestMethod]
        public void ChannelTest()
        {
            var rnd = new Random((Int32)DateTime.Now.Ticks);

            var msg = new ChannelMessage();
            msg.Message = new EntityMessage { Value = SimpleObj.Create() };
            msg.Channel = (Byte)rnd.Next(0, 256);
            msg.SessionID = rnd.Next();
            MsgTest<ChannelMessage>(msg);

            var n = 1;
            if (msg.SessionID > 0x7F) n++;
            if (msg.SessionID > 0x7FF) n++;
            if (msg.SessionID > 0x7FFF) n++;
            // 压缩编码整数，当整数太大时可能占用5字节
            if (msg.SessionID > 0x7FFFF) n++;
            // 1个字节对象引用
            Assert.AreEqual(1 + 1 + n + 1 + msg.Message.GetStream().Length, msg.GetStream().Length, "Channel消息序列化失败！SessionID=" + msg.SessionID);

            // 为空
            msg = new ChannelMessage();
            msg.Message = null;
            msg.Channel = (Byte)rnd.Next(0, 256);
            msg.SessionID = rnd.Next();
            MsgTest<ChannelMessage>(msg);

            n = 1;
            if (msg.SessionID > 0x7F) n++;
            if (msg.SessionID > 0x7FF) n++;
            if (msg.SessionID > 0x7FFF) n++;
            if (msg.SessionID > 0x7FFFF) n++;
            // 1个字节对象引用
            Assert.AreEqual(1 + 1 + n + 1, msg.GetStream().Length, "Channel空消息序列化失败！SessionID=" + msg.SessionID);
        }
    }
}