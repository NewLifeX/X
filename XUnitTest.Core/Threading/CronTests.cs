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
            Assert.Equal(4, cron.DaysOfWeek.Length);
            Assert.Equal(0, cron.DaysOfWeek[0]);
            Assert.Equal(2, cron.DaysOfWeek[1]);
            Assert.Equal(4, cron.DaysOfWeek[2]);
            Assert.Equal(6, cron.DaysOfWeek[3]);
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