using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

public class DictionaryConfigProviderTests
{
    [Fact]
    public void Xml_SaveAndLoad_Dictionary_String_String()
    {
        var prv = new XmlConfigProvider { FileName = "Config/dic_ss.xml" };

        var m1 = new DictionaryModel
        {
            Settings = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
            {
                ["token"] = "aa",
                ["mode"] = "MODE_BACKTEST",
            }
        };

        prv.Save(m1);

        var file = (prv as FileConfigProvider)!.FileName!.GetBasePath();
        var txt = File.ReadAllText(file);

        Assert.Contains("<Settings>", txt);
        Assert.Contains("<token>aa</token>", txt);
        Assert.Contains("<mode>MODE_BACKTEST</mode>", txt);
        Assert.DoesNotContain("<Keys>", txt);
        Assert.DoesNotContain("<Values>", txt);
        Assert.DoesNotContain("<Comparer", txt);
        Assert.DoesNotContain("<Count>", txt);
        Assert.DoesNotContain("<Capacity>", txt);

        Assert.Equal("""
            <?xml version="1.0" encoding="utf-8"?>
            <Root>
              <Settings>
                <token>aa</token>
                <mode>MODE_BACKTEST</mode>
              </Settings>
            </Root>
            """, txt);

        var m2 = prv.Load<DictionaryModel>();
        Assert.NotNull(m2);
        Assert.NotNull(m2!.Settings);
        Assert.Equal(2, m2.Settings.Count);
        Assert.Equal("aa", m2.Settings["token"]);
        Assert.Equal("MODE_BACKTEST", m2.Settings["mode"]);
    }

    [Fact]
    public void Xml_SaveAndLoad_Dictionary_String_Int32()
    {
        var prv = new XmlConfigProvider { FileName = "Config/dic_si.xml" };

        var m1 = new DictionaryIntModel
        {
            Settings = new Dictionary<String, Int32>
            {
                ["initCash"] = 1000000,
                ["adjust"] = 0,
            }
        };

        prv.Save(m1);

        var file = (prv as FileConfigProvider)!.FileName!.GetBasePath();
        var txt = File.ReadAllText(file);

        Assert.Contains("<Settings>", txt);
        Assert.Contains("<initCash>1000000</initCash>", txt);
        Assert.Contains("<adjust>0</adjust>", txt);
        Assert.DoesNotContain("<Keys>", txt);
        Assert.DoesNotContain("<Values>", txt);

        Assert.Equal("""
            <?xml version="1.0" encoding="utf-8"?>
            <Root>
              <Settings>
                <initCash>1000000</initCash>
                <adjust>0</adjust>
              </Settings>
            </Root>
            """, txt);

        var m2 = prv.Load<DictionaryIntModel>();
        Assert.NotNull(m2);
        Assert.NotNull(m2!.Settings);
        Assert.Equal(2, m2.Settings.Count);
        Assert.Equal(1000000, m2.Settings["initCash"]);
        Assert.Equal(0, m2.Settings["adjust"]);
    }

    private sealed class DictionaryModel
    {
        public Dictionary<String, String> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class DictionaryIntModel
    {
        public Dictionary<String, Int32> Settings { get; set; } = new();
    }
}
