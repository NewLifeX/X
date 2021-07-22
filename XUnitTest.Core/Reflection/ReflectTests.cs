using System;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Reflection
{
    public class ReflectTests
    {
        [Theory]
        [InlineData(typeof(Boolean))]
        [InlineData(typeof(Char))]
        [InlineData(typeof(Byte))]
        [InlineData(typeof(Int16))]
        [InlineData(typeof(UInt16))]
        [InlineData(typeof(Int32))]
        [InlineData(typeof(UInt32))]
        [InlineData(typeof(Int64))]
        [InlineData(typeof(UInt64))]
        [InlineData(typeof(Single))]
        [InlineData(typeof(Double))]
        [InlineData(typeof(Decimal))]
        [InlineData(typeof(String))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(Byte[]))]
        public void GetTypeExTest(Type type)
        {
            var name = type.Name;
            var t2 = name.GetTypeEx(false);
            Assert.Equal(type, t2);
        }

        [Theory]
        [InlineData("true", typeof(Boolean), true)]
        [InlineData("1234", typeof(Int16), (Int16)1234)]
        [InlineData("1234", typeof(Int32), 1234)]
        [InlineData("12.34", typeof(Double), 12.34)]
        [InlineData("byte[]", typeof(Type), typeof(Byte[]))]
        public void ChangeTypeTest(Object value, Type targetType, Object target)
        {
            var rs = value.ChangeType(targetType);
            Assert.Equal(target, rs);
        }
    }
}