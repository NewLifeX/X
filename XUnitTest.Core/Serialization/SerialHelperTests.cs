using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

public class SerialHelperTests
{
    [Fact]
    public void Test1()
    {
        var json = """
            {
                "Debug":  "True",
                "LogLevel":  1234,
                "LogPath":  "xxx",
                "NetworkLog":  "255.255.255.255:514",
                "LogFileFormat":  "{0:yyyy_MM_dd}.log",
                "TempMoney":  56.78,
                "PluginPath":  "Plugins",
                "PluginServers": [ "http://x.newlifex.com/", "http://127.0.0.1" ],
                "Sys":  {
                    "Name":  "NewLife.Cube",
                    "Version":  "",
                    "DisplayName":  "魔方平台",
                    "Company":  "新生命开发团队",
                    "Develop":  "True",
                    "Enable":  "True",
                    "InstallTime":  "2019-12-30 21:05:09",
                    "xxx": {
                        "yyy": "zzz"
                    }
                }
            }
            """;
        var dic = json.DecodeJson();

        var cls = dic.BuildModelClass("MyModel");

        var model = """
            public class MyModel
            {
            	public String Debug { get; set; }

            	public Int32 LogLevel { get; set; }

            	public String LogPath { get; set; }

            	public String NetworkLog { get; set; }

            	public String LogFileFormat { get; set; }

            	public Double TempMoney { get; set; }

            	public String PluginPath { get; set; }

            	public String[] PluginServers { get; set; }

            	public SysModel Sys { get; set; }

            	public class SysModel
            	{
            		public String Name { get; set; }

            		public String Version { get; set; }

            		public String DisplayName { get; set; }

            		public String Company { get; set; }

            		public String Develop { get; set; }

            		public String Enable { get; set; }

            		public String InstallTime { get; set; }

            		public XxxModel Xxx { get; set; }

            		public class XxxModel
            		{
            			public String Yyy { get; set; }
            		}
            	}
            }

            """;
        Assert.Equal(model, cls);
    }
}