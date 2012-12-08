using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Serialization.Json;

namespace NewLife.Core.Test
{
    [TestClass]
    public class SimpleJsonUtilTest
    {
        public SimpleJsonUtilTest() { }

        private TestContext testContextInstance;
        /// <summary>
        ///获取或设置测试上下文，该上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext { get { return testContextInstance; } set { testContextInstance = value; } }

        [TestMethod]
        public void ReadTest()
        {
            var _ = new SimpleJsonUtil();

            SimpleJson json = _.From(@"
{
    ""key1"":1,
    key2:false,
    key3:true,
    key4:[0.123,123,.123,],
    key5:'""""\u6D4B\u8BD5\r\n123',
    'key6':[,,,],
    key7:{a:[0,1,2,3],b:'foo bar'}
}
");
            Assert.IsTrue(json.Type == SimpleJsonType.Object);
            Assert.IsTrue(json["key1"].Get<int>() == 1);
            Assert.IsTrue(json["key2"].Get<bool>() == false);
            Assert.IsTrue(json["key3"].Get<bool>() == true);

            SimpleJson list = json["key4"];
            Assert.IsTrue(list.Type == SimpleJsonType.Array);
            Assert.IsTrue(list[0].Get<float>() == 0.123f);
            Assert.IsTrue(list[1].Get<int>() == 123);
            Assert.IsTrue(list[2].Get<float>() == .123f);
            Assert.IsTrue(list[3].IsUndefined);

            Assert.IsTrue(list[4].IsUndefined);

            string str = json["key5"].Get<string>();
            Assert.IsTrue(str == @"""""测试
123");
            list = json["key6"];
            Assert.IsTrue(list.Count == 3); // js中[,,,]会返回长度为3的数组
            Assert.IsTrue(list[0].IsUndefined);
            Assert.IsTrue(list[1].IsUndefined);
            Assert.IsTrue(list[2].IsUndefined);

            Assert.IsTrue(json.Get<int>("key4[1]") == 123);
            Assert.IsTrue(json.Get<int>("key7.a.0") == 0);
            Assert.IsTrue(json.Get<int>("key7.a.0") == json.Get<int>("key7.a[0]"));

            Console.WriteLine(string.Join(" , ", json.Keys));
        }

        [TestMethod]
        public void WriteTest()
        {
            var _ = new SimpleJsonUtil();

            Assert.IsTrue(_.To(_.String(@" ' "" \ , : ")) == @""" ' \"" \\ , : """);
            Assert.IsTrue(_.To(_.Array(1, 2, _.Undefined(), 3, _.Undefined())) == "[1,2,null,3]");
            Assert.IsTrue(_.To(_.Boolean(false)) == "false");
            Assert.IsTrue(_.To(_.Boolean(true)) == "true");
            Assert.IsTrue(_.To(_.Null()) == "null");
            Assert.IsTrue(_.To(_.Number(100)) == "100");
            Assert.IsTrue(_.To(_.Number(0.123f)) == "0.123");
            Assert.IsTrue(_.To(_.Object(
                "foo", "bar",
                "hello", 111,
                "world", _.Undefined()
            )) == @"{""foo"":""bar"",""hello"":111,""world"":null}");
            Assert.IsTrue(_.To(_.Undefined()) == "");

            SimpleJson v = _.Value(DateTime.Now);
            Assert.IsTrue(v.Type == SimpleJsonType.Unknown);
            Assert.IsTrue(_.To(v) == "");

            string jsonstr = _.To(_.Array(
                1, 2.3f, 4.5d, 6L,
                'c', "string",
                _.Object(
                    "key1", true,
                    "key2", false,
                    "key3", null,
                    "key4", _.Array(true, false, _.Undefined())
                ),
                _.Undefined()
            ));
            Console.WriteLine(jsonstr);

            string jsstrdefine = SimpleJsonUtil.JsStringDefine(jsonstr, true);
            Console.WriteLine(jsstrdefine);
        }
    }
}
