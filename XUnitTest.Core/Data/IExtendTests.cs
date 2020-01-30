using System;
using NewLife.Reflection;
using System.Collections.Generic;
using System.Text;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data
{
    public class IExtendTests
    {
        [Fact]
        public void ToExtend()
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
        public void ToExtend2()
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
        public void ToDictionary()
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
        public void ToDictionary2()
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
        public void ToDictionary3()
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
    }
}