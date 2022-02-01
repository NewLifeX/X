using System;
using System.IO;
using System.Linq;
using XCode.Code;
using XCode.DataAccessLayer;
using Xunit;

namespace XUnitTest.XCode.Code
{
    public class EntityBuilderTests
    {
        private IDataTable _table;
        private BuilderOption _option;

        public EntityBuilderTests()
        {
            _option = new BuilderOption();
            var tables = ClassBuilder.LoadModels(@"..\..\XCode\Membership\Member.xml", _option, out _);
            _table = tables.FirstOrDefault(e => e.Name == "User");
        }

        private String ReadTarget(String file, String text)
        {
            //var file2 = @"..\..\XUnitTest.XCode\".CombinePath(file);
            //File.WriteAllText(file2, text);

            var target = File.ReadAllText(file.GetFullPath());

            return target;
        }

        [Fact]
        public void Normal()
        {
            var option = new BuilderOption
            {
                ConnName = "MyConn",
                Namespace = "Company.MyName",
                Partial = true,
            };
            option.Usings.Add("NewLife.Remoting");

            var builder = new EntityBuilder
            {
                Table = _table,
                Option = option,
            };

            // 数据类
            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = ReadTarget("Code\\entity_user_normal.cs", rs);
            Assert.Equal(target, rs);

            // 业务类
            builder.Business = true;
            builder.Execute();

            rs = builder.ToString();
            Assert.NotEmpty(rs);

            target = ReadTarget("Code\\entity_user_normal_biz.cs", rs);
            Assert.Equal(target, rs);
        }

        [Fact]
        public void Exclude()
        {
            var option = new BuilderOption
            {
                ConnName = "MyConn",
                Namespace = "Company.MyName",
                Partial = true,
            };
            option.Usings.Add("NewLife.Remoting");

            var builder = new EntityBuilder
            {
                Table = _table,
                Option = option,
            };

            // 数据类
            builder.Execute();

            var columns = _table.Columns.Where(e => e.Properties["Interface"] == "False").ToList();
            Assert.Equal(4, columns.Count);

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = ReadTarget("Code\\entity_user_normal.cs", rs);
            Assert.Equal(target, rs);

            // 业务类
            builder.Business = true;
            builder.Execute();

            rs = builder.ToString();
            Assert.NotEmpty(rs);

            target = ReadTarget("Code\\entity_user_normal_biz.cs", rs);
            Assert.Equal(target, rs);
        }

        //[Fact]
        //public void GenericType()
        //{
        //    var option = new BuilderOption
        //    {
        //        ConnName = "MyConn",
        //        Namespace = "Company.MyName"
        //    };

        //    var builder = new EntityBuilder
        //    {
        //        Table = _table,
        //        GenericType = true,
        //        Option = option,
        //    };

        //    builder.Execute();

        //    var rs = builder.ToString();
        //    Assert.NotEmpty(rs);

        //    var target = File.ReadAllText("Code\\entity_user_generictype.cs",rs);
        //    Assert.Equal(target, rs);
        //}

        [Fact]
        public void BuildTT()
        {
            var dir = @".\Entity\".GetFullPath();
            if (Directory.Exists(dir)) Directory.Delete(dir, true);

            dir = @".\Output\EntityModels\".GetFullPath();
            if (Directory.Exists(dir)) Directory.Delete(dir, true);

            dir = @".\Output\EntityInterfaces\".GetFullPath();
            if (Directory.Exists(dir)) Directory.Delete(dir, true);

            // 加载模型文件，得到数据表
            var file = @"..\..\XUnitTest.XCode\Code\Member.xml";
            var option = new BuilderOption();
            var tables = ClassBuilder.LoadModels(file, option, out var atts);
            EntityBuilder.FixModelFile(file, option, atts, tables);

            // 生成实体类
            option.BaseClass = "I{name}";
            option.ModelNameForCopy = "I{name}";
            EntityBuilder.BuildTables(tables, option, chineseFileName: true);

            // 生成简易模型类
            option.Output = @"Output\EntityModels\";
            option.ClassNameTemplate = "{name}Model";
            option.ModelNameForCopy = "I{name}";
            ClassBuilder.BuildModels(tables, option);

            // 生成简易接口
            option.BaseClass = null;
            option.ClassNameTemplate = null;
            option.Output = @"Output\EntityInterfaces\";
            ClassBuilder.BuildInterfaces(tables, option);

            // 精确控制生成
            /*foreach (var item in tables)
            {
                var builder = new ClassBuilder
                {
                    Table = item,
                    Option = option,
                };
                builder.Execute();
                builder.Save(null, true, false);
            }*/

            {
                var rs = File.ReadAllText("Entity\\用户.cs".GetFullPath());
                var target = ReadTarget("Code\\Entity\\用户.cs", rs);
                Assert.Equal(target, rs);
            }

            {
                var rs = File.ReadAllText("Entity\\用户.Biz.cs".GetFullPath());
                var target = ReadTarget("Code\\Entity\\用户.Biz.cs", rs);
                Assert.Equal(target, rs);
            }

            {
                var rs = File.ReadAllText("Output\\EntityModels\\UserModel.cs".GetFullPath());
                var target = ReadTarget("Code\\EntityModels\\UserModel.cs", rs);
                Assert.Equal(target, rs);
            }

            {
                var rs = File.ReadAllText("Output\\EntityInterfaces\\IUser.cs".GetFullPath());
                var target = ReadTarget("Code\\EntityInterfaces\\IUser.cs", rs);
                Assert.Equal(target, rs);
            }
        }
    }
}
