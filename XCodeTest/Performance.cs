using System;
using System.Diagnostics;
using NewLife.Reflection;
using XCode.DataAccessLayer;
using NewLife.CommonEntity;
using XCode;
using System.Data.Common;
using System.Data;
using NewLife.Log;

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
            //String connStr = "";
            //connStr = "Data Source=.;Initial Catalog=Common_Performance;Integrated Security=SSPI";
            //DAL.AddConnStr("Common", connStr, null, "mssql");

            Console.WriteLine("正在准备反向工程……");
            // 让通用实体库进入内存
            Administrator admin = Administrator.FindByKey(1);
            Role role = Role.FindByID(1);

            DAL dal = DAL.Create("Common");

            IEntityOperate eoAdmin = dal.CreateOperate("Administrator");
            eoAdmin = EntityFactory.CreateOperate("Administrator");

            //// 清理数据表
            //dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Administrator");
            //dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Role");
            //dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Menu");
            //dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "RoleMenu");
            //DatabaseSchema.Check(dal.Db);

            Param p = new Param();
            p.Dal = dal;
            DbConnection conn = p.Conn;
            conn.Open();
            conn.Close();

            Console.WriteLine("准备工作完成，任意键开始！");
            Console.ReadKey(true);
            Console.Clear();

            p.Cmd = p.Conn.CreateCommand();
            p.Cmd.CommandText = "Insert Into Administrator(Name, Password, DisplayName, RoleID, Logins, LastLogin, LastLoginIP, SSOUserID, IsEnable) Values(@Name, null, null, @RoleID, 0, null, null, 0, 0)";

            p.UserTrans = false;
            InsertTest(1000, p);

            p.UserTrans = true;
            InsertTest(1000, p);

            //p.UserTrans = false;
            //InsertTest(10000, p);

            p.UserTrans = true;
            InsertTest(10000, p);

            //p.UserTrans = false;
            //InsertTest(100000, p);

            //p.UserTrans = true;
            //InsertTest(100000, p);
        }

        static void InsertTest(Int32 count, Param p)
        {
            // 清理数据表
            DAL dal = p.Dal;
            dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Administrator");
            dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Role");
            dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Menu");
            dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "RoleMenu");
            DatabaseSchema.Check(dal.Db);

            Console.WriteLine();
            Console.WriteLine("插入测试（{0}管理员、{1}角色）{2}：", count, count / AdminNumPerRole, p.UserTrans ? "使用事务" : "");

            CodeTimer tsBase;
            CodeTimer ts;

            ts = Test("ADO.SQL：", ADONetInsert, count, p);
            tsBase = ts;
            ShowPercent(tsBase, ts);
            ts = Test("ADO.Param：", ADONetParamInsert, count, p);
            ShowPercent(tsBase, ts);
            ts = Test("DAL：", DALTestInsert, count, p);
            ShowPercent(tsBase, ts);
            ts = Test("DALIdentity：", DALInsertAndGetIdentity, count, p);
            ShowPercent(tsBase, ts);
            ts = Test("Entity：", EntityInsert, count, p);
            ShowPercent(tsBase, ts);
            ts = Test("WeakEntity：", WeakEntityInsert, count, p);
            ShowPercent(tsBase, ts);
            ts = Test("DynEntity：", DynEntityInsert, count, p, false);// 这次插入的数据不清理，留作后面查询测试使用
            ShowPercent(tsBase, ts);

            Console.WriteLine("查询测试（{0}管理员、{1}角色）{2}：", count, count / AdminNumPerRole, p.UserTrans ? "使用事务" : "");

            ts = Test("ADO.SQL：", ADONetQuery, count, p, false);
            tsBase = ts;
            ShowPercent(tsBase, ts);
            ts = Test("ADO.Param：", ADONetParamQuery, count, p, false);
            ShowPercent(tsBase, ts);
            ts = Test("DAL：", DALTestQuery, count, p, false);
            ShowPercent(tsBase, ts);
            ts = Test("Entity：", EntityQuery, count, p, false);
            ShowPercent(tsBase, ts);
            ts = Test("WeakEntity：", WeakEntityQuery, count, p, false);
            ShowPercent(tsBase, ts);
            ts = Test("DynEntity：", DynEntityQuery, count, p, false);
            ShowPercent(tsBase, ts);
        }

        static void ShowPercent(CodeTimer tsBase, CodeTimer ts)
        {
            Console.WriteLine(" {0:n2} {1:n2}", (Double)ts.Elapsed.Ticks / tsBase.Elapsed.Ticks, (Double)ts.ThreadTime / tsBase.ThreadTime);
        }

        #region 辅助
        static CodeTimer Test(String title, Func<Param, Int32, Int32> fun, Int32 times, Param p, Boolean isClearData = true)
        {
            Console.Write("{0,12} ", title);
            p.Times = times;

            PerfTest timer = new PerfTest();
            timer.p = p;
            timer.Fun = fun;
            timer.IsADO = title.StartsWith("ADO");
            timer.IsClearData = isClearData;

            timer.Times = times;
            timer.ShowProgress = true;

            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            timer.TimeOne();
            timer.Time();

            Console.Write(timer.ToString());

            Console.ForegroundColor = currentForeColor;

            return timer;
        }

        class Param
        {
            public DAL Dal;

            private DbConnection _Conn;
            public DbConnection Conn { get { return _Conn ?? (_Conn = Dal.Db.CreateSession().Conn); } }

            public DbCommand Cmd;

            public Boolean UserTrans = false;

            public Int32 Times;
        }

        class PerfTest : CodeTimer
        {
            #region 业务
            public Param p;
            public Func<Param, Int32, Int32> Fun;
            public Boolean IsClearData = true;

            private Boolean _IsADO;
            /// <summary>是否ADO</summary>
            public Boolean IsADO
            {
                get { return _IsADO; }
                set { _IsADO = value; }
            }

            void Open()
            {
                if (IsADO)
                {
                    p.Conn.Open();
                    if (p.UserTrans) p.Cmd.Transaction = p.Conn.BeginTransaction();
                }
                else
                {
                    if (p.UserTrans) p.Dal.BeginTransaction();
                }
            }

            void Close()
            {
                if (IsADO)
                {
                    if (p.UserTrans) p.Cmd.Transaction.Commit();
                    p.Conn.Close();
                    p.Cmd.Transaction = null;
                }
                else
                {
                    if (p.UserTrans) p.Dal.Commit();
                }
            }
            #endregion

            #region 重载
            public override void Time()
            {
                if (IsClearData)
                {
                    // 清理数据表
                    DAL dal = p.Dal;
                    dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Administrator");
                    dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Role");
                    dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Menu");
                    dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "RoleMenu");
                    DatabaseSchema.Check(dal.Db);
                }

                base.Time();
            }

            public override void Init()
            {
                //base.Init();

                Open();
            }

            public override void Finish()
            {
                //base.Finish();

                Close();
            }

            public override void Time(int index)
            {
                //base.Time(index);

                Fun(p, index);
            }
            #endregion
        }
        #endregion

        #region 插入
        const Int32 AdminNumPerRole = 20;

        static Int32 lastRoleID = 0;

        static Int32 ADONetInsert(Param p, Int32 index)
        {
            String sql1 = String.Format("SET NOCOUNT ON;Insert Into [Role](Name) Values('EntityRole_{0}');Select SCOPE_IDENTITY()", index);
            if (index % AdminNumPerRole == 0)
            {
                DbCommand cmd1 = p.Conn.CreateCommand();
                cmd1.Transaction = p.Cmd.Transaction;
                cmd1.CommandText = sql1;
                lastRoleID = Convert.ToInt32(cmd1.ExecuteScalar());
            }

            String sql2 = String.Format("Insert Into Administrator(Name, Password, DisplayName, RoleID, Logins, LastLogin, LastLoginIP, SSOUserID, IsEnable) Values('EntityAdmin_{0}', null, null, {1}, 0, null, null, 0, 0)", index, lastRoleID);
            DbCommand cmd2 = p.Conn.CreateCommand();
            cmd2.Transaction = p.Cmd.Transaction;
            cmd2.CommandText = sql2;
            return cmd2.ExecuteNonQuery();
        }

        static Int32 ADONetParamInsert(Param p, Int32 index)
        {
            String sql1 = String.Format("SET NOCOUNT ON;Insert Into [Role](Name) Values('EntityRole_{0}');Select SCOPE_IDENTITY()", index);
            if (index % AdminNumPerRole == 0)
            {
                DbCommand cmd1 = p.Conn.CreateCommand();
                cmd1.Transaction = p.Cmd.Transaction;
                cmd1.CommandText = sql1;
                lastRoleID = Convert.ToInt32(cmd1.ExecuteScalar());
            }

            //String sql2 = String.Format("Insert Into Administrator(Name, Password, DisplayName, RoleID, Logins, LastLogin, LastLoginIP, SSOUserID, IsEnable) Values('EntityAdmin_{0}', null, null, {1}, 0, null, null, 0, 0)", index, lastRoleID);
            //DbCommand cmd2 = p.Conn.CreateCommand();
            //cmd2.CommandText = sql2;

            DbCommand cmd2 = p.Cmd;
            cmd2.Parameters.Clear();

            DbParameter dp1 = cmd2.CreateParameter();
            //dp1.DbType = DbType.String;
            dp1.ParameterName = "@Name";
            dp1.Value = String.Format("EntityAdmin_{0}", index);
            cmd2.Parameters.Add(dp1);

            DbParameter dp2 = cmd2.CreateParameter();
            //dp2.DbType = DbType.Int32;
            dp2.ParameterName = "@RoleID";
            dp2.Value = lastRoleID;
            cmd2.Parameters.Add(dp2);

            return cmd2.ExecuteNonQuery();
        }

        static Int32 DALTestInsert(Param p, Int32 index)
        {
            String sql1 = String.Format("Insert Into [Role](Name) Values('EntityRole_{0}')", index);
            if (index % AdminNumPerRole == 0) lastRoleID = (Int32)p.Dal.InsertAndGetIdentity(sql1, "Role");

            String sql2 = String.Format("Insert Into Administrator(Name, Password, DisplayName, RoleID, Logins, LastLogin, LastLoginIP, SSOUserID, IsEnable) Values('EntityAdmin_{0}', null, null, {1}, 0, null, null, 0, 0)", index, lastRoleID);
            return p.Dal.Execute(sql2, "Administrator");
        }

        static Int32 DALInsertAndGetIdentity(Param p, Int32 index)
        {
            String sql1 = String.Format("Insert Into [Role](Name) Values('EntityRole_{0}')", index);
            if (index % AdminNumPerRole == 0) lastRoleID = (Int32)p.Dal.InsertAndGetIdentity(sql1, "Role");

            String sql2 = String.Format("Insert Into Administrator(Name, Password, DisplayName, RoleID, Logins, LastLogin, LastLoginIP, SSOUserID, IsEnable) Values('EntityAdmin_{0}', null, null, {1}, 0, null, null, 0, 0)", index, lastRoleID);
            return (Int32)p.Dal.InsertAndGetIdentity(sql2, "Administrator");
        }

        static Int32 EntityInsert(Param p, Int32 index)
        {
            if (index % AdminNumPerRole == 0)
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

        static Int32 WeakEntityInsert(Param p, Int32 index)
        {
            if (index % AdminNumPerRole == 0)
            {
                IEntityOperate eoRole = EntityFactory.CreateOperate("Role");

                IEntity role = eoRole.Create();
                role["Name"] = "EntityRole_" + index;
                role.Save();

                lastRoleID = (Int32)role["ID"];
            }

            IEntityOperate eoAdmin = EntityFactory.CreateOperate("Administrator");

            IEntity admin = eoAdmin.Create();
            admin["Name"] = "EntityAdmin_" + index;
            admin["RoleID"] = lastRoleID;
            return admin.Save();
        }

        static Int32 DynEntityInsert(Param p, Int32 index)
        {
            if (index % AdminNumPerRole == 0)
            {
                IEntityOperate eoRole = p.Dal.CreateOperate("Role");

                IEntity role = eoRole.Create();
                role["Name"] = "EntityRole_" + index;
                role.Save();

                lastRoleID = (Int32)role["ID"];
            }

            IEntityOperate eoAdmin = p.Dal.CreateOperate("Administrator");

            IEntity admin = eoAdmin.Create();
            admin["Name"] = "EntityAdmin_" + index;
            admin["RoleID"] = lastRoleID;
            return admin.Save();
        }
        #endregion

        #region 查询
        /// <summary>
        /// 数据插入是顺序的，查询的时候随机查
        /// </summary>
        static Random Rnd = new Random((Int32)DateTime.Now.Ticks);

        static Int32 ADONetQuery(Param p, Int32 index)
        {
            String sql1 = "select * from Administrator where ID=" + (Rnd.Next(0, p.Times) + 1);

            DbCommand cmd1 = p.Conn.CreateCommand();
            cmd1.Transaction = p.Cmd.Transaction;
            cmd1.CommandText = sql1;
            DataSet ds1 = Fill(p, cmd1);

            Int32 id = (Int32)ds1.Tables[0].Rows[0]["RoleID"];

            String sql2 = "select * from Role where ID=" + id;
            DbCommand cmd2 = p.Conn.CreateCommand();
            cmd2.Transaction = p.Cmd.Transaction;
            cmd2.CommandText = sql2;
            DataSet ds2 = Fill(p, cmd2);

            return 0;
        }

        static Int32 ADONetParamQuery(Param p, Int32 index)
        {
            String sql1 = "select * from Administrator where ID=@ID";

            DbCommand cmd1 = p.Conn.CreateCommand();
            cmd1.Transaction = p.Cmd.Transaction;
            cmd1.CommandText = sql1;

            DbParameter dp1 = cmd1.CreateParameter();
            dp1.ParameterName = "@ID";
            dp1.Value = Rnd.Next(0, p.Times) + 1;
            cmd1.Parameters.Add(dp1);

            DataSet ds1 = Fill(p, cmd1);

            Int32 id = (Int32)ds1.Tables[0].Rows[0]["RoleID"];

            String sql2 = "select * from Role where ID=@ID";
            DbCommand cmd2 = p.Conn.CreateCommand();
            cmd2.Transaction = p.Cmd.Transaction;
            cmd2.CommandText = sql2;

            DbParameter dp2 = cmd2.CreateParameter();
            dp2.ParameterName = "@ID";
            dp2.Value = id;
            cmd2.Parameters.Add(dp2);

            DataSet ds2 = Fill(p, cmd2);

            return 0;
        }

        static DataSet Fill(Param p, DbCommand cmd)
        {
            using (DbDataAdapter da = p.Dal.Db.Factory.CreateDataAdapter())
            {
                da.SelectCommand = cmd;
                DataSet ds = new DataSet();
                da.Fill(ds);
                return ds;
            }
        }

        static Int32 DALTestQuery(Param p, Int32 index)
        {
            //String sql1 = "Select * From Administrator Where ID=" + (Rnd.Next(0, p.Times) + 1);
            //String sql1 = String.Format("Select Top 1 * From Administrator Where ID={0} Order By ID Desc", Rnd.Next(0, p.Times) + 1);

            SelectBuilder builder1 = new SelectBuilder();
            builder1.Table = "Administrator";
            builder1.Where = "ID=" + (Rnd.Next(0, p.Times) + 1);
            builder1.OrderBy = "ID Desc";
            String sql1 = p.Dal.PageSplit(builder1, 0, 1, "ID");

            DataSet ds1 = p.Dal.Select(sql1, "Administrator");

            Int32 id = (Int32)ds1.Tables[0].Rows[0]["RoleID"];

            //String sql2 = "Select * From Role Where ID=" + id;
            //String sql2 = String.Format("Select Top 1 * From Role Where ID={0} Order By ID Desc", id);

            SelectBuilder builder2 = new SelectBuilder();
            builder2.Table = "Role";
            builder2.Where = "ID=" + id;
            builder2.OrderBy = "ID Desc";
            String sql2 = p.Dal.PageSplit(builder2, 0, 1, "ID");
            
            DataSet ds2 = p.Dal.Select(sql2, "Role");

            return 0;
        }

        static Int32 EntityQuery(Param p, Int32 index)
        {
            Administrator admin = Administrator.FindByKey(Rnd.Next(0, p.Times) + 1);
            //if (admin == null) return 0;

            Role role = admin.Role;
            return role == null ? 0 : role.ID;
        }

        static Int32 WeakEntityQuery(Param p, Int32 index)
        {
            IEntityOperate eoAdmin = EntityFactory.CreateOperate("Administrator");
            IEntityOperate eoRole = EntityFactory.CreateOperate("Role");

            IEntity admin = eoAdmin.FindByKey(Rnd.Next(0, p.Times) + 1);
            //if (admin == null) return 0;

            IEntity role = admin["Role"] as IEntity;
            return role == null ? 0 : (Int32)role["ID"];
        }

        static Int32 DynEntityQuery(Param p, Int32 index)
        {
            IEntityOperate eoAdmin = p.Dal.CreateOperate("Administrator");
            IEntityOperate eoRole = p.Dal.CreateOperate("Role");

            IEntity admin = eoAdmin.FindByKey(Rnd.Next(0, p.Times) + 1);
            //if (admin == null) return 0;

            IEntity role = eoRole.FindByKey(admin["RoleID"]);
            return role == null ? 0 : (Int32)role["ID"];
        }
        #endregion
    }
}
