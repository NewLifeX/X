using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Threading;
using Xunit;

namespace XUnitTest.Threading;

public class CronTests
{

    [Fact]
    public void GetPreviousTest()
    {
        var cron = new Cron();
        var b = cron.Parse("0 0 16 * * ? ");
        var ss = cron.GetNext(DateTime.Now);

        var s = cron.GetPrevious(DateTime.Now);
        var aa = s.ToString();
        //Assert.True(cron.IsTime(DateTime.Parse("11:00:00 10/12/2008")));
        //Assert.False(cron.IsTime(DateTime.Parse("11:01:00 10/12/2008")));
    }



    [Theory]
    [InlineData("*/2")]
    [InlineData("* * * * *")]
    [InlineData("0 * * * *")]
    [InlineData("0,1,2 * * * *")]
    [InlineData("*/2 * * * *")]
    [InlineData("5/20 * * * *")]
    [InlineData("1-4 * * * *")]
    [InlineData("1-55/3 * * * *")]
    [InlineData("1,10,20 * * * *")]
    [InlineData("* 1,10,20 * * *")]
    [InlineData("* 1-10,13,5/20 * * *")]
    public void Valid(String expression)
    {
        var cron = new Cron();
        Assert.True(cron.Parse(expression));
    }

    [Fact]
    public void is_time_second_test()
    {
        var cron = new Cron("0 * * * *");
        Assert.True(cron.IsTime(DateTime.Parse("8:00:00")));
        Assert.False(cron.IsTime(DateTime.Parse("8:00:01")));
        Assert.Single(cron.Seconds);
        Assert.Equal(0, cron.Seconds[0]);

        cron = new Cron("0-10 * * * *");
        Assert.True(cron.IsTime(DateTime.Parse("8:00:00")));
        Assert.True(cron.IsTime(DateTime.Parse("8:00:03")));
        Assert.Equal(11, cron.Seconds.Length);
        Assert.Equal(0, cron.Seconds[0]);
        Assert.Equal(10, cron.Seconds[10]);

        cron = new Cron("*/2 * * * *");
        Assert.True(cron.IsTime(DateTime.Parse("8:00:00")));
        Assert.True(cron.IsTime(DateTime.Parse("8:00:02")));
        Assert.False(cron.IsTime(DateTime.Parse("8:00:03")));
        Assert.Equal(30, cron.Seconds.Length);

        cron = new Cron("5/20 * * * *");
        Assert.True(cron.IsTime(DateTime.Parse("8:00:05")));
        Assert.True(cron.IsTime(DateTime.Parse("8:00:25")));
        Assert.False(cron.IsTime(DateTime.Parse("8:00:20")));
        Assert.Equal(3, cron.Seconds.Length);

        // 下一次，5秒后
        var dt = DateTime.Today;
        var dt2 = cron.GetNext(dt);
        Assert.Equal(dt.AddSeconds(5), dt2);

        // 后续每次间隔20秒
        dt2 = cron.GetNext(dt2);
        Assert.Equal(dt.AddSeconds(25), dt2);

        dt2 = cron.GetNext(dt2);
        Assert.Equal(dt.AddSeconds(45), dt2);
    }

    [Fact]
    public void is_time_hour_test()
    {
        var cron = new Cron("* * 3 * * *");
        Assert.True(cron.IsTime(DateTime.Parse("3:00:00")));
        Assert.Single(cron.Hours);
        Assert.Equal(3, cron.Hours[0]);

        cron = new Cron("* * 0,12 * * *");
        Assert.True(cron.IsTime(DateTime.Parse("12:00:00")));
        Assert.True(cron.IsTime(DateTime.Parse("12:00:00 pm")));
        Assert.Equal(2, cron.Hours.Length);
        Assert.Equal(0, cron.Hours[0]);
        Assert.Equal(12, cron.Hours[1]);

        cron = new Cron("0 0 0,12 * * *");

        // 下一次，零点
        var dt = DateTime.Today;
        var dt2 = cron.GetNext(dt);
        Assert.Equal(dt.AddHours(12), dt2);

        dt2 = cron.GetNext(dt2);
        Assert.Equal(dt.AddHours(24), dt2);

        dt2 = cron.GetNext(dt2);
        Assert.Equal(dt.AddHours(36), dt2);
    }

    [Fact]
    public void is_time_day_of_month_test()
    {
        var cron = new Cron("* * * 1 * *");
        Assert.True(cron.IsTime(DateTime.Parse("2010/08/01")));
        Assert.Single(cron.DaysOfMonth);
        Assert.Equal(1, cron.DaysOfMonth[0]);

        cron = new Cron("0 0 0 1 * *");

        // 下一次
        var dt = DateTime.Parse("2010/08/01");
        var dt2 = cron.GetNext(dt);
        Assert.Equal(dt.AddMonths(1), dt2);

        dt2 = cron.GetNext(dt2);
        Assert.Equal(dt.AddMonths(2), dt2);

        dt2 = cron.GetNext(dt2);
        Assert.Equal(dt.AddMonths(3), dt2);
    }

    [Fact]
    public void is_time_month_test()
    {
        var cron = new Cron("* * * * 1 *");
        Assert.True(cron.IsTime(DateTime.Parse("1/1/2008")));
        Assert.Single(cron.Months);
        Assert.Equal(1, cron.Months[0]);

        cron = new Cron("* * * * 12 *");
        Assert.False(cron.IsTime(DateTime.Parse("1/1/2008")));
        Assert.Single(cron.Months);
        Assert.Equal(12, cron.Months[0]);

        cron = new Cron("* * * * */3 *");
        Assert.True(cron.IsTime(DateTime.Parse("3/1/2008")));
        Assert.True(cron.IsTime(DateTime.Parse("6/1/2008")));
        Assert.Equal(4, cron.Months.Length);
        Assert.Equal(3, cron.Months[0]);
        Assert.Equal(6, cron.Months[1]);
        Assert.Equal(9, cron.Months[2]);
        Assert.Equal(12, cron.Months[3]);

        cron = new Cron("0 0 0 1 */3 *");

        // 下一次
        var dt = DateTime.Parse("2010/08/01");
        var dt2 = cron.GetNext(dt);
        Assert.Equal(DateTime.Parse("2010-09-01"), dt2);

        dt2 = cron.GetNext(dt2);
        Assert.Equal(DateTime.Parse("2010-12-01"), dt2);

        dt2 = cron.GetNext(dt2);
        Assert.Equal(DateTime.Parse("2011-03-01"), dt2);
    }

    [Fact]
    public void is_time_day_of_week_test()
    {
        var cron = new Cron("* * * * * 0");
        Assert.True(cron.IsTime(DateTime.Parse("10/12/2008")));
        Assert.False(cron.IsTime(DateTime.Parse("10/13/2008")));
        Assert.Single(cron.DaysOfWeek);
        Assert.Equal(0, cron.DaysOfWeek[0]);

        cron = new Cron("* * * * * */2");
        Assert.True(cron.IsTime(DateTime.Parse("10/14/2008")));
        Assert.Equal(4, cron.DaysOfWeek.Count);

        var weeks = cron.DaysOfWeek.Keys.ToArray();
        Assert.Equal(0, weeks[0]);
        Assert.Equal(2, weeks[1]);
        Assert.Equal(4, weeks[2]);
        Assert.Equal(6, weeks[3]);
    }

    [Fact]
    public void is_time_test()
    {
        var cron = new Cron("* 0 11 12 10 *");
        Assert.True(cron.IsTime(DateTime.Parse("11:00:00 10/12/2008")));
        Assert.False(cron.IsTime(DateTime.Parse("11:01:00 10/12/2008")));
    }

    [Fact]
    public void dayweek_test()
    {
        // 每个月的第二个星期三
        var cron = new Cron("0 0 0 ? ? 3#2");
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/8/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/15/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/22/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/29/2023")));

        cron = new Cron("0 0 0 ? ? 0-6#1");//每月第一周的任意一天（周一~周日）
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 2/27/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 2/28/2023")));
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/1/2023")));
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/2/2023")));
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/3/2023")));
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/4/2023")));
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/5/2023")));
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/6/2023")));
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/7/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/8/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/9/2023")));

        cron = new Cron("0 0 0 ? ? 1-7#1");//每月第一周的任意一天（周一~周日）
        cron.Sunday = 1;
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/5/2023")));//3月5日是周日 如果认为是第二周第一天，则应该为false、
                                                                      //同时，6、7都为第二周，也应为false

        // 每个月倒数第二个星期三到星期五
        cron = new Cron("0 0 0 ? ? 3-5#L2");
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/8/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/15/2023")));
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/22/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/29/2023")));

        // 每个月的第二个星期二
        cron = new Cron("0 0 0 ? ? 3#2");
        cron.Sunday = 1;
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/7/2023")));
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/14/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/21/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/28/2023")));

        // 每个月倒数第二个星期一到星期三
        cron = new Cron("0 0 0 ? ? 2-4#L2");
        cron.Sunday = 1;
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/7/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/14/2023")));
        Assert.True(cron.IsTime(DateTime.Parse("00:00:00 3/21/2023")));
        Assert.False(cron.IsTime(DateTime.Parse("00:00:00 3/28/2023")));
    }
}