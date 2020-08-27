using System;
using System.Collections.Generic;
using System.Text;
using XCode.Code;
using XCode.Membership;
using Xunit;
using System.IO;

namespace XUnitTest.XCode.Code
{
    public class ClassBuilderTests
    {
        [Fact]
        public void Normal()
        {
            var builder = new ClassBuilder();
            builder.Table = UserX.Meta.Table.DataTable;
            builder.Namespace = "Company.MyName";
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
            var builder = new ClassBuilder();
            builder.Table = UserX.Meta.Table.DataTable;
            builder.BaseClass = "MyEntityBase";

            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\class_user_baseclass.cs".GetFullPath());
            Assert.Equal(target, rs);
        }

        [Fact]
        public void Pure()
        {
            var builder = new ClassBuilder();
            builder.Table = UserX.Meta.Table.DataTable;
            builder.Pure = true;

            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\class_user_pure.cs".GetFullPath());
            Assert.Equal(target, rs);
        }

        [Fact]
        public void Interface()
        {
            var builder = new ClassBuilder();
            builder.Table = UserX.Meta.Table.DataTable;
            builder.Interface = true;

            builder.Execute();

            var rs = builder.ToString();
            Assert.NotEmpty(rs);

            var target = File.ReadAllText("Code\\class_user_interface.cs".GetFullPath());
            Assert.Equal(target, rs);
        }
    }
}