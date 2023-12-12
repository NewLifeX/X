using System.IO;
using NewLife;
using NewLife.IoT.Drivers;
using NewLife.IoT.Protocols;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Xml;
using Xunit;

namespace XUnitTest.Serialization;

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

    [Fact]
    public void EnumTest()
    {
        var pm = new ModbusTcpParameter
        {
            Server = "127.0.0.1:502",

            Host = 1,
            ReadCode = FunctionCodes.ReadRegister,
            WriteCode = FunctionCodes.WriteRegister,
        };

        var xml = new NewLife.Serialization.Xml
        {
            //Encoding = encoding,
            UseAttribute = false,
            UseComment = true
        };
        xml.Write(pm);

        var str = xml.GetString();
        Assert.Equal("""
                <ModbusTcpParameter>
                  <!--主机/站号-->
                  <Host>1</Host>
                  <!--读取功能码。若点位地址未指定区域，则采用该功能码-->
                  <ReadCode>ReadRegister</ReadCode>
                  <!--写入功能码。若点位地址未指定区域，则采用该功能码-->
                  <WriteCode>WriteRegister</WriteCode>
                  <!--网络超时。发起请求后等待响应的超时时间，默认3000ms-->
                  <Timeout>3000</Timeout>
                  <!--批间隔。两个点位地址小于等于该值时凑为一批，默认1-->
                  <BatchStep>1</BatchStep>
                  <!--批大小。凑批请求时，每批最多点位个数-->
                  <BatchSize>0</BatchSize>
                  <!--批延迟。相邻请求之间的延迟时间，单位毫秒-->
                  <BatchDelay>0</BatchDelay>
                  <!--地址。tcp地址如127.0.0.1:502-->
                  <Server>127.0.0.1:502</Server>
                  <!--协议标识。默认0-->
                  <ProtocolId>0</ProtocolId>
                </ModbusTcpParameter>
                """, str);

        var xml2 = new NewLife.Serialization.Xml
        {
            //Encoding = encoding,
            UseAttribute = false,
            UseComment = true,
            EnumString = false,
        };
        xml2.Write(pm);

        var str2 = xml2.GetString();
        Assert.Equal("""
                <ModbusTcpParameter>
                  <!--主机/站号-->
                  <Host>1</Host>
                  <!--读取功能码。若点位地址未指定区域，则采用该功能码-->
                  <ReadCode>3</ReadCode>
                  <!--写入功能码。若点位地址未指定区域，则采用该功能码-->
                  <WriteCode>6</WriteCode>
                  <!--网络超时。发起请求后等待响应的超时时间，默认3000ms-->
                  <Timeout>3000</Timeout>
                  <!--批间隔。两个点位地址小于等于该值时凑为一批，默认1-->
                  <BatchStep>1</BatchStep>
                  <!--批大小。凑批请求时，每批最多点位个数-->
                  <BatchSize>0</BatchSize>
                  <!--批延迟。相邻请求之间的延迟时间，单位毫秒-->
                  <BatchDelay>0</BatchDelay>
                  <!--地址。tcp地址如127.0.0.1:502-->
                  <Server>127.0.0.1:502</Server>
                  <!--协议标识。默认0-->
                  <ProtocolId>0</ProtocolId>
                </ModbusTcpParameter>
                """, str2);
    }

    [Fact]
    public void XmlParserDecode()
    {
        var str = """
                ﻿<ModbusRtuParameter>
                  <!--主机/站号-->
                  <Host>1</Host>
                  <!--读取功能码。若点位地址未指定区域，则采用该功能码-->
                  <ReadCode>ReadRegister</ReadCode>
                  <!--写入功能码。若点位地址未指定区域，则采用该功能码-->
                  <WriteCode>WriteRegister</WriteCode>
                  <!--串口-->
                  <PortName>COM1</PortName>
                  <!--主机波特率站号-->
                  <Baudrate>9600</Baudrate>
                </ModbusRtuParameter>
                """;

        //str = str.Trim().Trim((Char)0xFEFF).Trim();
        var dic = XmlParser.Decode(str);

        Assert.NotNull(dic);
        Assert.Equal(5, dic.Count);
    }

    [Fact]
    public void CommentTest()
    {
        var set = new ModbusTcpParameter
        {
            Server = "127.0.0.1:502",

            Host = 1,
            ReadCode = FunctionCodes.ReadRegister,
            WriteCode = FunctionCodes.WriteRegister,
        };

        var xml = set.ToXml(null, false, false, false);
        Assert.Equal("""
            <?xml version="1.0" encoding="utf-8"?>
            <ModbusTcpParameter>
              <Host>1</Host>
              <ReadCode>ReadRegister</ReadCode>
              <WriteCode>WriteRegister</WriteCode>
              <Timeout>3000</Timeout>
              <BatchStep>1</BatchStep>
              <BatchSize>0</BatchSize>
              <BatchDelay>0</BatchDelay>
              <Server>127.0.0.1:502</Server>
              <ProtocolId>0</ProtocolId>
            </ModbusTcpParameter>
            """, xml);

        xml = set.ToXml(null, true, false, true);
        Assert.Equal("""
            <ModbusTcpParameter>
              <!--主机/站号-->
              <Host>1</Host>
              <!--读取功能码。若点位地址未指定区域，则采用该功能码-->
              <ReadCode>ReadRegister</ReadCode>
              <!--写入功能码。若点位地址未指定区域，则采用该功能码-->
              <WriteCode>WriteRegister</WriteCode>
              <!--网络超时。发起请求后等待响应的超时时间，默认3000ms-->
              <Timeout>3000</Timeout>
              <!--批间隔。两个点位地址小于等于该值时凑为一批，默认1-->
              <BatchStep>1</BatchStep>
              <!--批大小。凑批请求时，每批最多点位个数-->
              <BatchSize>0</BatchSize>
              <!--批延迟。相邻请求之间的延迟时间，单位毫秒-->
              <BatchDelay>0</BatchDelay>
              <!--地址。tcp地址如127.0.0.1:502-->
              <Server>127.0.0.1:502</Server>
              <!--协议标识。默认0-->
              <ProtocolId>0</ProtocolId>
            </ModbusTcpParameter>
            """, xml);
    }

    [Fact]
    public void OmitXmlDeclarationTest()
    {
        var set = new ModbusTcpParameter
        {
            Server = "127.0.0.1:502",

            Host = 1,
            ReadCode = FunctionCodes.ReadRegister,
            WriteCode = FunctionCodes.WriteRegister,
        };

        var xml = set.ToXml(null, false, false, false);
        Assert.Equal("""
            <?xml version="1.0" encoding="utf-8"?>
            <ModbusTcpParameter>
              <Host>1</Host>
              <ReadCode>ReadRegister</ReadCode>
              <WriteCode>WriteRegister</WriteCode>
              <Timeout>3000</Timeout>
              <BatchStep>1</BatchStep>
              <BatchSize>0</BatchSize>
              <BatchDelay>0</BatchDelay>
              <Server>127.0.0.1:502</Server>
              <ProtocolId>0</ProtocolId>
            </ModbusTcpParameter>
            """, xml);

        xml = set.ToXml(null, false, false, true);
        Assert.Equal("""
            <ModbusTcpParameter>
              <Host>1</Host>
              <ReadCode>ReadRegister</ReadCode>
              <WriteCode>WriteRegister</WriteCode>
              <Timeout>3000</Timeout>
              <BatchStep>1</BatchStep>
              <BatchSize>0</BatchSize>
              <BatchDelay>0</BatchDelay>
              <Server>127.0.0.1:502</Server>
              <ProtocolId>0</ProtocolId>
            </ModbusTcpParameter>
            """, xml);
    }
}