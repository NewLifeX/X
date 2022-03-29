﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NewLife.Data;
using NewLife.Security;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class DAL_Mapper_Tests
    {
        public DAL_Mapper_Tests()
        {
            EntityFactory.InitEntity(typeof(User));
        }

        [Fact]
        public void Query()
        {
            var dal = User.Meta.Session.Dal;

            var list = dal.Query<MyUser>("select * from user", null, 0, 100).ToList();
            list = dal.Query<MyUser>("select * from user where name=@name", new { Name = "admin" }).ToList();
            Assert.NotNull(list);
            Assert.Single(list);

            var user = list[0];
            Assert.Equal(1, user.Id);
            Assert.Equal("admin", user.Name);

            var id = dal.QuerySingle<Int32>("select id from user where name=@name", new { Name = "admin" });
            Assert.Equal(1, id);

            var id2 = dal.QuerySingle<Int32?>("select id from user where name=@name", new { Name = "admin" });
            Assert.Equal(1, id2);

            var user2 = dal.QuerySingle<MyUser>("select * from user where name='xxx'");
            Assert.Null(user2);

            var id3 = dal.QuerySingle<Int32?>("select id from user where name='xxx'");
            Assert.Null(id3);
        }

        [Fact]
        public void QueryPage()
        {
            // 拦截Sql
            var sql = "";
            DAL.LocalFilter = s => sql = s;

            var dal = User.Meta.Session.Dal;

            var list = dal.Query<MyUser>("select * from user where name=@name", new { Name = "admin" }, 0, 5).ToList();
            Assert.NotNull(list);
            Assert.Single(list);

            var user = list[0];
            Assert.Equal(1, user.Id);
            Assert.Equal("admin", user.Name);

            Assert.Equal("[Membership] select * from user where name=@name limit 5 [@Name=admin]", sql);
        }

        [Fact]
        public void QueryPage2()
        {
            // 拦截Sql
            var sql = "";
            DAL.LocalFilter = s => sql = s;

            var dal = User.Meta.Session.Dal;

            var list = dal.Query<MyUser>("select * from user where name=@name", new { Name = "admin" }, new PageParameter { PageIndex = 1, PageSize = 20 }).ToList();
            Assert.NotNull(list);
            Assert.Single(list);

            var user = list[0];
            Assert.Equal(1, user.Id);
            Assert.Equal("admin", user.Name);

            Assert.Equal("[Membership] select * from user where name=@name limit 20 [@Name=admin]", sql);
        }

        [Fact]
        public void Execute()
        {
            var dal = User.Meta.Session.Dal;

            var rs = dal.Execute("Insert Into user(Name) Values(@Name)", new { name = Rand.NextString(8) });
            Assert.True(rs > 0);
        }

        [Fact]
        public void InsertAndUpdateAndDelete()
        {
            var dal = User.Meta.Session.Dal;
            var user = new MyUser { Id = Rand.Next(), Name = Rand.NextString(8) };
            dal.Insert(user, "user");

            dal.Update(new { enable = true }, new { id = user.Id }, "user");

            var list = dal.Query<MyUser>("select * from user where name=@name", new { Name = user.Name }).ToList();
            Assert.NotNull(list);
            Assert.Single(list);

            var user2 = list[0];
            Assert.Equal(user.Id, user2.Id);
            Assert.Equal(user.Name, user2.Name);

            dal.Delete("user", new { id = user.Id });
        }

        [Fact]
        public void InsertNullMember()
        {
            var dal = User.Meta.Session.Dal;

            dal.Delete("user", new { Name = "" });

            var user = new { Id = Rand.Next(), Name = Rand.NextString(8), Mobile = (String)null };
            dal.Insert(user, "user");

            var list = dal.Query<MyUser>("select * from user where id=@id", new { user.Id }).ToList();
            Assert.NotNull(list);
            Assert.Single(list);

            var list2 = dal.Query<MyUser2>("select * from user where id=@id", new { user.Id }).ToList();
            Assert.NotNull(list2);
            Assert.Single(list2);
            Assert.Null(list2[0].RegisterTime);

            dal.Delete("user", new { id = user.Id });
        }

        [Fact]
        public void ExecuteScalar()
        {
            var dal = User.Meta.Session.Dal;

            var id = dal.ExecuteScalar<Int64>("select id from user order by id desc limit 1");
            Assert.True(id > 0);
        }

        [Fact]
        public void ExecuteReader()
        {
            var dal = User.Meta.Session.Dal;

            using var reader = dal.ExecuteReader("select * from user where name=@name", new { name = "admin" });
            var count = reader.FieldCount;
            Assert.True(count > 0);
            while (reader.Read())
            {
                Assert.Equal("admin", reader["name"]);
            }
        }

        class MyUser
        {
            public Int32 Id { get; set; }

            public String Name { get; set; }
        }

        class MyUser2
        {
            public Int32 Id { get; set; }

            public String Name { get; set; }

            public DateTime? RegisterTime { get; set; }
        }

        [Fact]
        public void NullableParameter()
        {
            var dal = User.Meta.Session.Dal;
            var user = new MyUser2 { Id = Rand.Next(), Name = Rand.NextString(8) };

            var dps = dal.Db.CreateParameters(user);
            Assert.Equal(3, dps.Length);

            var dp = dps[2];
            Assert.Equal(DbType.DateTime, dp.DbType);
            Assert.Null(dp.Value);
        }
    }
}