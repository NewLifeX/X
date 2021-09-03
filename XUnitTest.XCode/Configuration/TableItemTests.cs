using NewLife.Log;
using XCode.Configuration;
using Xunit;
using XUnitTest.XCode.TestEntity;

namespace XUnitTest.XCode.Configuration
{
    public class TableItemTests
    {
        [Fact]
        public void TrimIndex()
        {
            var ti = TableItem.Create(typeof(Log2));
            XTrace.WriteLine(ti.TableName);
            Assert.Equal(4, ti.DataTable.Indexes.Count);
        }
    }
}