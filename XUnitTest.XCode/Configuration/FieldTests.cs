using System;
using System.Collections.Generic;
using System.Text;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Configuration
{
    public class FieldTests
    {
        [Fact(DisplayName = "基础测试")]
        public void BasicTest()
        {
            var fi = User._.Password;
            Assert.Equal("Password", fi.Name);
            Assert.Equal("密码", fi.DisplayName);
            Assert.Equal("密码", fi.Description);
            Assert.Equal(typeof(String), fi.Type);
            Assert.False(fi.IsIdentity);
            Assert.False(fi.PrimaryKey);
            Assert.False(fi.Master);
            Assert.True(fi.IsNullable);
            Assert.Equal(200, fi.Length);
            Assert.True(fi.IsDataObjectField);
            Assert.False(fi.IsDynamic);
            Assert.Equal("Password", fi.ColumnName);
            Assert.False(fi.ReadOnly);
            Assert.NotNull(fi.Table);
            Assert.NotNull(fi.Field);
            Assert.NotNull(fi.Factory);
            Assert.Equal("Password", fi.FormatedName);
            Assert.Null(fi.OriField);
            Assert.Null(fi.Map);

            Assert.True(User._.ID.IsIdentity);
            Assert.True(User._.ID.PrimaryKey);
            Assert.True(User._.Name.Master);
            Assert.False(User._.Name.IsNullable);

            fi = User.Meta.Table.FindByName("DepartmentName");
            Assert.NotNull(fi);
            Assert.NotNull(fi.OriField);
            Assert.Equal("DepartmentID", fi.OriField.Name);
            Assert.NotNull(fi.Map);
        }
    }
}