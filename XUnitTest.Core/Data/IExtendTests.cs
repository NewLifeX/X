using System;
using System.Collections.Generic;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Data
{
    public class IExtendTests
    {
        [Fact]
        public void ToExtend_Dictionary()
        {
            var dic = new Dictionary<String, Object>
            {
                ["aaa"] = 1234
            };

            var ext = dic.ToExtend();
            Assert.NotNull(ext);
            Assert.Equal("ExtendDictionary", ext.GetType().Name);
            Assert.Equal(1234, ext["aaa"]);

            // 引用型，可以共用
            ext["bbb"] = "xxx";
            Assert.Equal("xxx", dic["bbb"]);
        }

        [Fact]
        public void ToExtend_Interface()
        {
            var dic = new ExtendTest
            {
                ["aaa"] = 1234
            };

            var ext = dic.ToExtend();
            Assert.NotNull(ext);
            Assert.Equal(typeof(ExtendTest), ext.GetType());
            Assert.Equal(1234, ext["aaa"]);

            ext["bbb"] = "xxx";
            Assert.Equal("xxx", dic["bbb"]);
        }

        class ExtendTest : Dictionary<String, Object>, IExtend { }

        [Fact]
        public void ToDictionary_Interface()
        {
            var ext = new ExtendTest
            {
                ["aaa"] = 1234
            };

            var dic = ext.ToDictionary();
            Assert.NotNull(dic);
            Assert.Equal(typeof(ExtendTest), dic.GetType());
            Assert.Equal(1234, dic["aaa"]);

            dic["bbb"] = "xxx";
            Assert.Equal("xxx", ext["bbb"]);
        }

        [Fact]
        public void ToDictionary_ExtendDictionary()
        {
            var ext = "NewLife.Data.ExtendDictionary".GetTypeEx().CreateInstance() as IExtend;
            ext["aaa"] = 1234;

            var dic = ext.ToDictionary();
            Assert.NotNull(dic);
            Assert.Equal(typeof(Dictionary<String, Object>), dic.GetType());
            Assert.Equal(1234, dic["aaa"]);

            dic["bbb"] = "xxx";
            Assert.Equal("xxx", ext["bbb"]);
        }

        [Fact]
        public void ToDictionary_OtherDictionary()
        {
            var ext = new ExtendTest2
            {
                ["aaa"] = 1234
            };

            var dic = ext.ToDictionary();
            Assert.NotNull(dic);
            Assert.Equal(typeof(Dictionary<String, Object>), dic.GetType());
            Assert.Equal(1234, dic["aaa"]);

            // 非引用型
            dic["bbb"] = "xxx";
            //Assert.Equal("xxx", ext["bbb"]);
            Assert.False(ext.ContainsKey("bbb"));
        }

        class ExtendTest2 : Dictionary<Object, Object>, IExtend
        {
            public Object this[String item]
            {
                get => base[item];
                set => base[item] = value;
            }
        }

        [Fact]
        public void ToDictionary_RefrectItems()
        {
            var ext = new ExtendTest3
            {
                ["aaa"] = 1234
            };

            var dic = ext.ToDictionary();
            Assert.NotNull(dic);
            Assert.Equal(typeof(NullableDictionary<String, Object>), dic.GetType());
            Assert.Equal(1234, dic["aaa"]);

            // 引用型
            dic["bbb"] = "xxx";
            Assert.Equal("xxx", ext["bbb"]);
        }

        class ExtendTest3 : IExtend
        {
            public NullableDictionary<String, Object> Items { get; set; } = new NullableDictionary<String, Object>();

            public Object this[String item]
            {
                get => Items[item];
                set => Items[item] = value;
            }
        }

        //[Fact]
        //public void ToDictionary_RefrectField()
        //{
        //    var ext = new ExtendTest4
        //    {
        //        ["aaa"] = 1234
        //    };

        //    var dic = ext.ToDictionary();
        //    Assert.NotNull(dic);
        //    Assert.Equal(typeof(NullableDictionary<String, Object>), dic.GetType());
        //    Assert.Equal(1234, dic["aaa"]);

        //    // 引用型
        //    dic["bbb"] = "xxx";
        //    Assert.Equal("xxx", ext["bbb"]);
        //}

        //class ExtendTest4 : IExtend
        //{
        //    private NullableDictionary<String, Object> Items = new NullableDictionary<String, Object>();

        //    public Object this[String item]
        //    {
        //        get => Items[item];
        //        set => Items[item] = value;
        //    }
        //}

        [Fact]
        public void ToDictionary_NotSupported()
        {
            var ext = new ExtendTest5
            {
                ["aaa"] = 1234
            };

            Assert.Throws<NotSupportedException>(() => ext.ToDictionary());
        }

        class ExtendTest5 : IExtend
        {
            private readonly NullableDictionary<String, Object> _ms = new NullableDictionary<String, Object>();

            public Object this[String item]
            {
                get => _ms[item];
                set => _ms[item] = value;
            }
        }
    }
}