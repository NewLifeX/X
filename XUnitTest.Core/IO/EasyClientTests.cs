using System;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.IO;
using Xunit;

namespace XUnitTest.IO;

public class EasyClientTests
{
    [Fact(Skip = "仅开发使用")]
    public async Task Test()
    {
        var client = new EasyClient
        {
            Server = "http://localhost:5300/io/",
        };

        var data = (ArrayPacket)"学无先后达者为师".GetBytes();
        var name = "aa.txt";
        var inf = await client.PutAsync(name, data);
        Assert.NotNull(inf);
        Assert.Equal(name, inf.Name);
        Assert.Equal(data.Length, inf.Length);
        Assert.Equal(DateTime.Today, inf.Time.Date);
        Assert.False(inf.IsDirectory);
        Assert.Equal(data, inf.Data.ReadBytes());

        inf = await client.GetAsync(name);
        Assert.NotNull(inf);
        Assert.Equal(name, inf.Name);
        Assert.Equal(data, inf.Data.ReadBytes());

        await client.PutAsync($"test/aa.log", data);
        for (var i = 0; i < 3; i++)
        {
            await client.PutAsync($"test/{i + 1}.txt", data);
        }

        var infs = await client.SearchAsync("test/*.txt", 1, 4);
        Assert.NotNull(infs);
        Assert.DoesNotContain(infs, e => e.Name == "aa.txt");
        Assert.DoesNotContain(infs, e => e.Name == "test/aa.log");
        Assert.DoesNotContain(infs, e => e.Name == "aa.log");
        Assert.Contains(infs, e => e.Name == "test/2.txt");
        Assert.Contains(infs, e => e.Name == "test/3.txt");
        Assert.DoesNotContain(infs, e => e.Name == "test/4.txt");

        var rs = await client.DeleteAsync(name);
        Assert.Equal(1, rs);

        infs = await client.Search();
        Assert.NotNull(infs);
        foreach (var item in infs)
        {
            rs = await client.DeleteAsync(item.Name);
            Assert.Equal(1, rs);
        }
    }
}