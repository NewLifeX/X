﻿using System.IO;
using NewLife;
using NewLife.Log;
using NewLife.Xml;
using Xunit;

namespace XUnitTest.Serialization
{
    public class XmlTest
    {
        [Fact(DisplayName = "基础测试")]
        public void Test1()
        {
            var set = new Setting
            {
                LogLevel = LogLevel.Error,
                LogPath = "xxx",
            };

            var xml = set.ToXml();
            Assert.Contains("<Setting>", xml);
            Assert.Contains("</Setting>", xml);

            var xml2 = set.ToXml(null, false, true);
            Assert.Contains("<Setting ", xml2);

            var set2 = xml.ToXmlEntity<Setting>();

            Assert.Equal(LogLevel.Error, set2.LogLevel);
            Assert.Equal("xxx", set2.LogPath);
        }

        [Fact]
        public void StarAgentTest()
        {
            var f = "./Serialization/StarAgent.config";
            var str = File.ReadAllText(f.GetFullPath());

            var set = str.ToXmlEntity<StarSetting>();

            Assert.NotNull(set);
            Assert.NotEmpty(set.Code);
            Assert.NotNull(set.Services);
            Assert.Equal(2, set.Services.Length);

            var svc = set.Services[0];
            Assert.Equal("test", svc.Name);
            Assert.Equal("cmd", svc.FileName);

            svc = set.Services[1];
            Assert.Equal("test2", svc.Name);
            Assert.Equal("cmd", svc.FileName);
        }

        [Fact]
        public void ArrayTest()
        {
            var xml = "<FDLibBaseCfgList version=\"2.0\" xmlns=\"http://www.isapi.org/ver20/XMLSchema\">\n<FDLibBaseCfg>\n<id>1</id>\n<FDID>1D28BF6FAA5D4C92929C9ED02F0F73E4</FDID>\n<name>路人库</name>\n<faceLibType>stranger</faceLibType>\n</FDLibBaseCfg>\n<FDLibBaseCfg>\n<id>2</id>\n<FDID>B1F5A8F601B84E18BE3C22EA52033345</FDID>\n<name>内部人员库</name>\n<faceLibType>ordinary</faceLibType>\n</FDLibBaseCfg>\n</FDLibBaseCfgList>\n";
            var cfg = xml.ToXmlEntity<FDLibBaseCfgList>();

            Assert.NotNull(cfg);
            Assert.Equal("http://www.isapi.org/ver20/XMLSchema", cfg.xmlns);
            Assert.Equal("2.0", cfg.version);

            //Assert.NotNull(cfg.FDLibBaseCfgs);
            //Assert.True(cfg.FDLibBaseCfgs.Count > 0);

            var cfgs = xml.ToXmlEntity<FDLibBaseCfg[]>();
            Assert.NotNull(cfgs);
            Assert.Equal(2, cfgs.Length);
        }
    }
}