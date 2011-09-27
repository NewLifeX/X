using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NewLife.Reflection;

namespace NewLife.IO
{
    /// <summary>
    /// 二进制操作测试
    /// </summary>
    public class BinaryTest : BinaryAccessor
    {
        /// <summary>
        /// 测试
        /// </summary>
        public static void Test()
        {
            Random rnd = new Random((Int32)DateTime.Now.Ticks);

            MemoryStream ms = new MemoryStream();
            BinaryTest entity = new BinaryTest();

            Console.WriteLine();
            Console.WriteLine("空数据序列化：");
            entity.Write(new BinaryWriterX(ms));
            Byte[] data = ms.ToArray();
            Console.WriteLine("结果：" + BitConverter.ToString(data));

            entity.ID = rnd.Next(0, 10000);
            entity.Name = rnd.Next(10000, 99999).ToString();
            entity.Time = DateTime.Now;
            entity.Consolekey = new ConsoleKeyInfo('B', ConsoleKey.A, false, false, false);
            entity.ByteProperty = (Byte)rnd.Next(0, 256);

            entity.Guid = Guid.NewGuid();
            entity.Address = IPAddress.Loopback;
            entity.Remote = new IPEndPoint(IPAddress.Loopback, rnd.Next(1, 65536));

            entity.Bytes = Guid.NewGuid().ToByteArray();
            entity.ByteList = new List<byte>(Guid.NewGuid().ToByteArray());
            entity.Dic = new Dictionary<int, byte>();
            //entity.HashTable = new Hashtable();
            Int32 n = rnd.Next(0, 10);
            for (int i = 0; i < n; i++)
            {
                Int32 key = rnd.Next(0, 10000);
                if (!entity.Dic.ContainsKey(key)) entity.Dic.Add(key, (Byte)rnd.Next(0, 256));
                //key = rnd.Next(0, 10000);
                //if (!entity.HashTable.ContainsKey(key)) entity.HashTable.Add(key, (Byte)rnd.Next(0, 256));
            }

            FieldInfoX fi = FieldInfoX.Create(typeof(KeyValuePair<Int32, byte>), "key");
            foreach (KeyValuePair<Int32, byte> item in entity.Dic)
            {
                n = (Int32)fi.GetValue(item);
            }

            Console.WriteLine();
            Console.WriteLine("序列化：");
            ms = new MemoryStream();
            entity.Write(new BinaryWriterX(ms));

            data = ms.ToArray();
            Console.WriteLine("结果：" + BitConverter.ToString(data));

            ms = new MemoryStream(data);
            Console.WriteLine();
            Console.WriteLine("反序列化：");
            BinaryTest entity2 = new BinaryTest();
            entity2.Read(new BinaryReaderX(ms));

            Console.WriteLine();
            Console.WriteLine("比对：");
        }

        #region 基本类型
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

        private Byte _ByteProperty;
        /// <summary>属性说明</summary>
        public Byte ByteProperty
        {
            get { return _ByteProperty; }
            set { _ByteProperty = value; }
        }

        private DateTime _Time;
        /// <summary>属性说明</summary>
        public DateTime Time
        {
            get { return _Time; }
            set { _Time = value; }
        }

        private ConsoleKeyInfo _ConsoleKey;
        /// <summary>属性说明</summary>
        public ConsoleKeyInfo Consolekey
        {
            get { return _ConsoleKey; }
            set { _ConsoleKey = value; }
        }
        #endregion

        #region 扩展类型
        private Guid _Guid;
        /// <summary>属性说明</summary>
        public Guid Guid
        {
            get { return _Guid; }
            set { _Guid = value; }
        }

        private IPAddress _Address;
        /// <summary>属性说明</summary>
        public IPAddress Address
        {
            get { return _Address; }
            set { _Address = value; }
        }

        private IPEndPoint _Remote;
        /// <summary>属性说明</summary>
        public IPEndPoint Remote
        {
            get { return _Remote; }
            set { _Remote = value; }
        }
        #endregion

        #region 接口
        #endregion

        #region 数组
        private Byte[] _Bytes;
        /// <summary>属性说明</summary>
        public Byte[] Bytes
        {
            get { return _Bytes; }
            set { _Bytes = value; }
        }

        private List<Byte> _ByteList;
        /// <summary>属性说明</summary>
        public List<Byte> ByteList
        {
            get { return _ByteList; }
            set { _ByteList = value; }
        }

        private Dictionary<Int32, Byte> _Dic;
        /// <summary>属性说明</summary>
        public Dictionary<Int32, Byte> Dic
        {
            get { return _Dic; }
            set { _Dic = value; }
        }

        //private Hashtable _HashTable;
        ///// <summary>属性说明</summary>
        //public Hashtable HashTable
        //{
        //    get { return _HashTable; }
        //    set { _HashTable = value; }
        //}
        #endregion

        #region 复杂类型
        #endregion
    }
}
