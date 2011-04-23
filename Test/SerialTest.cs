using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;
using NewLife.CommonEntity;
using System.IO;
using NewLife.Xml;
using System.Xml;

namespace Test
{
    /// <summary>
    /// 序列化测试
    /// </summary>
    public static class SerialTest
    {
        /// <summary>
        /// 二进制序列化测试
        /// </summary>
        public static void BinaryTest()
        {
            BinaryWriterX writer = new BinaryWriterX();
            //writer.IsLittleEndian = false;
            writer.EncodeInt = true;

            Administrator entity = GetDemo();

            writer.WriteObject(entity);

            Byte[] buffer = writer.ToArray();
            Console.WriteLine(BitConverter.ToString(buffer));

            BinaryReaderX reader = new BinaryReaderX();
            reader.Stream = writer.Stream;
            reader.Stream.Position = 0;
            reader.EncodeInt = true;

            Administrator admin = new Admin();
            Object obj = admin;
            reader.ReadObject(null, ref obj);
            Console.WriteLine(obj != null);

            //reader.ReadObject(typeof(Administrator));
        }

        /// <summary>
        /// Xml序列化测试
        /// </summary>
        public static void XmlTest()
        {
            XmlWriterX writer = new XmlWriterX();
            writer.MemberAsAttribute = false;
            writer.IgnoreDefault = false;

            Administrator entity = GetDemo();

            writer.WriteObject(entity);

            writer.Flush();
            Byte[] buffer = writer.ToArray();
            Console.WriteLine(Encoding.UTF8.GetString(buffer));

            #region 测试
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            writer.Stream.Position = 0;
            XmlReader xr = XmlReader.Create(writer.Stream, settings);
            while (xr.Read())
            {
                Console.WriteLine("{0}, {1}={2}", xr.NodeType, xr.Name, xr.Value);
            }
            #endregion

            XmlReaderX reader = new XmlReaderX();
            reader.Stream = writer.Stream;
            reader.Stream.Position = 0;
            reader.MemberAsAttribute = writer.MemberAsAttribute;
            reader.IgnoreDefault = writer.IgnoreDefault;

            Administrator admin = new Admin();
            Object obj = admin;
            reader.ReadObject(null, ref obj);
            Console.WriteLine(obj != null);
        }

        static Administrator GetDemo()
        {
            Admin entity = new Admin();
            entity.ID = 123;
            entity.Name = "nnhy";
            entity.DisplayName = "大石头";
            entity.Logins = 65535;
            entity.LastLogin = DateTime.Now;
            entity.SSOUserID = 555;

            Department dp = new Department();
            dp.ID = 1;
            dp.Name = "部门一";

            Department dp2 = new Department();
            dp2.ID = 2;
            dp2.Name = "部门二";

            entity.DP1 = dp;
            entity.DP2 = dp2;
            entity.DP3 = dp;

            entity.DPS = new Department[] { dp, dp2, dp };

            return entity;
        }

        class Admin : Administrator
        {
            private Department _DP1;
            /// <summary>属性说明</summary>
            public Department DP1
            {
                get { return _DP1; }
                set { _DP1 = value; }
            }

            private Department _DP2;
            /// <summary>属性说明</summary>
            public Department DP2
            {
                get { return _DP2; }
                set { _DP2 = value; }
            }

            private Department _DP3;
            /// <summary>属性说明</summary>
            public Department DP3
            {
                get { return _DP3; }
                set { _DP3 = value; }
            }

            private Department[] _DPS;
            /// <summary>属性说明</summary>
            public Department[] DPS
            {
                get { return _DPS; }
                set { _DPS = value; }
            }
        }

        class Department
        {
            private Int32 _ID;
            /// <summary>属性说明</summary>
            public Int32 ID
            {
                get { return _ID; }
                set { _ID = value; }
            }

            private String _Name;
            /// <summary>属性说明</summary>
            public String Name
            {
                get { return _Name; }
                set { _Name = value; }
            }

            public override string ToString()
            {
                return String.Format("{0}, {1}", ID, Name);
            }
        }
    }
}