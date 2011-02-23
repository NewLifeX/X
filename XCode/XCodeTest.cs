using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
using NewLife.Log;
using XCode.Code;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>
    /// 测试
    /// </summary>
    public class XCodeTest
    {
        #region 静态
        /// <summary>
        /// 多线程
        /// </summary>
        /// <param name="num">线程数</param>
        public static void MulThread(Int32 num)
        {
            //打开多个线程去测试
            if (num < 1) num = 20;

            List<AutoResetEvent> events = new List<AutoResetEvent>();

            for (int i = 0; i < num; i++)
            {
                Thread thread = new Thread(SingleWrap);
                thread.Name = String.Format("XCode测试线程{0}", i + 1);
                thread.IsBackground = true;
                AutoResetEvent e = new AutoResetEvent(false);
                thread.Start(new Object[] { e, i + 1 });
                events.Add(e);
            }

            if (!WaitHandle.WaitAll(events.ToArray(), 5 * 60 * 1000)) throw new Exception("超时！");
        }

        private static void SingleWrap(Object state)
        {
            Object[] objs = (Object[])state;

            XCodeTest test = new XCodeTest();
            test.ID = (Int32)objs[1];

            try
            {
                test.Single();
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.Message);
            }
            finally
            {
                AutoResetEvent e = objs[0] as AutoResetEvent;
                if (e != null) e.Set();
            }
        }
        #endregion

        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        public Int32 ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        private DAL _Dal;
        /// <summary>数据层</summary>
        private DAL Dal
        {
            get { return _Dal; }
            set { _Dal = value; }
        }

        private IDbSession Session { get { return Dal.Session as DbSession; } }

        private IMetaData MetaData { get { return Dal.Db.CreateMetaData() as IMetaData; } }
        #endregion

        #region 测试
        /// <summary>
        /// 单线程
        /// </summary>
        public void Single()
        {
            Random rnd = new Random((Int32)DateTime.Now.Ticks);

            //选择数据库
            Int32 n = DAL.ConnStrs.Count;
            n = ID % n;
            Int32 m = 0;
            foreach (String item in DAL.ConnStrs.Keys)
            {
                if (m == n)
                {
                    Dal = DAL.Create(item);
                    break;
                }
                m++;
            }

            //测试数据库
            String dbName = Session.DatabaseName;

            Boolean dbExist = (Boolean)MetaData.SetSchema(DDLSchema.DatabaseExist, dbName);

            if (dbExist)
            {
                XTrace.WriteLine("删除数据库：{0}", dbName);
                MetaData.SetSchema(DDLSchema.DropDatabase, dbName);
            }

            //创建数据库
            //if (!dbExist)
            {
                XTrace.WriteLine("创建数据库：{0}", dbName);
                MetaData.SetSchema(DDLSchema.CreateDatabase, dbName, null);
            }

            //创建数据表
            XTable table = new XTable();
            table.Name = "xtest";
            table.DbType = DatabaseType.Access;
            table.Fields = new List<XField>();
            table.Description = "测试表";

            //检查数据表
            if ((Boolean)MetaData.SetSchema(DDLSchema.TableExist, table.Name))
            {
                XTrace.WriteLine("删除数据表：{0}", table.Name);
                MetaData.SetSchema(DDLSchema.DropTable, table.Name);
            }

            //创建字段
            #region 创建字段
            XField field = table.CreateField();
            field.ID = table.Fields.Count + 1;
            field.Name = "ID";
            field.DataType = typeof(Int32);
            field.Identity = true;
            field.PrimaryKey = true;
            field.Description = "编号";
            table.Fields.Add(field);

            field = table.CreateField();
            field.ID = table.Fields.Count + 1;
            field.Name = "Name";
            field.DataType = typeof(String);
            field.Length = 55;
            field.Nullable = false;
            field.Description = "名称";
            table.Fields.Add(field);

            field = table.CreateField();
            field.ID = table.Fields.Count + 1;
            field.Name = "ParentID";
            field.DataType = typeof(Int32);
            field.Nullable = false;
            field.Default = "99";
            field.Description = "父编号";
            table.Fields.Add(field);

            field = table.CreateField();
            field.ID = table.Fields.Count + 1;
            field.Name = "Value";
            field.DataType = typeof(Double);
            field.Scale = 10;
            field.Nullable = false;
            field.Default = "1.8";
            field.Description = "值";
            table.Fields.Add(field);

            field = table.CreateField();
            field.ID = table.Fields.Count + 1;
            field.Name = "OccurTime";
            field.DataType = typeof(DateTime);
            field.Nullable = false;
            field.Default = "now()";
            field.Description = "时间";
            table.Fields.Add(field);

            field = table.CreateField();
            field.ID = table.Fields.Count + 1;
            field.Name = "IsEnable";
            field.DataType = typeof(Boolean);
            field.Nullable = false;
            field.Default = "1";
            field.Description = "有效";
            table.Fields.Add(field);

            field = table.CreateField();
            field.ID = table.Fields.Count + 1;
            field.Name = "Description";
            field.DataType = typeof(String);
            field.Length = 5000;
            field.Nullable = true;
            field.Default = "无";
            field.Description = "描述";
            table.Fields.Add(field);

            XTrace.WriteLine("创建数据表：{0}", table.Name);
            MetaData.SetSchema(DDLSchema.CreateTable, table);
            #endregion

            //插入数据
            Int32 dataMax = rnd.Next(100, 300);
            XTrace.WriteLine("插入数据：{0}", dataMax);
            String sql = String.Empty;
            //是否使用事务
            Boolean EnableTran = rnd.Next(0, 2) == 0;
            if (EnableTran)
            {
                XTrace.WriteLine("使用事务！");
                Dal.BeginTransaction();
            }
            StringBuilder sb = new StringBuilder();
            Boolean debug_old = DbSession.Debug;
            DAL.Debug = false;
            for (int i = 0; i < dataMax; i++)
            {
                String name = "测试" + i.ToString("0000");
                sb.Append("无" + i + " ");
                String des = sb.ToString();
                Int32 pid = i % 99;

                if (pid == 0)
                {
                    sql = String.Format("Insert into {2}(Name, Description) values('{0}','{1}')", name, des, table.Name);
                }
                else if (i % 29 == 0)
                {
                    sql = String.Format("Insert into {2}(Name, ParentID) values('{0}',{1})", name, pid, table.Name);
                }
                else
                {
                    sql = String.Format("Insert into {3}(Name, ParentID, Description) values('{0}',{1},'{2}')", name, pid, des, table.Name);
                }

                Int64 n2 = Dal.InsertAndGetIdentity(sql, table.Name);
                if (i + 1 != n2) throw new Exception("插入返回编号不匹配！");
            }
            DAL.Debug = debug_old;
            if (EnableTran) Dal.Commit();

            //查询
            XTrace.WriteLine("查询测试！");

            XTrace.WriteLine("普通分页测试！");
            DataSet ds = Dal.Select("select * from xtest", 33, 44, "ID", "test");
            if (ds.Tables[0].Rows.Count != 44) throw new Exception("查询返回记录数不匹配！");
            if ((Int32)ds.Tables[0].Rows[0]["ID"] != 33 + 1) throw new Exception("查询返回记录不匹配！");
            if ((Int32)ds.Tables[0].Rows[43]["ID"] != 33 + 44) throw new Exception("查询返回记录不匹配！");

            ds = Dal.Select("select * from xtest order by name", 33, 44, "ID", "test");
            if (ds.Tables[0].Rows.Count != 44) throw new Exception("查询返回记录数不匹配！");
            if ((String)ds.Tables[0].Rows[0]["name"] != "测试" + (33).ToString("0000")) throw new Exception("查询返回记录不匹配！");
            if ((String)ds.Tables[0].Rows[43]["name"] != "测试" + (33 + 44 - 1).ToString("0000")) throw new Exception("查询返回记录不匹配！");

            XTrace.WriteLine("自增数字分页测试！");
            ds = Dal.Select("select * from xtest", 33, 44, "ID asc", "test");
            if (ds.Tables[0].Rows.Count != 44) throw new Exception("查询返回记录数不匹配！");
            if ((Int32)ds.Tables[0].Rows[0]["ID"] != 33 + 1) throw new Exception("查询返回记录不匹配！");
            if ((Int32)ds.Tables[0].Rows[43]["ID"] != 33 + 44) throw new Exception("查询返回记录不匹配！");

            ds = Dal.Select("select * from xtest order by id", 33, 44, "ID asc", "test");
            if (ds.Tables[0].Rows.Count != 44) throw new Exception("查询返回记录数不匹配！");
            if ((Int32)ds.Tables[0].Rows[0]["ID"] != 33 + 1) throw new Exception("查询返回记录不匹配！");
            if ((Int32)ds.Tables[0].Rows[43]["ID"] != 33 + 44) throw new Exception("查询返回记录不匹配！");

            ds = Dal.Select("select * from xtest order by id", 33, 44, "ID", "test");
            if (ds.Tables[0].Rows.Count != 44) throw new Exception("查询返回记录数不匹配！");
            if ((Int32)ds.Tables[0].Rows[0]["ID"] != 33 + 1) throw new Exception("查询返回记录不匹配！");
            if ((Int32)ds.Tables[0].Rows[43]["ID"] != 33 + 44) throw new Exception("查询返回记录不匹配！");

            ds = Dal.Select("select * from xtest order by id desc", 33, 44, "ID", "test");
            if (ds.Tables[0].Rows.Count != 44) throw new Exception("查询返回记录数不匹配！");
            if ((Int32)ds.Tables[0].Rows[0]["ID"] != dataMax - 33) throw new Exception("查询返回记录不匹配！");
            if ((Int32)ds.Tables[0].Rows[43]["ID"] != dataMax - 33 - 44 + 1) throw new Exception("查询返回记录不匹配！");

            ds = Dal.Select("select * from xtest order by id desc", 33, 44, "ID asc", "test");
            if (ds.Tables[0].Rows.Count != 44) throw new Exception("查询返回记录数不匹配！");
            if ((Int32)ds.Tables[0].Rows[0]["ID"] != dataMax - 33) throw new Exception("查询返回记录不匹配！");
            if ((Int32)ds.Tables[0].Rows[43]["ID"] != dataMax - 33 - 44 + 1) throw new Exception("查询返回记录不匹配！");

            ds = Dal.Select("select * from xtest order by name", 33, 44, "ID unknown", "test");
            if (ds.Tables[0].Rows.Count != 44) throw new Exception("查询返回记录数不匹配！");
            if ((String)ds.Tables[0].Rows[0]["name"] != "测试" + (33).ToString("0000")) throw new Exception("查询返回记录不匹配！");
            if ((String)ds.Tables[0].Rows[43]["name"] != "测试" + (33 + 44 - 1).ToString("0000")) throw new Exception("查询返回记录不匹配！");

            //n = Dal.SelectCount("select * from xtest", 33, 44, "ID desc", "test");
            //if (n != 44) throw new Exception("查询返回记录数不匹配！");

            //更新
            XTrace.WriteLine("更新测试！");
            n = (Int32)ds.Tables[0].Rows[0]["ParentID"];
            sql = "Update xtest set ParentID=998877 where ID=33+1";
            Dal.Execute(sql, "test");

            ds = Dal.Select("select * from xtest", 33, 44, "ID", "xtest");
            m = (Int32)ds.Tables[0].Rows[0]["ParentID"];
            if (m != 998877) throw new Exception("更新失败！");

            //删除
            dataMax = rnd.Next(50, 100);
            XTrace.WriteLine("删除测试！" + dataMax);
            n = Dal.SelectCount("select * from xtest", "xtest");
            XTrace.WriteLine("删除前" + n);

            for (int i = 0; i < dataMax; i++)
            {
                sql = String.Format("Delete from xtest where id={0}", i + 1);
                Dal.Execute(sql, "test");
            }

            m = Dal.SelectCount("select * from xtest", "xtest");
            XTrace.WriteLine("删除后" + m);
            if (m != n - dataMax) throw new Exception("删除失败！");

            //新增字段
            field = table.CreateField();
            field.ID = table.Fields.Count + 1;
            field.Name = "Extend";
            field.DataType = typeof(String);
            field.Length = 99;
            field.Default = "没有";
            field.Description = "扩展";

            XTrace.WriteLine("新增字段：{0}", field);
            MetaData.SetSchema(DDLSchema.AddColumn, table.Name, field);

            //修改字段
            field.Length = 555;
            XTrace.WriteLine("修改字段：{0}", field);
            MetaData.SetSchema(DDLSchema.AlterColumn, table.Name, field);

            //删除字段
            XTrace.WriteLine("删除字段：{0}", field);
            MetaData.SetSchema(DDLSchema.DropColumn, table.Name, field.Name);

            //删除数据表
            XTrace.WriteLine("删除数据表：{0}", table.Name);
            MetaData.SetSchema(DDLSchema.DropTable, table.Name);
            if ((Boolean)MetaData.SetSchema(DDLSchema.TableExist, table.Name)) throw new Exception("删除表失败！");

            //删除数据库
            //if (!dbExist)
            {
                XTrace.WriteLine("删除数据库：{0}", Dal.ConnName);
                MetaData.SetSchema(DDLSchema.DropDatabase, Dal.ConnName);
            }
        }
        #endregion

        #region 生成代码测试
        /// <summary>
        /// 代码生成测试
        /// </summary>
        /// <param name="dal"></param>
        public static void CodeTest(DAL dal)
        {
            //XTable table = dal.Tables[0];

            //foreach (XTable item in dal.Tables)
            //{
            //    if (item.Name == "Area")
            //    {
            //        table = item;
            //        break;
            //    }
            //}

            EntityAssembly asm = new EntityAssembly();
            asm.Dal = dal;
            asm.NameSpace = new System.CodeDom.CodeNamespace("XCode.Test.Entities");

            //EntityClass entity = asm.Create(table);
            //entity.Create();
            //entity.AddProperties();
            //entity.AddIndexs();
            //entity.AddNames();

            EntityClass entity = asm.Create("Area");
            String str = entity.GenerateCSharpCode();
            Console.WriteLine(str);

            CompilerResults rs = asm.Compile(null);
            foreach (String item in rs.Output)
            {
                Console.WriteLine(item);
            }

            //asm.CreateAll();

            //str = asm.GenerateCSharpCode();
            ////File.WriteAllText(dal.ConnName + ".cs", str);

            //Console.WriteLine(str);
        }
        #endregion

        #region 动态访问测试
        /// <summary>
        /// 动态访问测试
        /// </summary>
        public static void DynTest()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Console.WriteLine("预热");
            DAL dal = DAL.Create("Common");
            Int32 n = dal.SelectCount("select * from Administrator", "");
            Console.WriteLine(n);

            sw.Stop();
            Console.WriteLine("耗时：{0}", sw.Elapsed);
            Thread.Sleep(10000);

            Console.WriteLine("第一次");
            sw.Reset();
            sw.Start();

            DynTest2("Area");

            sw.Stop();
            Console.WriteLine("耗时：{0}", sw.Elapsed);

            Console.WriteLine("第二次");
            sw.Reset();
            sw.Start();

            DynTest2("Area");

            sw.Stop();
            Console.WriteLine("耗时：{0}", sw.Elapsed);

            Console.WriteLine("一百次");
            sw.Reset();
            sw.Start();

            for (int i = 0; i < 1000; i++)
            {
                DynTest2("Area");
            }

            sw.Stop();
            Console.WriteLine("耗时：{0}", sw.Elapsed);

            Console.WriteLine("第二次");
            sw.Reset();
            sw.Start();

            DynTest2("Administrator");

            sw.Stop();
            Console.WriteLine("耗时：{0}", sw.Elapsed);

            Console.WriteLine("一百次");
            sw.Reset();
            sw.Start();

            for (int i = 0; i < 1000; i++)
            {
                DynTest2("Administrator");
            }

            sw.Stop();
            Console.WriteLine("耗时：{0}", sw.Elapsed);
        }

        static void DynTest2(String tableName)
        {
            DAL dal = DAL.Create("Common");
            IEntityOperate factory = dal.CreateOperate(tableName);
            Int32 count = factory.FindCount();
            //Console.WriteLine(count);
        }
        #endregion
    }
}
