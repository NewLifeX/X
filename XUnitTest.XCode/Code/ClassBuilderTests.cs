using System;
using System.Collections.Generic;
using System.Text;
using XCode.Code;
using XCode.Membership;
using Xunit;
using System.IO;
using NewLife.Security;
using XCode.DataAccessLayer;
using System.Linq;

namespace XUnitTest.XCode.Code
{
    public class ClassBuilderTests
    {
        private IDataTable _table;
        public ClassBuilderTests()
        {
            var tables = EntityBuilder.LoadModels(@"..\..\XCode\Membership\Member.xml", out _, out _);
            _table = tables.FirstOrDefault(e => e.Name == "User");
        }

        [Fact]
        public void Normal()
        {
            var builder = new ClassBuilder
            {
                Table = _table,
                Namespace = "Company.MyName"
            };
            builder.Usings.Add("NewLife.Remoting");

            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\class_user_normal.cs".GetFullPath());
            Assert.Equal(target, rs);
        }

        [Fact]
        public void BaseClass()
        {
            var builder = new ClassBuilder
            {
                Table = _table,
                BaseClass = "MyEntityBase",
                Partial = false
            };

            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\class_user_baseclass.cs".GetFullPath());
            Assert.Equal(target, rs);
        }

        [Fact]
        public void Pure()
        {
            var builder = new ClassBuilder
            {
                Table = _table,
                Pure = true
            };

            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\class_user_pure.cs".GetFullPath());
            Assert.Equal(target, rs);
        }

        [Fact]
        public void Interface()
        {
            var builder = new ClassBuilder
            {
                Table = _table,
                Interface = true
            };

            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\class_user_interface.cs".GetFullPath());
            Assert.Equal(target, rs);
        }

        [Fact]
        public void Save()
        {
            var builder = new ClassBuilder
            {
                Table = _table,
                Pure = true,
                Output = ".\\Output\\" + Rand.NextString(8)
            };

            builder.Execute();

            var file = (builder.Output + "\\" + builder.Table.DisplayName + ".cs").GetFullPath();
            if (File.Exists(file)) File.Delete(file);

            builder.Save();
            Assert.True(File.Exists(file));

            file = (builder.Output + "\\" + builder.Table.Name + ".xs").GetFullPath();
            if (File.Exists(file)) File.Delete(file);

            builder.Save(".xs", false, false);
            Assert.True(File.Exists(file));

            // 清理
            Directory.Delete(builder.Output.GetFullPath(), true);
        }
    }
}