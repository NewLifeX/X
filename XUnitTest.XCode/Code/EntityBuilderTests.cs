using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XCode.Code;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Code
{
    public class EntityBuilderTests
    {
        private IDataTable _table;
        public EntityBuilderTests()
        {
            var tables = EntityBuilder.LoadModels(@"..\..\XCode\Membership\Member.xml", out _, out _);
            _table = tables.FirstOrDefault(e => e.Name == "User");
        }

        [Fact]
        public void Normal()
        {
            var builder = new EntityBuilder
            {
                Table = _table,
                ConnName = "MyConn",
                Namespace = "Company.MyName"
            };
            builder.Usings.Add("NewLife.Remoting");

            // 数据类
            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\entity_user_normal.cs".GetFullPath());
            Assert.Equal(target, rs);

            // 业务类
            builder.Business = true;
            builder.Execute();

            rs = builder.ToString();
            Assert.NotEmpty(rs);

            target = File.ReadAllText("Code\\entity_user_normal_biz.cs".GetFullPath());
            Assert.Equal(target, rs);
        }

        [Fact]
        public void GenericType()
        {
            var builder = new EntityBuilder
            {
                Table = _table,
                GenericType = true,
                Namespace = "Company.MyName"
            };

            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\entity_user_generictype.cs".GetFullPath());
            Assert.Equal(target, rs);
        }
    }
}
