using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XCode.Configuration;
using Xunit;

namespace XUnitTest.XCode.Configuration
{
    public class SqlTemplateTests
    {
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
    }
}