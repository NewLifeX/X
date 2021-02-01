using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Threading;
using Xunit;

namespace XUnitTest.Threading
{
    public class CronTests
    {
        [Theory]
        [InlineData("*/2")]
        [InlineData("* * * * *")]
        [InlineData("0 * * * *")]
        [InlineData("0,1,2 * * * *")]
        [InlineData("*/2 * * * *")]
        [InlineData("1-4 * * * *")]
        [InlineData("1-55/3 * * * *")]
        [InlineData("1,10,20 * * * *")]
        [InlineData("* 1,10,20 * * *")]
        public void Valid(String expression)
        {
            var cron = new Cron();
            Assert.True(cron.Parse(expression));
        }

        [Fact]
        public void is_time_minute_test()
        {
            var cron = new Cron("0 * * * *");
            Assert.True(cron.IsTime(DateTime.Parse("8:00:00")));
            Assert.False(cron.IsTime(DateTime.Parse("8:00:01")));

            cron = new Cron("0-10 * * * *");
            Assert.True(cron.IsTime(DateTime.Parse("8:00:00")));
            Assert.True(cron.IsTime(DateTime.Parse("8:00:03")));

            cron = new Cron("*/2 * * * *");
            Assert.True(cron.IsTime(DateTime.Parse("8:00:00")));
            Assert.True(cron.IsTime(DateTime.Parse("8:00:02")));
            Assert.False(cron.IsTime(DateTime.Parse("8:00:03")));
        }

        [Fact]
        public void is_time_hour_test()
        {
            var cron = new Cron("* 0 * * *");
            Assert.True(cron.IsTime(DateTime.Parse("12:00:00")));

            cron = new Cron("* 0,12 * * *");
            Assert.True(cron.IsTime(DateTime.Parse("12:00:00")));
            Assert.True(cron.IsTime(DateTime.Parse("12:00:00 pm")));
        }

        [Fact]
        public void is_time_day_of_month_test()
        {
            var cron = new Cron("* * * 1 * *");
            Assert.True(cron.IsTime(DateTime.Parse("2010/08/01")));
        }

        [Fact]
        public void is_time_month_test()
        {
            var cron = new Cron("* * * * 1 *");
            Assert.True(cron.IsTime(DateTime.Parse("1/1/2008")));

            cron = new Cron("* * * * 12 *");
            Assert.False(cron.IsTime(DateTime.Parse("1/1/2008")));

            cron = new Cron("* * * * */3 *");
            Assert.True(cron.IsTime(DateTime.Parse("3/1/2008")));
            Assert.True(cron.IsTime(DateTime.Parse("6/1/2008")));
        }

        [Fact]
        public void is_time_day_of_week_test()
        {
            var cron = new Cron("* * * * * 0");
            Assert.True(cron.IsTime(DateTime.Parse("10/12/2008")));
            Assert.False(cron.IsTime(DateTime.Parse("10/13/2008")));

            cron = new Cron("* * * * * */2");
            Assert.True(cron.IsTime(DateTime.Parse("10/14/2008")));
        }

        [Fact]
        public void is_time_test()
        {
            var cron = new Cron("* 0 11 12 10 *");
            Assert.True(cron.IsTime(DateTime.Parse("11:00:00 10/12/2008")));
            Assert.False(cron.IsTime(DateTime.Parse("11:01:00 10/12/2008")));
        }
    }
}