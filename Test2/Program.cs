using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;
using NewLife.Net.Tcp;
using System.Net;
using NewLife.Net.Sockets;
using XCode;
using XCode.DataAccessLayer;
using System.IO;
using NewLife.Xml;
using System.Data.Common;
using System.Data;

namespace Test2
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);
            while (true)
            {
#if !DEBUG
                try
                {
#endif
                    Test3();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
#endif

                Console.WriteLine("OK!");
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void Test1()
        {
            IPAddress address = IPAddress.Loopback;
            for (int i = 0; i < 10000; i++)
            {
                TcpClientX tc = new TcpClientX();
                tc.Connect(address, 7);
                tc.Received += new EventHandler<NewLife.Net.Sockets.NetEventArgs>(tc_Received);
                tc.ReceiveAsync();

                tc.Send("我是大石头" + i + "号！");
            }
        }

        static void tc_Received(object sender, NetEventArgs e)
        {
            Console.WriteLine("[{0}] {1}", e.RemoteEndPoint, e.GetString());
        }

        static void Test2()
        {
            DAL dal = DAL.Create("Common1");
            IList<IDataTable> list = dal.Tables;
            //foreach (IDataTable item in list)
            //{

            //}

            XmlWriterX writer = new XmlWriterX();
            writer.WriteObject(list);

            Console.WriteLine(writer.ToString());
        }

        static void Test3()
        {
            DAL dal = DAL.Create("Common4");
            IDatabase db = dal.Db;

            DbCommand cmd = db.CreateSession().CreateCommand();
            cmd.CommandText = "select * from area";

            DataTable dt = null;
            using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly))
            {
                dt = reader.GetSchemaTable();
            }
            Console.WriteLine(dt);

            DbCommandBuilder builder = db.Factory.CreateCommandBuilder();
            builder.DataAdapter = db.Factory.CreateDataAdapter();
            builder.DataAdapter.SelectCommand = cmd;

            cmd = builder.GetInsertCommand();
            Console.WriteLine(cmd.CommandText);
            cmd = builder.GetInsertCommand(true);
            Console.WriteLine(cmd.CommandText);

            //String xml = dal.Export();
            //Console.WriteLine(xml);

            //File.WriteAllText(dal.ConnName + ".xml", xml);

            //List<IDataTable> tables = DAL.Import(xml);
            //Console.WriteLine(tables);

            //String xml2 = DAL.Export(tables);
            //Console.WriteLine(xml == xml2);
        }
    }

    public class NewLifeDb
    {
        public IDatabase _obj = DAL.Create("Common1").Db;

        public IDbSession CreateSession()
        {
            IDbSession session = _obj.CreateSession();
            Console.WriteLine("为{0}创建会话", session.DatabaseName);
            return session;
        }
    }
}