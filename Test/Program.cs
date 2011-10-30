using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NewLife.CommonEntity;
using NewLife.Log;
using NewLife.Model;
using XCode;
using XCode.Accessors;
using System.Text;
using System.Reflection;
using System.Data.Common;
using NewLife.Reflection;
using XCode.DataAccessLayer;
using System.Collections.Generic;
using System.Data.OracleClient;
using System.Data;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
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
                    Test2();
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

        static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void Test1()
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

        static void Test2()
        {
            //OracleClientFactory d = OracleClientFactory.Instance;
            //Console.WriteLine(d);

            Type type = null;
            //type = TypeX.GetType("Oracle.DataAccess.Client.OpsInit", true);
            //MethodInfoX mix = MethodInfoX.Create(type, "CheckVersionCompatibility");
            //mix.Invoke(null, "2.112.1.0");
            //OpsInit.CheckVersionCompatibility("2.112.1.0");
            //OracleInit.Initialize();

            String sql = null;
            DAL dal = DAL.Create("Common");

            //            sql = @"select sysdate from dual;
            //select sysdate from dual";
            //            DataSet ds = dal.Select(sql, "");
            //            Console.WriteLine(ds);

            //            sql = @"Create Table Administrator(
            //        ID NUMBER(10, 0) NOT NULL,
            //        Name NVARCHAR2(50) NULL,
            //        Password NVARCHAR2(50) NULL,
            //        DisplayName NVARCHAR2(50) NULL,
            //        RoleID NUMBER(10, 0) NULL,
            //        Logins NUMBER(10, 0) NULL,
            //        LastLogin DATE NULL,
            //        LastLoginIP NVARCHAR2(50) NULL,
            //        SSOUserID NUMBER(10, 0) NULL,
            //        IsEnable NUMBER(1, 0) NULL,
            //        constraint pk_Administrator primary key (ID)
            //);
            //Create Sequence SEQ_Administrator Minvalue 1 Maxvalue 9999999999 Start With 1 In
            //crement By 1 Cache 20";
            //            dal.Execute(sql, "");

            List<IDataTable> tables = dal.Tables;
            Console.WriteLine(tables);

            //try
            //{
            //    Assembly asm = Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Oracle.DataAccess.dll"));
            //    type = asm.GetType("Oracle.DataAccess.Client.OracleClientFactory");

            //    FieldInfo field = type.GetField("Instance");
            //    //DbProviderFactory df= Activator.CreateInstance(type) as DbProviderFactory;

            //    DbProviderFactory df = field.GetValue(null) as DbProviderFactory;
            //    Console.WriteLine(df);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}
            //Console.ReadKey(true);

            Administrator admin = Administrator.FindAll()[0];
            Console.WriteLine(admin);
        }
    }
}