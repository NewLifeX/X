using System;
using System.Collections.Generic;
using System.Linq;
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

        [Fact]
        public void ModelsTest()
        {
            var list = new List<UserModel>
            {
                new UserModel { ID = 11, Name = "Stone", Time = DateTime.Now },
                new UserModel { ID = 22, Name = "大石头", Time = DateTime.Today },
                new UserModel { ID = 33, Name = "新生命", Time = DateTime.UtcNow }
            };

            var dt = new DbTable();
            dt.WriteModels(list);

            Assert.NotNull(dt.Columns);
            Assert.Equal(3, dt.Columns.Length);
            Assert.Equal(nameof(UserModel.ID), dt.Columns[0]);
            Assert.Equal(nameof(UserModel.Name), dt.Columns[1]);
            Assert.Equal(nameof(UserModel.Time), dt.Columns[2]);

            Assert.NotNull(dt.Types);
            Assert.Equal(3, dt.Types.Length);
            Assert.Equal(typeof(Int32), dt.Types[0]);
            Assert.Equal(typeof(String), dt.Types[1]);
            Assert.Equal(typeof(DateTime), dt.Types[2]);

            Assert.NotNull(dt.Rows);
            Assert.Equal(3, dt.Rows.Count);
            Assert.Equal(11, dt.Rows[0][0]);
            Assert.Equal("大石头", dt.Rows[1][1]);

            var list2 = dt.ReadModels<UserModel2>().ToList();
            Assert.NotNull(list2);
            Assert.Equal(3, list2.Count);
            for (var i = 0; i < list2.Count; i++)
            {
                var m = list[i];
                var n = list2[i];
                Assert.Equal(m.ID, n.ID);
                Assert.Equal(m.Name, n.Name);
                Assert.Equal(m.Time, n.Time);
            }
        }

        private class UserModel
        {
            public Int32 ID { get; set; }

            public String Name { get; set; }

            public DateTime Time { get; set; }
        }

        private class UserModel2
        {
            public Int32 ID { get; set; }

            public String Name { get; set; }

            public DateTime Time { get; set; }
        }
    }
}