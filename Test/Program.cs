using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Web;
using System.Web.Caching;
using NewLife.CommonEntity;
using NewLife.Compression;
using NewLife.IO;
using NewLife.Log;
using NewLife.Threading;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Test;
using NewLife.Reflection;
using System.Reflection;

namespace Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                    Test8();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        private static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        private static void Test1()
        {
            Administrator admin = Administrator.Login("admin", "admin");
            Int32 id = admin.ID;
            admin = Administrator.Find(new String[] { Administrator._.ID, Administrator._.Name }, new Object[] { id, "admin" });
            admin = Administrator.Find(new String[] { Administrator._.ID, Administrator._.Name }, new String[] { id.ToString(), "admin" });

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, admin);
            ms.Position = 0;
            bf = new BinaryFormatter();
            IAdministrator admin2 = bf.Deserialize(ms) as IAdministrator;
            Console.WriteLine(admin2);
        }

        private static void Test2()
        {
            DAL dal = DAL.Create("Common1");
            IDataTable table = Log.Meta.Table.DataTable;

            IEntityOperate op = dal.CreateOperate(table.Name);
            Int32 count = op.Count;
            Console.WriteLine(op.Count);

            Int32 pagesize = 10;
            String order = table.PrimaryKeys[0].Alias + " Asc";
            String order2 = table.PrimaryKeys[0].Alias + " Desc";
            String selects = table.Columns[5].Name;
            //selects = table.PrimaryKeys[0].Name;

            DAL.ShowSQL = false;
            Int32 t = 2;
            Int32 p = (Int32)((t + count) / pagesize * pagesize);

            SelectBuilder builder = new SelectBuilder();
            builder.Table = table.Name;
            builder.Key = table.PrimaryKeys[0].Name;

            Console.WriteLine();
            Console.WriteLine("选择主键：");
            selects = table.PrimaryKeys[0].Name;
            TestPageSplit("无排序", builder, op, selects, null, t);
            TestPageSplit("升序", builder, op, selects, order, t);
            TestPageSplit("降序", builder, op, selects, order2, t);

            Console.WriteLine();
            Console.WriteLine("选择非主键：");
            selects = table.Columns[5].Name;
            TestPageSplit("无排序", builder, op, selects, null, t);
            TestPageSplit("升序", builder, op, selects, order, t);
            TestPageSplit("降序", builder, op, selects, order2, t);

            Console.WriteLine();
            Console.WriteLine("选择所有：");
            selects = null;
            TestPageSplit("无排序", builder, op, selects, null, t);
            TestPageSplit("升序", builder, op, selects, order, t);
            TestPageSplit("降序", builder, op, selects, order2, t);
        }

        private static void TestPageSplit(String title, SelectBuilder builder, IEntityOperate op, String selects, String order, Int32 t)
        {
            Int32 pagesize = 10;
            Int32 p = 0;
            Int32 count = op.Count;
            Boolean needTimeOne = true;

            builder.Column = selects;
            builder.OrderBy = order;

            Console.WriteLine();
            CodeTimer.ShowHeader();
            Console.WriteLine(title);

            Console.WriteLine(MSPageSplit.PageSplit(builder, 0, pagesize, false));
            CodeTimer.TimeLine("首页", t, n => op.FindAll(null, order, selects, 0, pagesize), needTimeOne);
            Console.WriteLine(MSPageSplit.PageSplit(builder, pagesize * 2, pagesize, false));
            CodeTimer.TimeLine("第三页", t, n => op.FindAll(null, order, selects, pagesize * 2, pagesize), needTimeOne);

            p = count / pagesize / 2;
            Console.WriteLine(MSPageSplit.PageSplit(builder, pagesize * p, pagesize, false));
            CodeTimer.TimeLine(p + "页", t, n => op.FindAll(null, order, selects, pagesize * p, pagesize), needTimeOne);
            Console.WriteLine(MSPageSplit.PageSplit(builder, pagesize * p + 1, pagesize, false));
            CodeTimer.TimeLine((p + 1) + "页", t, n => op.FindAll(null, order, selects, pagesize * p + 1, pagesize), needTimeOne);

            p = (Int32)((t + count) / pagesize * pagesize);
            Console.WriteLine(MSPageSplit.PageSplit(builder, p, pagesize, false));
            CodeTimer.TimeLine("尾页", t, n => op.FindAll(null, order, selects, p, pagesize), needTimeOne);
            Console.WriteLine(MSPageSplit.PageSplit(builder, p - pagesize * 2, pagesize, false));
            CodeTimer.TimeLine("倒数第三页", t, n => op.FindAll(null, order, selects, p - pagesize * 2, pagesize), needTimeOne);
        }

        private static void Test3()
        {
            Int32 n = EntityTest.Meta.Count;
            Console.WriteLine(n);

            EntityList<EntityTest> list = EntityTest.FindAll();
            DataTable dt = list.ToDataTable();
            Console.WriteLine(dt);

            dt = Administrator.FindAll().ToDataTable();
        }

        private static void Test4()
        {
            IDataTable table = Log.Meta.Table.DataTable;

            SelectBuilder builder = new SelectBuilder();
            builder.Table = table.Name;
            builder.Key = table.PrimaryKeys[0].Name;
            builder.Keys = new String[] { "Category", "ID" };

            String order = "Category Desc," + table.PrimaryKeys[0].Alias + " Asc";
            String order2 = "Category Desc," + table.PrimaryKeys[0].Alias + " Desc";
            String selects = "ID," + table.Columns[2].Name;
            //selects = table.PrimaryKeys[0].Name;

            DAL.ShowSQL = false;

            //selects = null;
            TestPageSplit("未排序", builder, selects, null);
            TestPageSplit("升序", builder, selects, order);
            TestPageSplit("降序", builder, selects, order2);
        }

        private static void TestPageSplit(String title, SelectBuilder builder, String selects, String order)
        {
            Int32 pagesize = 10;
            Int32 p = 0;

            builder.Column = selects;
            builder.OrderBy = order;

            String sql = null;

            Console.WriteLine();
            Console.WriteLine("--" + title);

            //sql = MSPageSplit.PageSplit(builder, p, pagesize, false).ToString();
            //Console.WriteLine("--首页SQL2000：\n{0}", sql);
            //sql = MSPageSplit.PageSplit(builder, p, pagesize, true).ToString();
            //Console.WriteLine("--首页SQL2005：\n{0}", sql);

            p = 50 * pagesize;
            sql = MSPageSplit.PageSplit(builder, p, pagesize, false).ToString();
            Console.WriteLine("--50页SQL2000：\n{0}", sql);
            sql = MSPageSplit.PageSplit(builder, p, pagesize, true).ToString();
            Console.WriteLine("--50页SQL2005：\n{0}", sql);
        }

        private static void Test5()
        {
            ThreadPoolX.QueueUserWorkItem(Test5_0);

            String k = "asdf";
            String value = "vvv";
            CacheItemUpdateCallback callback = new CacheItemUpdateCallback(delegate(string key, CacheItemUpdateReason reason, out object expensiveObject, out CacheDependency dependency, out DateTime absoluteExpiration, out TimeSpan slidingExpiration)
            {
                XTrace.WriteLine("Update:{0}", reason);

                expensiveObject = null;
                dependency = null;
                absoluteExpiration = Cache.NoAbsoluteExpiration;
                //slidingExpiration = new TimeSpan(0, 0, 9);
                slidingExpiration = Cache.NoSlidingExpiration;
            });

            //HttpRuntime.Cache.Insert(k, value, null, DateTime.UtcNow.AddSeconds(5), Cache.NoSlidingExpiration, callback);
            //HttpRuntime.Cache.Insert(k, value, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, 0, 30), callback);
            //HttpRuntime.Cache.Insert(k, value, new EntityCacheDependency<Administrator>(), Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, callback);

            Thread.Sleep(9000);

            Administrator admin = Administrator.Login("admin", "admin");
            Console.WriteLine(admin);
        }

        private static void Test5_0()
        {
            String k = "asdf";
            String value = "vvv";
            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(1000);

                value = HttpRuntime.Cache[k] as String;
                XTrace.WriteLine("{0} {1}", i, value);
            }
        }

        private static void Test6()
        {
            // 添加一个连接
            //DAL.AddConnStr("test", "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=netbar.xls;" + "Extended Properties=Excel 8.0;", null, null);
            DAL.AddConnStr("test", "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=netbar2.xls;", null, null);
            DAL.AddConnStr("netbar", "Data Source=.;Initial Catalog=Console;user id=sa;password=Pass@word", null, "mssql");
            DAL dal = DAL.Create("test");
            DAL netbar = DAL.Create("netbar");
            IEntityOperate nb = netbar.CreateOperate("Netbar");

            List<Area> ars = Area.FindByName("江苏省");
            if (ars != null && ars.Count > 0) Console.WriteLine(ars[0]);
            Area root = ars[0];

            DAL.ShowSQL = false;

            // 遍历所有表
            foreach (var table in dal.Tables)
            {
                Console.WriteLine("表 {0}：", table.Name);

                // 创建一个实体操作者，这里会为数据表动态生成一个实体类，并使用CodeDom编译
                IEntityOperate op = dal.CreateOperate(table.Alias);
                if (op == null) continue;

                //// 因为动态生成代码的缺陷，表名中的$已经被去掉，并且Excel的查询总必须给表名加上方括号，还是因为有$
                //// 下面通过快速反射设置Meta.TableName
                //Type type = op.GetType();
                //type = typeof(Entity<>.Meta).MakeGenericType(type);
                //PropertyInfoX.Create(type, "TableName").SetValue("[" + table.Name + "]");

                // 如果没有记录，跳过
                if (op.FindCount() < 1) continue;

                // 输出表头
                if (op.Fields == null || op.Fields.Length < 1) continue;
                foreach (var item in op.Fields)
                {
                    if (item.Name.StartsWith("F")) break;

                    Console.Write("{0}\t", item.Name);
                }
                Console.WriteLine();

                // 查找所有数据
                IEntityList list = op.FindAll();
                //DataSet ds = list.ToDataSet();

                // 输出数据
                foreach (IEntity entity in list)
                {
                    String name = (String)entity["门店名称"];
                    Console.WriteLine("正在处理 {0}", name);
                    if (nb.FindCount("Name", name) > 0)
                    {
                        Console.WriteLine("{0}已存在，跳过！", name);
                        continue;
                    }

                    IEntity n = nb.Create();
                    foreach (FieldItem item in op.Fields)
                    {
                        //if (item.Name.StartsWith("F")) break;

                        //Console.Write("{0}\t", entity[item.Name]);

                        if (item.Name == "门店名称")
                            n.SetItem("Name", entity[item.Name]);
                        else if (item.Field.Alias == "地址")
                            n.SetItem("Address", entity[item.Name]);
                        else if (item.Field.Alias == "联系电话")
                            n.SetItem("Tel", entity[item.Name]);
                        else if (item.Field.Alias == "法定代表人")
                            n.SetItem("Manager", entity[item.Name]);
                        else if (item.Field.Alias == "门店性质")
                            n.SetItem("NetType", entity[item.Name]);
                        else if (item.Field.Alias == "备注")
                            n.SetItem("Remark", entity[item.Name]);
                        else if (item.Field.Alias == "地区")
                        {
                            String str = (String)entity[item.Name];
                            if (!String.IsNullOrEmpty(str))
                            {
                                if (str.EndsWith("开发区")) str = str.Substring(0, str.Length - 3);
                                //if (str.Length > 3 && str.StartsWith("连云港")) str = str.Substring(3);
                                List<Area> ss = root.FindAllByName(str, true, 2);
                                if (ss != null && ss.Count > 0)
                                    n.SetItem("AreaCode", ss[0].Code);
                                else
                                {
                                }
                            }
                        }
                    }
                    //Console.WriteLine();

                    n.Save();
                }
            }
        }

        private static void Test7()
        {
            Type type = typeof(DatabaseType);
            var dic = EnumHelper.GetDescriptions(type);

            // 产生字符串
            Int32 max = 1000000;
            String[] ss = new String[max];
            Random rnd = new Random((Int32)DateTime.Now.Ticks);
            for (int i = 0; i < max; i++)
            {
                Int32 len = rnd.Next(0, 20);
                Char[] cs = new Char[len];
                for (int j = 0; j < len; j++)
                {
                    cs[j] = (Char)rnd.Next(0x4e00, 0x9fa5);
                }
                ss[i] = new String(cs);
            }

            List<String> list = new List<string>();
            Hashtable ht = new Hashtable();

            Stopwatch sw = new Stopwatch();

            sw.Start();
            String str = null;
            for (int i = 0; i < max; i++)
            {
                str = ss[i];
                if (!ht.ContainsKey(str))
                {
                    ht.Add(str, str);

                    list.Add(str);
                }
            }
            sw.Stop();
            Console.WriteLine("双列表：{0}", sw.Elapsed);

            list = new List<string>();
            ht = new Hashtable();
            sw.Start();
            for (int i = 0; i < max; i++)
            {
                str = ss[i];
                if (!ht.ContainsKey(str))
                {
                    ht.Add(str, "");

                    list.Add(str);
                }
            }
            sw.Stop();
            Console.WriteLine("双列表：{0}", sw.Elapsed);

            list = new List<string>();
            sw.Start();
            for (int i = 0; i < max; i++)
            {
                str = ss[i];
                if (!list.Contains(str)) list.Add(str);
            }
            sw.Stop();
            Console.WriteLine("单列表：{1}", sw.Elapsed);
        }

        private static void Test8()
        {
            //var reader = new MethodBodyReader(typeof(Program).GetMethod("Test3", BindingFlags.Static | BindingFlags.NonPublic));
            //Console.WriteLine(reader.GetBodyCode());

            String file = @"E:\Net\SharpZipLib\SharpZipLib_0860_Bin.zip";
            file = @"E:\X\Src\Src_20111215194303.zip";
            Stream stream = File.OpenRead(file);

            Random rnd = new Random((Int32)DateTime.Now.Ticks);
            Int32 p = rnd.Next(0, (Int32)stream.Length);
            Int32 len = rnd.Next(4, 12);
            Byte[] buffer = new Byte[len];
            stream.Seek(p, SeekOrigin.Begin);
            stream.Read(buffer, 0, buffer.Length);

            stream.Seek(0, SeekOrigin.Begin);
            Int64 p2 = stream.IndexOf(buffer);
            //Int64 p2 = File.ReadAllBytes(file).IndexOf(0, 0, buffer);
            Console.WriteLine(p == p2);

            //stream.Seek(0, SeekOrigin.Begin);
            //Int32 SignatureToFind = 101010256;
            //byte[] targetBytes = BitConverter.GetBytes(SignatureToFind);

            //p2 = stream.IndexOf(targetBytes);
            //Console.WriteLine(p2);

            //using (ZipFile zf = new ZipFile(file))
            //{
            //    foreach (var item in zf)
            //    {
            //        Console.WriteLine(item.FileName);
            //    }

            //    zf.Extract("ExtractTest");
            //}


            using (ZipFile zf = new ZipFile())
            {
                //zf.AddDirectory(@"E:\X\Src\Test");
                zf.AddDirectory(@"..\Src\Test");

                Console.WriteLine(zf.Count);

                zf.Write(File.Create("aa.zip"));
            }
        }
    }
}