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
            MemoryStream ms = new MemoryStream();
            writer.Writer = new BinaryWriter(ms);

            //writer.IsLittleEndian = false;
            writer.EncodeInt = true;

            Administrator entity = new Administrator();
            entity.ID = 123;
            entity.Name = "nnhy";
            entity.DisplayName = "大石头";

            writer.WriteObject(entity);

            Byte[] buffer = ms.ToArray();
            Console.WriteLine(BitConverter.ToString(buffer));

            BinaryReaderX reader = new BinaryReaderX();
            ms.Position = 0;
            reader.Reader = new BinaryReader(ms);
            reader.EncodeInt = true;

            Administrator admin = new Administrator();
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
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xtw = new XmlTextWriter(ms, writer.Encoding);
            xtw.Formatting = Formatting.Indented;
            writer.Writer = xtw;

            //writer.MemberStyle = XmlMemberStyle.Element;
            writer.MemberStyle = XmlMemberStyle.Attribute;

            Administrator entity = new Administrator();
            entity.ID = 123;
            entity.Name = "nnhy";
            entity.DisplayName = "大石头";

            writer.WriteObject(entity);

            writer.Writer.Flush();
            Byte[] buffer = ms.ToArray();
            Console.WriteLine(Encoding.UTF8.GetString(buffer));

            //XmlReaderX reader = new XmlReaderX();
            //ms.Position = 0;
            //reader.Reader = new XmlTextReader(ms);
            //reader.MemberStyle = XmlMemberStyle.Element;

            //Administrator admin = new Administrator();
            //Object obj = admin;
            //reader.ReadObject(null, ref obj);
            //Console.WriteLine(obj != null);
        }
    }
}