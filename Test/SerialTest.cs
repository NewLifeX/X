using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;
using NewLife.CommonEntity;
using System.IO;

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
            reader.TryReadObject(null, ref obj);
            Console.WriteLine(obj != null);
            //reader.ReadObject(typeof(Administrator));
        }
    }
}