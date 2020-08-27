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
        private IList<IDataTable> _tables;
        private IDataTable _table;
        public ClassBuilderTests()
        {
            _tables = EntityBuilder.LoadModels(@"..\..\XCode\Membership\Member.xml", out _, out _);
            _table = _tables.FirstOrDefault(e => e.Name == "User");
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

        [Fact]
        public void BuildModels()
        {
            //var dir = ".\\Output\\" + Rand.NextString(8);
            var dir = ".\\Output\\Models\\";
            if (Directory.Exists(dir.GetFullPath())) Directory.Delete(dir.GetFullPath(), true);

            ClassBuilder.BuildModels(_tables, dir, "Model");
            ClassBuilder.BuildInterfaces(_tables, dir, "Model");

            foreach (var item in _tables)
            {
                var file = dir.CombinePath(item.Name + "Model.cs").GetFullPath();
                Assert.True(File.Exists(file));

                file = dir.CombinePath("I" + item.Name + "Model.cs").GetFullPath();
                Assert.True(File.Exists(file));
            }

            // 清理
            //Directory.Delete(dir.GetFullPath(), true);
        }

        [Fact]
        public void BuildDtos()
        {
            //var dir = ".\\Output\\" + Rand.NextString(8);
            var dir = ".\\Output\\Dtos\\";
            if (Directory.Exists(dir.GetFullPath())) Directory.Delete(dir.GetFullPath(), true);

            ClassBuilder.BuildModels(_tables, dir, "Dto");
            ClassBuilder.BuildInterfaces(_tables, dir);

            foreach (var item in _tables)
            {
                var file = dir.CombinePath(item.Name + "Dto.cs").GetFullPath();
                Assert.True(File.Exists(file));

                file = dir.CombinePath("I" + item.Name + ".cs").GetFullPath();
                Assert.True(File.Exists(file));
            }

            // 清理
            //Directory.Delete(dir.GetFullPath(), true);
        }
    }
}