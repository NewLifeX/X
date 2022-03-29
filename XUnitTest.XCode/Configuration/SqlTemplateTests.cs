using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.Data;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;
using XUnitTest.XCode.TestEntity;

namespace XUnitTest.XCode.Configuration
{
    public class SqlTemplateTests
    {
        private static String _mysql_ConnStr = "Server=.;Port=3306;Database=sys;Uid=root;Pwd=root";

        public SqlTemplateTests()
        {
            var f = "Config\\mysql.config".GetFullPath();
            if (File.Exists(f))
                _mysql_ConnStr = File.ReadAllText(f);
            else
                File.WriteAllText(f, _mysql_ConnStr);
        }

        [Fact]
        public void ParseString()
        {
            var txt = @"select * from userx";
            var st = new SqlTemplate();

            var rs = st.Parse(txt);

            Assert.True(rs);
            Assert.Null(st.Name);
            Assert.Equal(txt, st.Sql);
            Assert.Empty(st.Sqls);
        }

        [Fact]
        public void ParseString2()
        {
            var txt = @"
select * from userx where id=@id

-- [mysql]
select * from userx where id=?id

-- [oracle]
select * from userx where id=@id

-- [sqlserver]
select * from userx where id=@id

";
            var st = new SqlTemplate();

            var rs = st.Parse(txt);

            Assert.True(rs);
            Assert.Null(st.Name);
            Assert.Equal("select * from userx where id=@id", st.Sql);

            Assert.Equal(3, st.Sqls.Count);

            var sql = st.Sqls["MySql"];
            Assert.Equal("select * from userx where id=?id", sql);

            sql = st.Sqls["Oracle"];
            Assert.Equal("select * from userx where id=@id", sql);

            sql = st.Sqls["SqlServer"];
            Assert.Equal("select * from userx where id=@id", sql);
        }

        [Fact]
        public void ParseStream()
        {
            var txt = @"
select * from userx where id=@id

-- [mysql]
select * from userx where id=?id

-- [oracle]
select * from userx where id=@id

-- [sqlserver]
select * from userx where id=@id

";
            var st = new SqlTemplate();
            var ms = new MemoryStream(txt.GetBytes());
            var rs = st.Parse(ms);

            Assert.True(rs);
            Assert.Null(st.Name);
            Assert.Equal("select * from userx where id=@id", st.Sql);

            Assert.Equal(3, st.Sqls.Count);

            var sql = st.Sqls["MySql"];
            Assert.Equal("select * from userx where id=?id", sql);

            sql = st.Sqls["Oracle"];
            Assert.Equal("select * from userx where id=@id", sql);

            sql = st.Sqls["SqlServer"];
            Assert.Equal("select * from userx where id=@id", sql);
        }

        [Fact]
        public void ParseEmbedded()
        {
            var st = new SqlTemplate();
            var type = GetType();
            var rs = st.ParseEmbedded(type.Assembly, type.Namespace, "AreaX.Sql");

            Assert.True(rs);
            Assert.Equal("AreaX", st.Name);
            Assert.Equal("select * from area where enable=1", st.Sql);

            Assert.Equal(2, st.Sqls.Count);

            var sql = st.Sqls["MySql"];
            Assert.Equal("select * from area where `enable`=1", sql);

            sql = st.Sqls["Sqlite"];
            Assert.Equal("select * from area where 'enable'=1", sql);

            sql = st.GetSql(DatabaseType.SqlServer);
            Assert.Equal("select * from area where enable=1", st.Sql);
        }

        [Fact]
        public void EntityTest()
        {
            var fact = Menu3.Meta.Factory;
            var st = fact.Template;
            Assert.NotNull(st);
            Assert.NotEmpty(st.Name);
            Assert.NotEmpty(st.Sql);

            Assert.Equal("select * from menu2 where visible=1", st.Sql);
            Assert.Equal(2, st.Sqls.Count);

            var sql = st.Sqls["MySql"];
            Assert.Equal("select * from menu2 where 'visible'=1", sql);

            sql = st.Sqls["Sqlite"];
            Assert.Equal("select * from menu2 where 'visible'=2", sql);
        }

        [Fact]
        public void EntityTest2()
        {
            var fact = Role2.Meta.Factory;
            var st = fact.Template;
            Assert.NotNull(st);
            Assert.Null(st.Name);
            Assert.Null(st.Sql);

            Assert.Equal(0, st.Sqls.Count);
        }

        [Fact]
        public void EntityTestWithSqlite()
        {
            EntityFactory.InitEntity(typeof(Menu2));

            // 拦截Sql
            var sql = "";
            DAL.LocalFilter = s => sql = s;

            var count = Menu3.Meta.Count;
            Assert.Equal("[test] Select Count(*) From (select * from menu2 where 'visible'=2) SourceTable", sql);

            var menu = Menu3.FindByID(1234);
            Assert.Equal("[test] Select * From (select * from menu2 where 'visible'=2) SourceTable Order By ID Desc", sql);

            var menu2 = Menu3.FindByKey(1234);
            Assert.Equal("[test] Select * From (select * from menu2 where 'visible'=2) SourceTable Where ID=1234", sql);

            var date = DateTime.Today;
            var list = Menu3.Search(date, date, "stone", new PageParameter { PageIndex = 2, PageSize = 30 });
            Assert.Equal("[test] Select * From (select * from menu2 where 'visible'=2) SourceTable Where (Name Like '%stone%' Or DisplayName Like '%stone%' Or FullName Like '%stone%' Or Url Like '%stone%' Or Icon Like '%stone%' Or Permission Like '%stone%' Or Remark Like '%stone%') Order By ID Desc limit 30, 30", sql);
        }

        [Fact]
        public void EntityTestWithMySql()
        {
            DAL.AddConnStr("mysql_member", _mysql_ConnStr, null, "mysql");

            Menu2.Meta.ConnName = "mysql_member";
            Menu3.Meta.ConnName = "mysql_member";
            var n = Menu2.Meta.Count;

            // 拦截Sql
            var sql = "";
            DAL.LocalFilter = s => sql = s;

            var count = Menu3.Meta.Count;
            Assert.Equal("[mysql_member] Select Count(*) From (select * from menu2 where 'visible'=1) SourceTable", sql);

            var menu = Menu3.FindByID(1234);
            Assert.Equal("[mysql_member] Select * From (select * from menu2 where 'visible'=1) SourceTable Order By ID Desc", sql);

            var menu2 = Menu3.FindByKey(1234);
            Assert.Equal("[mysql_member] Select * From (select * from menu2 where 'visible'=1) SourceTable Where ID=1234", sql);

            var date = DateTime.Today;
            var list = Menu3.Search(date, date, "stone", new PageParameter { PageIndex = 2, PageSize = 30 });
            Assert.Equal("[mysql_member] Select * From (select * from menu2 where 'visible'=1) SourceTable Where (Name Like '%stone%' Or DisplayName Like '%stone%' Or FullName Like '%stone%' Or Url Like '%stone%' Or Icon Like '%stone%' Or Permission Like '%stone%' Or Remark Like '%stone%') Order By ID Desc limit 30, 30", sql);
        }
    }
}