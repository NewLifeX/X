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
    }
}