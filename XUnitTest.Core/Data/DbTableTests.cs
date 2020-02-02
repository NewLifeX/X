using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data
{
    public class DbTableTests
    {
        //[Fact]
        //public void ToJson()
        //{

        //}

        [Fact]
        public void ToDictionary()
        {
            var db = new DbTable
            {
                Columns = new[] { "Id", "Name", "CreateTime" },
                Rows = new List<Object[]>
                {
                    new Object[] { 123, "Stone", DateTime.Now },
                    new Object[] { 456, "NewLife", DateTime.Today }
                }
            };

            var list = db.ToDictionary();
            Assert.NotNull(list);
            Assert.Equal(2, list.Count);

            var dic = list[0];
            Assert.Equal(123, dic["Id"]);
            Assert.Equal("Stone", dic["Name"]);
        }
    }
}