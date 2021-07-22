using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using NewLife;

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

        [Fact]
        public void DateTimeTest()
        {
            var str = "2020-03-09T21:16:17.88";
            var dt = str.ToDateTime();
            Assert.Equal(new DateTime(2020, 3, 9, 21, 16, 17, 880), dt);
        }

        [Fact]
        public void DateTimeOffsetTest()
        {
            var str = "2020-03-09T21:16:25.905+08:00";
            var dt = str.ToDateTime();
            Assert.Equal(new DateTime(2020, 3, 9, 21, 16, 25, 905, DateTimeKind.Local), dt);

            str = "2020-03-09T21:16:25.9052764+08:00";
            var df = str.ToDateTimeOffset();
            Assert.Equal(new DateTimeOffset(2020, 3, 9, 21, 16, 25, 905, TimeSpan.FromHours(8)).AddTicks(2764), df);
        }

        [Fact]
        public void GMKTest()
        {
            var n = 1023L;
            Assert.Equal("1,023", n.ToGMK());

            n = (Int64)(1023.456 * 1024);
            Assert.Equal("1,023.46K", n.ToGMK());

            n = (Int64)(1023.456 * 1024 * 1024);
            Assert.Equal("1,023.46M", n.ToGMK());

            n = (Int64)(1023.456 * 1024 * 1024 * 1024);
            Assert.Equal("1,023.46G", n.ToGMK());

            n = (Int64)(1023.456 * 1024 * 1024 * 1024 * 1024);
            Assert.Equal("1,023.46T", n.ToGMK());

            n = (Int64)(1023.456 * 1024 * 1024 * 1024 * 1024 * 1024);
            Assert.Equal("1,023.46P", n.ToGMK());

            n = (Int64)(1.46 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024);
            Assert.Equal("1.46E", n.ToGMK());
        }

        [Fact]
        public void GMKTest2()
        {
            var format = "n1";

            var n = 1023L;
            Assert.Equal("1,023", n.ToGMK(format));

            n = (Int64)(1023.456 * 1024);
            Assert.Equal("1,023.5K", n.ToGMK(format));

            n = (Int64)(1023.456 * 1024 * 1024);
            Assert.Equal("1,023.5M", n.ToGMK(format));

            n = (Int64)(1023.456 * 1024 * 1024 * 1024);
            Assert.Equal("1,023.5G", n.ToGMK(format));

            n = (Int64)(1023.456 * 1024 * 1024 * 1024 * 1024);
            Assert.Equal("1,023.5T", n.ToGMK(format));

            n = (Int64)(1023.456 * 1024 * 1024 * 1024 * 1024 * 1024);
            Assert.Equal("1,023.5P", n.ToGMK(format));

            n = (Int64)(1.46 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024);
            Assert.Equal("1.5E", n.ToGMK(format));
        }

        [Fact]
        public void PrimitiveTest()
        {
            foreach (TypeCode item in Enum.GetValues(typeof(TypeCode)))
            {
                var type = Type.GetType("System." + item);
                Assert.NotNull(type);
                switch (item)
                {
                    case TypeCode.Empty:
                    case TypeCode.Object:
                    case TypeCode.DBNull:
                        Assert.False(type.IsPrimitive);
                        break;
                    case TypeCode.Boolean:
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                        Assert.True(type.IsPrimitive);
                        break;
                    case TypeCode.Decimal:
                    case TypeCode.DateTime:
                    case TypeCode.String:
                        Assert.False(type.IsPrimitive);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}