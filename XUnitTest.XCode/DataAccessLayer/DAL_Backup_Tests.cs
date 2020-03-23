using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Security;
using XCode;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.DataAccessLayer
{
    public class DAL_Backup_Tests
    {
        [Fact]
        public void BackupTest()
        {
            var n = UserX.Meta.Count;
            var dal = UserX.Meta.Session.Dal;
            var table = UserX.Meta.Table.DataTable;

            dal.Backup(table, $"data/{table.Name}.table");
            dal.Backup(table, $"data/{table.Name}.gz");
        }

        [Fact]
        public void BackupAllTest()
        {
            var n = UserX.Meta.Count;
            var dal = UserX.Meta.Session.Dal;
            var tables = EntityFactory.GetTables(dal.ConnName, false);

            dal.BackupAll(tables, $"data/{dal.ConnName}.zip");
        }

        [Fact]
        public void RestoreAllTest()
        {
            try
            {
                var dal = UserX.Meta.Session.Dal;

                // 随机连接名，得到SQLite连接字符串，实行导入
                UserX.Meta.ConnName = Rand.NextString(8);

                var dal2 = UserX.Meta.Session.Dal;

                var rs = dal2.RestoreAll($"data/{dal.ConnName}.zip", null);
                Assert.NotNull(rs);
            }
            finally
            {
                UserX.Meta.ConnName = null;
            }
        }
    }
}