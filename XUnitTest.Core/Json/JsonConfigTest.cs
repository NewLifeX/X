using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using Xunit;

namespace NewLife.Json
{
    /// <summary>
    /// 测试Json配置文件功能
    /// </summary>
    public static class JsonConfigTest
    {
        /// <summary>
        /// 开始测试
        /// </summary>
        [Fact]
        public static void Start()
        {
            Console.WriteLine("测试Json配置文件功能\r\n");

            Console.WriteLine("测试-生成Json配置文件");

            var cur = TestJsonConfig.Current;
            if (File.Exists(cur.ConfigFile))
            {
                File.Delete(cur.ConfigFile);
            }

            cur = TestJsonConfig.Current;
            cur.Save();

            if (!File.Exists(cur.ConfigFile))
            {
                Console.WriteLine("失败-保存Json配置文件\r\n");
            }
            else
            {
                Console.WriteLine("成功-保存Json配置文件\r\n");
            }

            Console.WriteLine("测试-修改标题并保存");
            cur.Title = "改变标题";
            cur.Save();
            TestJsonConfig.Current = null;
            cur = TestJsonConfig.Current;
            if (cur.Title.Equals("改变标题"))
            {
                Console.WriteLine("成功-修改保存");
            }
            else if (cur.Title.Equals("测试标题"))
            {
                Console.WriteLine("失败-修改保存");
            }

            Console.WriteLine("测试-忽略属性");
            String height = File.ReadAllText(cur.ConfigFile);
            if (height.Contains("Height"))
            {
                Console.WriteLine("失败-忽略属性");
            }
            else
            {
                Console.WriteLine("成功-忽略属性");
            }
        }
    }

    [JsonConfigFile("Config\\TestJsonConfig.json")]
    class TestJsonConfig : JsonConfig<TestJsonConfig>
    {
        /// <summary>标题</summary>
        [Description("标题")]
        public String Title { get; set; } = "测试标题";

        /// <summary>宽度</summary>
        [Description("宽度")]
        public Int32 Width { get; set; } = 520;

        /// <summary>高度</summary>
        [Description("高度")]
        [XmlIgnore]
        public Int32 Height { get; set; } = 520;
    }
}