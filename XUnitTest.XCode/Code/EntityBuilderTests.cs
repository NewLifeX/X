using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XCode.Code;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Code
{
    public class EntityBuilderTests
    {
        [Fact]
        public void Normal()
        {
            var builder = new EntityBuilder();
            builder.Table = UserX.Meta.Table.DataTable;
            builder.ConnName = "MyConn";
            builder.Namespace = "Company.MyName";
            builder.Usings.Add("NewLife.Remoting");

            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\entity_user_normal.cs".GetFullPath());
            Assert.Equal(target, rs);
        }

        [Fact]
        public void GenericType()
        {
            var builder = new EntityBuilder();
            builder.Table = UserX.Meta.Table.DataTable;
            builder.GenericType = true;
            builder.Namespace = "Company.MyName";

            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\entity_user_generictype.cs".GetFullPath());
            Assert.Equal(target, rs);
        }
    }
}
