using System;
using System.Collections.Generic;
using System.Text;
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
    }
}