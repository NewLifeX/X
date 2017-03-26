using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewLife.Net.DNS
{
    /// <summary>DNS名称访问器</summary>
    class DNSNameAccessor
    {
        #region 属性
        /// <summary>键</summary>
        public List<Int32> Keys { get; set; }

        /// <summary>值</summary>
        public List<String> Values { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化DNS名称访问器</summary>
        public DNSNameAccessor()
        {
            Keys = new List<Int32>();
            Values = new List<String>();
        }
        #endregion

        #region 方法
        String this[Int32 key]
        {
            get
            {
                var p = Keys.IndexOf(key);
#if DEBUG
                if (p < 0) throw new XException("DNS名称解析未找到序号为{0}的字符串！", key);
#else
                if (p < 0) return null;
#endif
                return Values[p];
            }
        }

        Int32 this[String value]
        {
            get
            {
                var p = Values.IndexOf(value);
                if (p < 0) throw new XException("DNS名称解析未找到字符串为{0}的序号！", value);
                return Keys[p];
            }
        }

        /// <summary>从流中读取字符串</summary>
        /// <param name="stream">数据流</param>
        /// <param name="offset">数据流相对于真正数据的位移</param>
        /// <returns></returns>
        public String Read(Stream stream, Int64 offset)
        {
            // 先用局部变量临时保存，因为每新读出来一段，就要全部加上去
            var keys = new List<Int32>();
            var values = new List<String>();

            var start = stream.Position;
            var p = start;
            var n = 0;
            var sb = new StringBuilder();
            while (true)
            {
                n = stream.ReadByte();
                if (n == 0) break;
                //if (n == -1) n = (Int32)(Byte)n;
                if (n == -1) break;

                if (sb.Length > 0) sb.Append(".");

                String str = null;

                // 0xC0表示是引用，下一个地址指示偏移量
                //if (n == 0xC0)
                if (n >= 0xC0)
                {
                    var n2 = stream.ReadByte();
                    // 偏移量的标准公式是：(Cn-C0)*256+偏移
                    str = this[(n - 0xC0) * 256 + n2];

                    // 之前的每个加上str
                    for (var i = 0; i < values.Count; i++) values[i] += "." + str;

                    // 局部引用，前面还有一段本地读出来的，这样子，整个就形成了一个新的字符串
                    if (sb.Length > 0)
                    {
                        keys.Add((Int32)(offset + start));
                        values.Add(sb + str);
                    }

                    sb.Append(str);

                    break;
                }

                var buffer = stream.ReadBytes(n);
                str = Encoding.UTF8.GetString(buffer);

                // 之前的每个加上str
                for (var i = 0; i < values.Count; i++) values[i] += "." + str;

                // 加入当前项。因为引用项马上就要跳出了，不会做二次引用，所以不加
                if (!values.Contains(str))
                {
                    keys.Add((Int32)(offset + p));
                    values.Add(str);
                }

                sb.Append(str);

                p = stream.Position;
            }
            for (var i = 0; i < keys.Count; i++)
            {
                if (!Values.Contains(values[i]))
                {
                    Keys.Add(keys[i]);
                    Values.Add(values[i]);
                }
            }

            return sb.ToString();
        }

        public void Write(Stream stream, String value, Int64 offset)
        {
            // 先用局部变量临时保存，因为每新读出来一段，就要全部加上去
            var keys = new List<Int32>();
            var values = new List<String>();

            var start = stream.Position;
            var p = 0;
            var isRef = false;
            var ss = ("" + value).Split(".");
            for (var i = 0; i < ss.Length; i++)
            {
                isRef = false;

                // 如果已存在，则写引用
                var name = String.Join(".", ss, i, ss.Length - i);
                if (Values.Contains(name))
                {
                    //stream.WriteByte(0xC0);
                    //stream.WriteByte((Byte)this[name]);

                    // 偏移量的标准公式是：(Cn-C0)*256+偏移
                    // 相对位置，注意超长位移（大于0xFF）
                    var abp = this[name];
                    var ab = abp / 0xFF;
                    abp = abp & 0xFF;
                    stream.WriteByte((Byte)(0xC0 + ab));
                    stream.WriteByte((Byte)abp);

                    // 之前的每个加上str
                    for (var j = 0; j < values.Count; j++) values[j] += "." + name;

                    // 使用引用的必然是最后一个
                    isRef = true;

                    break;
                }

                // 否则，先写长度，后存入引用
                p = (Int32)(stream.Position - start);

                var buffer = Encoding.UTF8.GetBytes(ss[i]);
                stream.WriteByte((Byte)buffer.Length);
                stream.Write(buffer, 0, buffer.Length);

                // 之前的每个加上str
                for (var j = 0; j < values.Count; j++) values[j] += "." + ss[i];

                // 加入当前项
                keys.Add((Int32)(offset + p));
                values.Add(ss[i]);
            }
            if (!isRef) stream.WriteByte((Byte)0);

            for (var i = 0; i < keys.Count; i++)
            {
                Keys.Add(keys[i]);
                Values.Add(values[i]);
            }
        }
        #endregion
    }
}