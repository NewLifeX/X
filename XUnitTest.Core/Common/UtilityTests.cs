using Microsoft.Extensions.Primitives;
using NewLife;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Common;

public class UtilityTests
{
    [Fact(DisplayName = "基础测试")]
    public void BasicTest()
    {
        var dt = DateTime.Now;
        Assert.Equal(DateTimeKind.Local, dt.Kind);
        Assert.Equal(dt.ToString("yyyy-MM-dd HH:mm:ss"), dt.ToFullString());
        Assert.Equal(dt.ToString("yyyy-MM-dd HH:mm:ss.fff"), dt.ToFullString(true));

        var dt_ = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
        Assert.Equal(DateTimeKind.Unspecified, dt_.Kind);
        Assert.Equal(dt.Trim(), dt_);

        var dt2 = dt.ToFullString().ToDateTime();
        Assert.Equal(DateTimeKind.Unspecified, dt2.Kind);
        Assert.Equal(dt.Trim(), dt2);

        dt2 = dt.ToInt().ToDateTime();
        Assert.Equal(DateTimeKind.Unspecified, dt2.Kind);
        Assert.Equal(dt.Trim(), dt2);

        dt2 = dt.ToLong().ToDateTime();
        Assert.Equal(DateTimeKind.Unspecified, dt2.Kind);
        Assert.Equal(dt.Trim("ms"), dt.ToLong().ToDateTime());

        dt2 = dt.ToInt().ToDateTime();
        Assert.Equal(dt.Trim("m"), dt2.AddSeconds(-dt2.Second));
        Assert.Equal(dt.Trim("h"), dt2.AddSeconds(-dt2.Second).AddMinutes(-dt2.Minute));
        Assert.Empty(DateTime.MinValue.ToFullString(""));
        Assert.Equal(dt.ToString("yyyy-MM-dd HH:mm:ss"), dt.ToString("", ""));
        Assert.Empty(DateTime.MinValue.ToString("", ""));

        var str = dt.ToString("O");
        XTrace.WriteLine(str);
        var dtU = str.ToDateTime();
        Assert.Equal(dt, dtU);
        Assert.Equal(dt.Kind, dtU.Kind);
    }

    [Fact]
    public void UtcTest()
    {
        var dt = DateTime.UtcNow;
        Assert.Equal(DateTimeKind.Utc, dt.Kind);
        Assert.Equal(dt.ToString("yyyy-MM-dd HH:mm:ss"), dt.ToFullString());
        Assert.Equal(dt.ToString("yyyy-MM-dd HH:mm:ss.fff"), dt.ToFullString(true));

        var dt_ = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc);
        Assert.Equal(DateTimeKind.Utc, dt_.Kind);
        Assert.Equal(dt.Trim(), dt_);

        var dt2 = dt.ToFullString().ToDateTime();
        Assert.Equal(DateTimeKind.Unspecified, dt2.Kind);
        Assert.Equal(dt.Trim(), dt2);

        dt2 = dt.ToInt().ToDateTime();
        Assert.Equal(DateTimeKind.Unspecified, dt2.Kind);
        Assert.Equal(dt.Trim(), dt2);

        dt2 = dt.ToLong().ToDateTime();
        Assert.Equal(DateTimeKind.Unspecified, dt2.Kind);
        Assert.Equal(dt.Trim("ms"), dt.ToLong().ToDateTime());

        dt2 = dt.ToInt().ToDateTime();
        Assert.Equal(dt.Trim("m"), dt2.AddSeconds(-dt2.Second));
        Assert.Equal(dt.Trim("h"), dt2.AddSeconds(-dt2.Second).AddMinutes(-dt2.Minute));
        Assert.Empty(DateTime.MinValue.ToFullString(""));
        Assert.Equal(dt.ToString("yyyy-MM-dd HH:mm:ss"), dt.ToString("", ""));
        Assert.Empty(DateTime.MinValue.ToString("", ""));

        var str = dt.ToString("O");
        XTrace.WriteLine(str);
        var dtU = str.ToDateTime();
        Assert.Equal(dt, dtU);
        Assert.Equal(dt.Kind, dtU.Kind);
    }

    [Fact]
    public void TimeZoneInfoTest()
    {
        var dto = DateTimeOffset.Now;
        Assert.Equal(dto.ToString("yyyy-MM-dd HH:mm:ss zzz"), dto.ToFullString());
        Assert.Equal(dto.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"), dto.ToFullString(true));
        Assert.Equal(dto.Trim(), dto.ToFullString().ToDateTimeOffset());
        Assert.Equal(dto.Trim(), dto.ToInt().ToDateTimeOffset());
        Assert.Equal(dto.Trim("ms"), dto.ToLong().ToDateTimeOffset());
        Assert.Empty(DateTimeOffset.MinValue.ToFullString(""));

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
    public void NanTest()
    {
        var num = Double.NaN;

        var dc = num.ToDecimal(123);
        Assert.Equal(123, dc);

        var dd = num.ToDouble(456);
        Assert.Equal(456, dd);
    }

    [Fact]
    public void StringValuesConvert()
    {
        var svs = new StringValues(new[] { "123", "456" });
        Assert.Equal(123, svs.ToInt());
        Assert.Equal(123L, svs.ToDouble());
        Assert.Equal(123, svs.ToDecimal());
        Assert.True(svs.ToBoolean());

        svs = new StringValues(new[] { "0", "1" });
        Assert.False(svs.ToBoolean());

        svs = new StringValues(new[] { "2023-1-11", "2022-1-1" });
        var dt = new DateTime(2023, 1, 11);
        Assert.Equal(dt, svs.ToDateTime());
        Assert.Equal(new DateTimeOffset(dt), svs.ToDateTimeOffset());
    }

    [Fact]
    public void DateTimeTest()
    {
        var str = "2020-03-09T21:16:17.88";
        var dt = str.ToDateTime();
        Assert.Equal(new DateTime(2020, 3, 9, 21, 16, 17, 880), dt);

        str = "20220406135923";
        dt = str.ToDateTime();
        Assert.Equal(new DateTime(2022, 4, 6, 13, 59, 23), dt);

        str = "20220406";
        dt = str.ToDateTime();
        Assert.Equal(new DateTime(2022, 4, 6), dt);

        str = "2022-4-6";
        dt = str.ToDateTime();
        Assert.Equal(new DateTime(2022, 4, 6), dt);

        str = "2022/4/6";
        dt = str.ToDateTime();
        Assert.Equal(new DateTime(2022, 4, 6), dt);

        var dt1970 = new DateTime(1970, 1, 1);
        var dt1970b = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // DateTime 改变时区后再相减，结果改变；而 DateTimeOffset 不会
        // DateTime 减去目标时间，忽略目标时区，直接减去时间值
        dt = DateTime.UtcNow;
        var n1 = (Int32)(dt - dt1970).TotalSeconds;
        var n2 = (Int32)(dt.ToLocalTime() - dt1970).TotalSeconds;
        var n3 = (Int32)(dt - dt1970b).TotalSeconds;
        var n4 = (Int32)(dt.ToLocalTime() - dt1970b).TotalSeconds;
        Assert.Equal(n1, n3);
        Assert.Equal(n2, n4);
    }

    [Fact]
    public void DateTimeOffsetTest()
    {
        var str = "2020-03-09T21:16:25.905+08:00";
        var dt = str.ToDateTime().ToUniversalTime();
        Assert.Equal(new DateTime(2020, 3, 9, 13, 16, 25, 905, DateTimeKind.Utc), dt);

        str = "2020-03-09T21:16:25.9052764+08:00";
        var df = str.ToDateTimeOffset();
        Assert.Equal(new DateTimeOffset(2020, 3, 9, 21, 16, 25, 905, TimeSpan.FromHours(8)).AddTicks(2764), df);

        var dt1970 = new DateTimeOffset(new DateTime(1970, 1, 1));
        var dt1970b = new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        // DateTimeOffset 可以改变时区后再相减，结果不变
        var dto = DateTimeOffset.UtcNow;
        var n1 = (Int32)(dto - dt1970).TotalSeconds;
        var n2 = (Int32)(dto.ToLocalTime() - dt1970).TotalSeconds;
        var n3 = (Int32)(dto - dt1970b).TotalSeconds;
        var n4 = (Int32)(dto.ToLocalTime() - dt1970b).TotalSeconds;
        Assert.Equal(n1, n2);
        Assert.Equal(n3, n4);
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

    [Fact]
    public void ToDouble()
    {
        var n = 12.34;
        var buf = BitConverter.GetBytes(n);
        Assert.Equal(8, buf.Length);

        var v = buf.ToDouble();
        Assert.Equal(n, v);
    }

    [Fact]
    public void MaxDateTime()
    {
        var n = 0x03000000031E615DL;
        var dt = n.ToDateTime();

        Assert.Equal(DateTime.MinValue, dt);
    }

    [Fact]
    public void UtcTime()
    {
        //!!! 两个时间相减，即使时区不同，也不会转为相同时区再相减，而是直接相减

        var now = DateTime.Now;
        var utc = now.ToUniversalTime();
        var diff = (Int64)(now - utc).TotalMilliseconds;

        {
            var baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
            var ms1 = (Int64)(now - baseTime).TotalMilliseconds;
            var ms2 = (Int64)(utc - baseTime).TotalMilliseconds;
            Assert.Equal(ms1, ms2 + diff);
        }

        {
            var baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ms1 = (Int64)(now - baseTime).TotalMilliseconds;
            var ms2 = (Int64)(utc - baseTime).TotalMilliseconds;
            Assert.Equal(ms1, ms2 + diff);
        }

        {
            var baseTime = new DateTime(1970, 1, 1);
            var ms1 = (Int64)(now - baseTime).TotalMilliseconds;
            var ms2 = (Int64)(utc - baseTime).TotalMilliseconds;
            Assert.Equal(ms1, ms2 + diff);
        }

        //var time = 0.ToDateTime();
        //Assert.Equal(DateTimeKind.Local, time.Kind);

        // Unspecified类型的时间，不管是转本地时间还是UTC时间，都会改变时间值
        now = DateTime.Now.Trim("s");
        var str = now.ToFullString();

        var time = str.ToDateTime();
        Assert.Equal(DateTimeKind.Unspecified, time.Kind);
        Assert.Equal(str, time.ToFullString());

        // github action 服务器刚好是UTC时间，所以这里测试不通过
        //Assert.NotEqual(str, time.ToLocalTime().ToFullString());
        //Assert.NotEqual(str, time.ToUniversalTime().ToFullString());
    }

    [Fact]
    public void NegativeNumber()
    {
        var n = "-1";
        var v = n.ToInt();

        Assert.Equal(-1, v);

        n = "+1";
        v = n.ToInt();
        Assert.Equal(+1, v);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void JsonNodeTest()
    {
        var json = "{\"Code\":1}";
        var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(json);

        var c = jsonNode["Code"];

        Assert.Equal(1, c.ToInt());
    }
#endif

    [Fact]
    public void BigDoubleTest()
    {
        var ticks = DateTime.Now.Ticks;
        var str = ticks.ToString();
        var d = str.ToDouble();
        Assert.Equal(ticks, d);

        str = ticks.ToString("E");
        var d2 = Double.Parse(str);
        //Assert.True(Math.Abs(ticks - d) < 10);
        Assert.Equal(ticks, d2, 1000_0000_0000f);

        var d3 = str.ToDouble();
        Assert.Equal(ticks, d3, 1000_0000_0000f);
        Assert.Equal(d2, d3);
    }

    [Fact]
    public void BigDecimalTest()
    {
        var ticks = (Decimal)DateTime.Now.Ticks;
        var str = ticks.ToString();
        var d = str.ToDecimal();
        Assert.Equal(ticks, d);

        str = ticks.ToString("e");
        //var d2 = Decimal.Parse(str);
        //Assert.Equal(ticks, d2, 17);
        Assert.Throws<FormatException>(() => Decimal.Parse(str));

        var d3 = str.ToDecimal();
        //Assert.Equal(ticks, d3, 17);
        Assert.True(Math.Abs(ticks - d3) < 1000_0000_0000);
        //Assert.Equal(d2, d3);
    }
}