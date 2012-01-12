using System;
using System.Diagnostics;

namespace NewLife.Serialization.Json
{
#if DEBUG
    public class SimpleJsonUtilTest
    {
        public static void Main(string[] args)
        {
            SimpleJsonUtil _ = new SimpleJsonUtil();
            ReadTest(_);
            WriteTest(_);
        }

        private static void ReadTest(SimpleJsonUtil _)
        {
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
            assert(json.Type == SimpleJsonType.Object);
            assert(json["key1"].Get<int>() == 1);
            assert(json["key2"].Get<bool>() == false);
            assert(json["key3"].Get<bool>() == true);

            SimpleJson list = json["key4"];
            assert(list.Type == SimpleJsonType.Array);
            assert(list[0].Get<float>() == 0.123f);
            assert(list[1].Get<int>() == 123);
            assert(list[2].Get<float>() == .123f);
            assert(list[3].IsUndefined);

            assert(list[4].IsUndefined);

            string str = json["key5"].Get<string>();
            assert(str == @"""""测试
123");
            list = json["key6"];
            assert(list.Count == 3); // js中[,,,]会返回长度为3的数组
            assert(list[0].IsUndefined);
            assert(list[1].IsUndefined);
            assert(list[2].IsUndefined);

            assert(json.Get<int>("key4[1]") == 123);
            assert(json.Get<int>("key7.a.0") == 0);
            assert(json.Get<int>("key7.a.0") == json.Get<int>("key7.a[0]"));

            Console.WriteLine(string.Join(" , ", json.Keys));
        }

        private static void WriteTest(SimpleJsonUtil _)
        {
            assert(_.To(_.String(@" ' "" \ , : ")) == @""" ' \"" \\ , : """);
            assert(_.To(_.Array(1, 2, _.Undefined(), 3, _.Undefined())) == "[1,2,null,3]");
            assert(_.To(_.Boolean(false)) == "false");
            assert(_.To(_.Boolean(true)) == "true");
            assert(_.To(_.Null()) == "null");
            assert(_.To(_.Number(100)) == "100");
            assert(_.To(_.Number(0.123f)) == "0.123");
            assert(_.To(_.Object(
                "foo", "bar",
                "hello", 111,
                "world", _.Undefined()
            )) == @"{""foo"":""bar"",""hello"":111,""world"":null}");
            assert(_.To(_.Undefined()) == "");

            SimpleJson v = _.Value(DateTime.Now);
            assert(v.Type == SimpleJsonType.Unknown);
            assert(_.To(v) == "");

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

        public static void assert(bool b)
        {
            Debug.Assert(b);
        }
    }
#endif
}