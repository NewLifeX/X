using System;
using System.Collections.Generic;
using System.Text;
using XCode;
using XCode.Membership;
using XCode.Model;
using Xunit;

namespace XUnitTest.XCode.Model
{
    public class WhereBuilderTests
    {
        [Fact(DisplayName = "普通表达式解析")]
        public void Parse()
        {
            var dic = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
            {
                ["userid"] = 1234
            };
            var builder = new WhereBuilder
            {
                Factory = Log.Meta.Factory,
                Expression = "CreateUserID={$userId}",
                Data = dic,
            };

            var exp = builder.GetExpression();
            Assert.NotNull(exp);

            var fe = exp as FieldExpression;
            Assert.NotNull(fe);
            Assert.Equal(Log._.CreateUserID, fe.Field);
            Assert.Equal("=", fe.Action);
            Assert.Equal(1234, fe.Value);
        }

        [Fact(DisplayName = "无数据源")]
        public void ParseNoData()
        {
            var builder = new WhereBuilder
            {
                Factory = Log.Meta.Factory,
                Expression = "CreateUserID={$userId}",
            };

            var ex = Assert.Throws<ArgumentException>("Data", () => builder.GetExpression());
            Assert.NotNull(ex);
        }

        [Fact(DisplayName = "数据源变量大小写")]
        public void ParseNoData2()
        {
            // 变量大小写
            var dic = new Dictionary<String, Object>()
            {
                ["userid"] = 1234
            };
            var builder = new WhereBuilder
            {
                Factory = Log.Meta.Factory,
                Expression = "CreateUserID={#userId}",
                Data2 = dic,
            };

            // 变量大小写
            var ex = Assert.Throws<ArgumentException>("Data2", () => builder.GetExpression());
            Assert.NotNull(ex);
        }

        [Fact(DisplayName = "无字段")]
        public void ParseNoField()
        {
            var builder = new WhereBuilder
            {
                Factory = Log.Meta.Factory,
                Expression = "UserID={$userId}",
            };

            var ex = Assert.Throws<XCodeException>(() => builder.GetExpression());
            Assert.NotNull(ex);
        }

        [Fact(DisplayName = "多层变量")]
        public void ParseVar()
        {
            var user = new UserX { ID = 1234 };
            var dic = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
            {
                ["user"] = user
            };
            var builder = new WhereBuilder
            {
                Factory = Log.Meta.Factory,
                Expression = "CreateUserID={$User.ID}",
                Data = dic,
            };

            var exp = builder.GetExpression();
            Assert.NotNull(exp);

            var fe = exp as FieldExpression;
            Assert.NotNull(fe);
            Assert.Equal(Log._.CreateUserID, fe.Field);
            Assert.Equal("=", fe.Action);
            Assert.Equal(1234, fe.Value);
        }

        [Fact(DisplayName = "高级或运算")]
        public void ParseOr()
        {
            var dic = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
            {
                ["SiteIds"] = new[] { 2, 4, 8, 16 },
                ["userid"] = 1234
            };
            var builder = new WhereBuilder
            {
                Factory = Log.Meta.Factory,
                Expression = "linkid in{#SiteIds} or CreateUserID={#userId}",
                Data2 = dic,
            };

            var exp = builder.GetExpression();
            Assert.NotNull(exp);

            var where = exp as WhereExpression;
            Assert.NotNull(where);
            Assert.Equal(Operator.Or, where.Operator);

            var left = where.Left as FormatExpression;
            Assert.NotNull(left);
            Assert.Equal(Log._.LinkID, left.Field);
            Assert.Equal("{0} In({1})", left.Format);
            Assert.Equal(dic["SiteIds"], left.Value);
        }

        [Fact(DisplayName = "复杂比较操作")]
        public void ParseUnknown()
        {
            var user = new UserX { ID = 1234 };
            var dic = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
            {
                ["user"] = user
            };
            var builder = new WhereBuilder
            {
                Factory = Log.Meta.Factory,
                Expression = "CreateUserID>={$User.ID}",
                Data = dic,
            };

            var exp = builder.GetExpression();
            Assert.NotNull(exp);
            Assert.Equal("CreateUserID>=1234", exp.Text);
        }
    }
}