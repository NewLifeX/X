using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using XCode.Configuration;
using XCode.Membership;
using Xunit;

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