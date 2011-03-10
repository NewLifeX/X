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
            String connStr = "";
            connStr = "Data Source=.;Initial Catalog=Common_Performance;Integrated Security=SSPI";
            DAL.AddConnStr("Common", connStr, null, "mssql");

            Console.WriteLine("正在准备反向工程……");
            // 让通用实体库进入内存
            Administrator admin = Administrator.FindByKey(1);
            Role role = Role.FindByID(1);

            DAL dal = DAL.Create("Common");

            // 清理数据表
            dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Administrator");
            dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Role");
            dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "Menu");
            dal.Db.CreateMetaData().SetSchema(DDLSchema.DropTable, "RoleMenu");
            DatabaseSchema.Check(dal.Db);

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

            p.UserTrans = false;
            InsertTest(10000, p);

            p.UserTrans = true;
            InsertTest(10000, p);

            p.UserTrans = false;
            InsertTest(100000, p);

            p.UserTrans = true;
            InsertTest(100000, p);
        }

        static void InsertTest(Int32 count, Param p)
        {
            Console.WriteLine();
            Console.WriteLine("插入测试（插入{0}管理员、{1}角色）{2}：", count, count / 1000, p.UserTrans ? "使用事务" : "");

            TimeSpan tsBase;
            TimeSpan ts;

            ts = Test("ADO.Param：", ADONetParamInsert, count, p);
            tsBase = ts;
            Console.WriteLine(" {0:n2}", (Double)ts.Ticks / tsBase.Ticks);

            ts = Test("ADO.SQL：", ADONetInsert, count, p);
            Console.WriteLine(" {0:n2}", (Double)ts.Ticks / tsBase.Ticks);

            ts = Test("DAL：", DALTestInsert, count, p);
            Console.WriteLine(" {0:n2}", (Double)ts.Ticks / tsBase.Ticks);
            ts = Test("Entity：", EntityInsert, count, p);
            Console.WriteLine(" {0:n2}", (Double)ts.Ticks / tsBase.Ticks);
            ts = Test("WeakEntity：", WeakEntityInsert, count, p);
            Console.WriteLine(" {0:n2}", (Double)ts.Ticks / tsBase.Ticks);
        }

        #region 辅助
        static Int32 lastLeft = 0;
        static TimeSpan Test0(String title, Func<Param, Int32, Int32> fun, Int32 times, Param p)
        {
            Console.Write("{0,12} ", title);
            lastLeft = Console.CursorLeft;

            // 预热
            if (title.StartsWith("ADO")) p.Conn.Open();
            fun(p, -1);
            if (title.StartsWith("ADO")) p.Conn.Close();

            //DbTransaction trans = null;

            // 开始
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (title.StartsWith("ADO"))
            {
                p.Conn.Open();
                if (p.UserTrans) p.Cmd.Transaction = p.Conn.BeginTransaction();
            }
            else
            {
                if (p.UserTrans) p.Dal.BeginTransaction();
            }

            for (int i = 0; i < times; i++)
            {
                if (i % 500 == 0)
                {
                    Double d = (Double)i / times;
                    Console.CursorLeft = lastLeft;
                    Console.Write("{0:p}", d);

                    if (p.UserTrans && i % 10000 == 0)
                    {
                        if (title.StartsWith("ADO"))
                        {
                            p.Cmd.Transaction.Commit();
                            p.Cmd.Transaction = p.Conn.BeginTransaction();
                        }
                        else
                        {
                            p.Dal.Commit();
                            p.Dal.BeginTransaction();
                        }
                    }
                }

                fun(p, i);
            }

            if (title.StartsWith("ADO"))
            {
                if (p.UserTrans) p.Cmd.Transaction.Commit();
                p.Conn.Close();
                p.Cmd.Transaction = null;
            }
            else
            {
                if (p.UserTrans) p.Dal.Commit();
            }

            sw.Stop();

            Console.CursorLeft = lastLeft;

            Console.Write("{0}", sw.Elapsed);

            return sw.Elapsed;
        }

        static TimeSpan Test(String title, Func<Param, Int32, Int32> fun, Int32 times, Param p)
        {
            Console.Write("{0,12} ", title);
            lastLeft = Console.CursorLeft;
            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            Boolean isADO = title.StartsWith("ADO");

            long cpu;
            Int32[] gen;
            TimeSpan ts = CodeTimer.Time(times, delegate(Int32 i)
            {
                if (i == -1)
                {
                    if (isADO) p.Conn.Open();
                    fun(p, i);
                    if (isADO) p.Conn.Close();
                    return;
                }
                else if (i == 0)
                {
                    if (isADO)
                    {
                        p.Conn.Open();
                        if (p.UserTrans) p.Cmd.Transaction = p.Conn.BeginTransaction();
                    }
                    else
                    {
                        if (p.UserTrans) p.Dal.BeginTransaction();
                    }
                }
                else if (i != times - 1 && i % 500 == 0)
                {
                    Double d = (Double)i / times;
                    Console.CursorLeft = lastLeft;
                    Console.Write("{0:p}", d);

                    if (p.UserTrans && i % 10000 == 0)
                    {
                        if (isADO)
                        {
                            p.Cmd.Transaction.Commit();
                            p.Cmd.Transaction = p.Conn.BeginTransaction();
                        }
                        else
                        {
                            p.Dal.Commit();
                            p.Dal.BeginTransaction();
                        }
                    }
                }

                fun(p, i);

                if (i == times - 1)
                {
                    if (isADO)
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
            }, out cpu, out gen);

            Console.CursorLeft = lastLeft;

            Console.Write(CodeTimer.Format(ts, cpu, gen));

            Console.ForegroundColor = currentForeColor;

            return ts;
        }

        class Param
        {
            public DAL Dal;

            private DbConnection _Conn;
            public DbConnection Conn { get { return _Conn ?? (_Conn = Dal.Db.CreateSession().Conn); } }

            public DbCommand Cmd;

            public Boolean UserTrans = false;
        }
        #endregion

        #region 插入
        static Int32 lastRoleID = 0;

        static Int32 ADONetInsert(Param p, Int32 index)
        {
            String sql1 = String.Format("SET NOCOUNT ON;Insert Into [Role](Name) Values('EntityRole_{0}');Select SCOPE_IDENTITY()", index);
            if (index % 1000 == 0)
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
            if (index % 1000 == 0)
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
            if (index % 1000 == 0) lastRoleID = (Int32)p.Dal.InsertAndGetIdentity(sql1, "Role");

            String sql2 = String.Format("Insert Into Administrator(Name, Password, DisplayName, RoleID, Logins, LastLogin, LastLoginIP, SSOUserID, IsEnable) Values('EntityAdmin_{0}', null, null, {1}, 0, null, null, 0, 0)", index, lastRoleID);
            return p.Dal.Execute(sql2, "Administrator");
        }

        static Int32 EntityInsert(Param p, Int32 index)
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

        static Int32 WeakEntityInsert(Param p, Int32 index)
        {
            if (index % 1000 == 0)
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
        static Int32 ADONetQuery(Param p, Int32 index)
        {
            return 0;
        }

        static Int32 DALTestQuery(Param p, Int32 index)
        {
            return 0;
        }

        static Int32 EntityQuery(Param p, Int32 index)
        {
            return 0;
        }

        static Int32 WeakEntityQuery(Param p, Int32 index)
        {
            return 0;
        }
        #endregion
    }
}
