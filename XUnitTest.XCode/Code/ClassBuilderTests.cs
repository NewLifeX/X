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
        private BuilderOption _option;

        public ClassBuilderTests()
        {
            _option = new BuilderOption();
            _tables = ClassBuilder.LoadModels(@"..\..\XCode\Membership\Member.xml", _option, out _);
            _table = _tables.FirstOrDefault(e => e.Name == "User");
        }

        [Fact]
        public void Normal()
        {
            var builder = new ClassBuilder
            {
                Table = _table,
            };
            builder.Option.Namespace = "Company.MyName";
            builder.Option.Usings.Add("NewLife.Remoting");

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
            };
            builder.Option.BaseClass = "MyEntityBase";
            builder.Option.Partial = false;

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
            };
            builder.Option.Pure = true;

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
            };
            builder.Option.Interface = true;

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
            };
            var option = builder.Option;
            option.Pure = true;
            option.Output = ".\\Output\\" + Rand.NextString(8);

            if (Directory.Exists(option.Output.GetFullPath())) Directory.Delete(option.Output.GetFullPath(), true);

            builder.Execute();

            var file = (option.Output + "\\" + builder.Table.DisplayName + ".cs").GetFullPath();
            if (File.Exists(file)) File.Delete(file);

            builder.Save();
            Assert.True(File.Exists(file));

            file = (option.Output + "\\" + builder.Table.Name + ".xs").GetFullPath();
            if (File.Exists(file)) File.Delete(file);

            builder.Save(".xs", false, false);
            Assert.True(File.Exists(file));

            //// 清理
            //Directory.Delete(option.Output.GetFullPath(), true);
        }

        [Fact]
        public void BuildModels()
        {
            //var dir = ".\\Output\\" + Rand.NextString(8);
            var dir = ".\\Output\\Models\\";
            if (Directory.Exists(dir.GetFullPath())) Directory.Delete(dir.GetFullPath(), true);

            var option = new BuilderOption
            {
                Output = dir,
                ClassPrefix = "Model"
            };

            ClassBuilder.BuildModels(_tables, option);
            ClassBuilder.BuildInterfaces(_tables, option);

            foreach (var item in _tables)
            {
                var file = dir.CombinePath(item.Name + "Model.cs").GetFullPath();
                Assert.True(File.Exists(file));

                file = dir.CombinePath("I" + item.Name + "Model.cs").GetFullPath();
                Assert.True(File.Exists(file));
            }
        }

        [Fact]
        public void BuildDtos()
        {
            var dir = ".\\Output\\Dtos\\";
            if (Directory.Exists(dir.GetFullPath())) Directory.Delete(dir.GetFullPath(), true);

            var option = new BuilderOption
            {
                Output = dir,
                ClassPrefix = "Dto"
            };

            ClassBuilder.BuildModels(_tables, option);

            option.ClassPrefix = null;
            ClassBuilder.BuildInterfaces(_tables, option);

            foreach (var item in _tables)
            {
                var file = dir.CombinePath(item.Name + "Dto.cs").GetFullPath();
                Assert.True(File.Exists(file));

                file = dir.CombinePath("I" + item.Name + ".cs").GetFullPath();
                Assert.True(File.Exists(file));
            }
        }

        [Fact]
        public void BuildTT()
        {
            var dir = ".\\Output\\BuildTT\\";
            if (Directory.Exists(dir.GetFullPath())) Directory.Delete(dir.GetFullPath(), true);

            var option = new BuilderOption
            {
                Output = dir,
                ClassPrefix = "TT"
            };

            // 测试Built.tt
            foreach (var item in _tables)
            {
                var builder = new ClassBuilder
                {
                    Table = item,
                    Option = option,
                };
                builder.Execute();
                builder.Save(null, true, false);
            }

            foreach (var item in _tables)
            {
                var file = dir.CombinePath(item.Name + "TT.cs").GetFullPath();
                Assert.True(File.Exists(file));
            }

            // 清理
            //Directory.Delete(dir.GetFullPath(), true);
        }
    }
}