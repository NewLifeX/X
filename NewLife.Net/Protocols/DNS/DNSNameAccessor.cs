using System;
using NewLife.IO;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>DNS名称访问器</summary>
    class DNSNameAccessor
    {
        #region 属性
        private List<Int32> _Keys;
        /// <summary>键</summary>
        public List<Int32> Keys { get { return _Keys ?? (_Keys = new List<int>()); } set { _Keys = value; } }

        private List<String> _Values;
        /// <summary>值</summary>
        public List<String> Values { get { return _Values ?? (_Values = new List<string>()); } set { _Values = value; } }
        #endregion

        #region 方法
        String this[Int32 key] { get { return Values[Keys.IndexOf(key)]; } }

        Int32 this[String value] { get { return Keys[Values.IndexOf(value)]; } }

        public String Read(Stream stream, Int64 offset)
        {
            // 先用局部变量临时保存，因为每新读出来一段，就要全部加上去
            var keys = new List<Int32>();
            var values = new List<String>();

            Int64 p = stream.Position;
            Int32 n = 0;
            StringBuilder sb = new StringBuilder();
            while ((n = stream.ReadByte()) != 0)
            {
                if (sb.Length > 0) sb.Append(".");

                String str = null;

                // 0xC0表示是引用，下一个地址指示偏移量
                if (n == 0xC0)
                {
                    var n2 = stream.ReadByte();
                    str = this[n2];

                    sb.Append(str);

                    // 之前的每个加上str
                    for (int i = 0; i < values.Count; i++) values[i] += "." + str;

                    break;
                }

                Byte[] buffer = stream.ReadBytes(n);
                str = Encoding.UTF8.GetString(buffer);

                // 之前的每个加上str
                for (int i = 0; i < values.Count; i++) values[i] += "." + str;

                // 加入当前项。因为引用项马上就要跳出了，不会做二次引用，所以不加
                keys.Add((Int32)(offset + p));
                values.Add(str);

                sb.Append(str);

                p = stream.Position;
            }
            for (int i = 0; i < keys.Count; i++)
            {
                Keys.Add(keys[i]);
                Values.Add(values[i]);
            }

            return sb.ToString();
        }

        public void Write(Stream stream, String value, Int64 offset)
        {
            // 先用局部变量临时保存，因为每新读出来一段，就要全部加上去
            var keys = new List<Int32>();
            var values = new List<String>();

            Int32 p = 0;
            Boolean isRef = false;
            String[] ss = ("" + value).Split(".");
            for (int i = 0; i < ss.Length; i++)
            {
                isRef = false;

                // 如果已存在，则写引用
                String name = String.Join(".", ss, i, ss.Length - i);
                if (Values.Contains(name))
                {
                    stream.WriteByte(0xC0);
                    stream.WriteByte((Byte)this[name]);

                    // 之前的每个加上str
                    for (int j = 0; j < values.Count; j++) values[j] += "." + name;

                    // 使用引用的必然是最后一个
                    isRef = true;

                    break;
                }

                // 否则，先写长度，后存入引用
                p = (Int32)stream.Position;

                Byte[] buffer = Encoding.UTF8.GetBytes(ss[i]);
                stream.WriteByte((Byte)buffer.Length);
                stream.Write(buffer, 0, buffer.Length);

                // 之前的每个加上str
                for (int j = 0; j < values.Count; j++) values[j] += "." + ss[i];

                // 加入当前项
                keys.Add((Int32)(offset + p));
                values.Add(ss[i]);
            }
            if (!isRef) stream.WriteByte((Byte)0);

            for (int i = 0; i < keys.Count; i++)
            {
                Keys.Add(keys[i]);
                Values.Add(values[i]);
            }
        }
        #endregion
    }
}