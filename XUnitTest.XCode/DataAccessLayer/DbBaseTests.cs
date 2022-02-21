using System;
using NewLife.Model;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class DbBaseTests
    {
        static DbBaseTests()
        {
            DAL.WriteLog("Init DbBaseTests");
        }

        [Theory]
        [InlineData("Name", "name")]
        [InlineData("NickName", "nick_name")]
        [InlineData("ID", "id")]
        [InlineData("Id", "id")]
        [InlineData("id", "id")]
        [InlineData("ProductID", "product_id")]
        [InlineData("CreateIP", "create_ip")]
        //[InlineData("IPStart", "ip_start")]
        [InlineData("RoleID", "role_id")]
        [InlineData("RoleIds", "role_ids")]
        [InlineData("LastLoginIP", "last_login_ip")]
        public void FormatUnderlineName(String name, String result)
        {
            var table = ObjectContainer.Current.Resolve<IDataTable>();
            table.Name = name;
            table.TableName = name;

            var db = DbFactory.Create(DatabaseType.SQLite);
            db.NameFormat = NameFormats.Underline;

            var rs = db.FormatName(table);
            Assert.Equal(result, rs);
        }

        [Fact]
        public void FormatUnderlineName2()
        {
            var table = ObjectContainer.Current.Resolve<IDataTable>();
            table.Name = "OAuthConfig";
            table.TableName = "OAuthConfig";

            var db = DbFactory.Create(DatabaseType.SQLite);
            db.NameFormat = NameFormats.Underline;

            var rs = db.FormatName(table);
            Assert.Equal("oauth_config", rs);
        }
    }
}
