using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data
{
    public class DbTableTests
    {
        [Fact]
        public void NomalTest()
        {
            var dt = new DbTable
            {
                Columns = new[] { "Id", "Name", "CreateTime" },
                Rows = new List<Object[]>
                {
                    new Object[] { 123, "Stone", DateTime.Now },
                    new Object[] { 456, "NewLife", DateTime.Today }
                }
            };

            Assert.Equal(123, dt.Get<Int32>(0, "Id"));
            Assert.Equal(456, dt.Get<Int32>(1, "ID"));

            Assert.Equal("NewLife", dt.Get<String>(1, "Name"));
            Assert.Equal(DateTime.Today, dt.Get<DateTime>(1, "CreateTime"));

            // 不存在的字段
            Assert.Equal(DateTime.MinValue, dt.Get<DateTime>(0, "Time"));

            Assert.False(dt.TryGet<DateTime>(1, "Time", out var time));

            var idx = dt.GetColumn("Name");
            Assert.Equal(1, idx);

            idx = dt.GetColumn("Time");
            Assert.Equal(-1, idx);

            // 迭代
            var i = 0;
            foreach (var row in dt)
            {
                if (i == 0)
                {
                    Assert.Equal(123, row["ID"]);
                    Assert.Equal("Stone", row["name"]);
                }
                else if (i == 1)
                {
                    Assert.Equal(456, row["ID"]);
                    Assert.Equal("NewLife", row["name"]);
                    Assert.Equal(DateTime.Today, row["CreateTime"]);
                }
                i++;
            }
        }

        [Fact]
        public void ToJson()
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

            var json = db.ToJson();
            Assert.NotNull(json);
            Assert.Contains("\"Id\":123", json);
            Assert.Contains("\"Name\":\"Stone\"", json);
            Assert.Contains("\"Id\":456", json);
            Assert.Contains("\"Name\":\"NewLife\"", json);
        }

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
        public void BinaryTest()
        {
            var file = Path.GetTempFileName();

            var dt = new DbTable
            {
                Columns = new[] { "ID", "Name", "Time" },
                Types = new[] { typeof(Int32), typeof(String), typeof(DateTime) },
                Rows = new List<Object[]>
                {
                    new Object[] { 11, "Stone", DateTime.Now.Trim() },
                    new Object[] { 22, "大石头", DateTime.Today },
                    new Object[] { 33, "新生命", DateTime.UtcNow.Trim() }
                }
            };
            dt.SaveFile(file, true);

            Assert.True(File.Exists(file));

            var dt2 = new DbTable();
            dt2.LoadFile(file, true);

            Assert.Equal(3, dt2.Rows.Count);
            for (var i = 0; i < 3; i++)
            {
                var m = dt.Rows[i];
                var n = dt2.Rows[i];
                Assert.Equal(m[0], n[0]);
                Assert.Equal(m[1], n[1]);
                Assert.Equal(m[2], n[2]);
            }
        }

        [Fact]
        public void BinaryVerTest()
        {
            var file = Path.GetTempFileName();

            var dt = new DbTable
            {
                Columns = new[] { "ID", "Name", "Time" },
                Types = new[] { typeof(Int32), typeof(String), typeof(DateTime) },
                Rows = new List<Object[]>
                {
                    new Object[] { 11, "Stone", DateTime.Now.Trim() },
                    new Object[] { 22, "大石头", DateTime.Today },
                    new Object[] { 33, "新生命", DateTime.UtcNow.Trim() }
                }
            };
            var pk = dt.ToPacket();

            // 修改版本
            pk[14]++;

            var ex = Assert.Throws<InvalidDataException>(() =>
            {
                var dt2 = new DbTable();
                dt2.Read(pk);
            });

            Assert.Equal("DbTable[ver=1]无法支持较新的版本[2]", ex.Message);
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