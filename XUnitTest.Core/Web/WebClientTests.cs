using System;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Web;

public class WebClientTests
{
    [Fact]
    public void GetLinks()
    {
        var client = new WebClientX();
        var links = client.GetLinks("http://x.newlifex.com");
        Assert.NotEmpty(links);

        var names = "System.Data.SQLite.win-x64,System.Data.SQLite.win,System.Data.SQLite_net80,System.Data.SQLite_netstandard21,System.Data.SQLite_netstandard20,System.Data.SQLite".Split(",", ";");

        links = links.Where(e => e.Name.EqualIgnoreCase(names) || e.FullName.EqualIgnoreCase(names)).ToArray();
        var link = links.OrderByDescending(e => e.Version).ThenByDescending(e => e.Time).FirstOrDefault();

        Assert.NotNull(link);
        Assert.Equal("System.Data.SQLite.win-x64", link.Name);
        Assert.True(link.Time >= "2024-05-14".ToDateTime());
        Assert.Null(link.Version);
    }
}
