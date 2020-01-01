using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace XUnitTest.Common
{
    public class UtilityTests
    {
        [Fact(DisplayName = "基础测试")]
        public void BasicTest()
        {
            var dt = DateTime.Now;
            Assert.Equal(DateTimeKind.Local, dt.Kind);
            Assert.Equal(dt.ToString("yyyy-MM-dd HH:mm:ss"), dt.ToFullString());
            var dt_ = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
            Assert.Equal(dt.Trim(), dt.ToFullString().ToDateTime());
            Assert.Equal(dt.Trim(), dt.ToInt().ToDateTime());
            Assert.Equal(dt.Trim("ms"), dt.ToLong().ToDateTime());

            var dto = DateTimeOffset.Now;
            Assert.Equal(dto.ToString("yyyy-MM-dd HH:mm:ss zzz"), dto.ToFullString());
            Assert.Equal(dto.Trim(), dto.ToFullString().ToDateTimeOffset());
            Assert.Equal(dto.Trim(), dto.ToInt().ToDateTimeOffset());
            Assert.Equal(dto.Trim("ms"), dto.ToLong().ToDateTimeOffset());

            var dt2 = dto.ToUniversalTime();
            Assert.Equal(dt2.ToString("yyyy-MM-dd HH:mm:ss zzz"), dt2.ToFullString());
            Assert.Equal(dt2.Trim(), dt2.ToFullString().ToDateTimeOffset());
            Assert.Equal(dt2.Trim(), dt2.ToInt().ToDateTimeOffset());
            Assert.Equal(dt2.Trim("ms"), dt2.ToLong().ToDateTimeOffset());

            // Newfoundland Standard Time,(GMT-03:30) 纽芬兰,纽芬兰标准时间
            var dt3 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dto, "Newfoundland Standard Time");
            Assert.Equal(dt3.ToString("yyyy-MM-dd HH:mm:ss zzz"), dt3.ToFullString());
            Assert.Equal(dt3.Trim(), dt3.ToFullString().ToDateTimeOffset());
            Assert.Equal(dt3.Trim(), dt3.ToInt().ToDateTimeOffset());
            Assert.Equal(dt3.Trim("ms"), dt3.ToLong().ToDateTimeOffset());

            // Nepal Standard Time,(GMT+05:45) 加德满都,尼泊尔标准时间
            var dt4 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dto, "Nepal Standard Time");
            Assert.Equal(dt4.ToString("yyyy-MM-dd HH:mm:ss zzz"), dt4.ToFullString());
            Assert.Equal(dt4.Trim(), dt4.ToFullString().ToDateTimeOffset());
            Assert.Equal(dt4.Trim(), dt4.ToInt().ToDateTimeOffset());
            Assert.Equal(dt4.Trim("ms"), dt4.ToLong().ToDateTimeOffset());
        }
    }
}