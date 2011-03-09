using System;
using System.Diagnostics;
using NewLife.Reflection;
using XCode.DataAccessLayer;
using NewLife.CommonEntity;
using XCode;

namespace XCodeTest
{
    /// <summary>
    /// 性能
    /// </summary>
    static class Performance
    {
        public static void Start()
        {
            // 准备数据库环境
            String connStr = "";
            connStr = "Data Source=.;Initial Catalog=Common_Performance;Integrated Security=SSPI";
            DAL.AddConnStr("Common", connStr, null, "mssql");
            DAL dal = DAL.Create("Common");

            Console.WriteLine("插入测试（插入10万管理员、100角色）：");
            //Console.WriteLine("ADO.Net：{0}", Test(ADONetInsert, 100000, dal));
            //Console.WriteLine("ADO.Net：{0}", Test(ADONetInsert, 100000, dal));
            Console.WriteLine("Entity：{0}", Test(EntityInsert, 100000, dal));
            Console.WriteLine("WeakEntity：{0}", Test(WeakEntityInsert, 100000, dal));
        }

        #region 辅助
        static TimeSpan Test(Func<DAL, Int32, Int32> fun, Int32 times, DAL dal)
        {
            // 预热
            fun(dal, -1);

            // 开始
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < times; i++)
            {
                fun(dal, i);
            }
            sw.Stop();
            return sw.Elapsed;
        }
        #endregion

        #region 插入
        static Int32 lastRoleID = 0;

        static Int32 ADONetInsert(DAL dal, Int32 index)
        {
            String sql = "SET NOCOUNT ON;Insert Into Log(Category, [Action], UserID, UserName, IP, OccurTime, Remark) Values('管理员', '添加', 0, null, null, {ts'2011-03-09 09:32:47'}, 'EntityAdmin_-1');Select SCOPE_IDENTITY()";
            return 0;
        }

        static Int32 DALTestInsert(DAL dal, Int32 index)
        {
            return 0;
        }

        static Int32 EntityInsert(DAL dal, Int32 index)
        {
            if (index % 1000 == 0)
            {
                Role role = new Role();
                role.Name = "EntityRole_" + index;
                role.Save();

                lastRoleID = role.ID;
            }

            Administrator admin = new Administrator();
            admin.Name = "EntityAdmin_" + index;
            admin.RoleID = lastRoleID;
            return admin.Save();
        }

        static Int32 WeakEntityInsert(DAL dal, Int32 index)
        {
            if (index % 1000 == 0)
            {
                IEntityOperate eoRole = dal.CreateOperate("Role");

                IEntity role = eoRole.Create();
                role["Name"] = "EntityRole_" + index;
                role.Save();

                lastRoleID = (Int32)role["ID"];
            }

            IEntityOperate eoAdmin = dal.CreateOperate("Administrator");

            IEntity admin = eoAdmin.Create();
            admin["Name"] = "EntityAdmin_" + index;
            admin["RoleID"] = lastRoleID;
            return admin.Save();
        }
        #endregion

        #region 查询
        static Int32 ADONetQuery(DAL dal, Int32 index)
        {
            return 0;
        }

        static Int32 DALTestQuery(DAL dal, Int32 index)
        {
            return 0;
        }

        static Int32 EntityQuery(DAL dal, Int32 index)
        {
            return 0;
        }

        static Int32 WeakEntityQuery(DAL dal, Int32 index)
        {
            return 0;
        }
        #endregion
    }
}
