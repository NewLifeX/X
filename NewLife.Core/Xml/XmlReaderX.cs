using System;
using System.Collections.Generic;
using System.Text;
using NewLife.IO;
using NewLife.Serialization;
using System.Xml;
using System.Reflection;

namespace NewLife.Xml
{
    /// <summary>
    /// Xml读取器
    /// </summary>
    public class XmlReaderX : ReaderBase
    {
        #region 属性
        private XmlReader _Reader;
        /// <summary>读取器</summary>
        public XmlReader Reader
        {
            get { return _Reader; }
            set { _Reader = value; }
        }

        private String _RootName;
        /// <summary>根元素名</summary>
        public String RootName
        {
            get { return _RootName; }
            set { _RootName = value; }
        }

        private XmlMemberStyle _MemberStyle;
        /// <summary>成员样式</summary>
        public XmlMemberStyle MemberStyle
        {
            get { return _MemberStyle; }
            set { _MemberStyle = value; }
        }

        private Boolean _IgnoreDefault;
        /// <summary>忽略默认</summary>
        public Boolean IgnoreDefault
        {
            get { return _IgnoreDefault; }
            set { _IgnoreDefault = value; }
        }
        #endregion

        #region 读取基础元数据
        #region 字节
        /// <summary>
        /// 从当前流中读取下一个字节，并使流的当前位置提升 1 个字节。
        /// </summary>
        /// <returns></returns>
        public override byte ReadByte() { return ReadBytes(1)[0]; }

        /// <summary>
        /// 从当前流中将 count 个字节读入字节数组，并使当前位置提升 count 个字节。
        /// </summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        public override byte[] ReadBytes(int count)
        {
            if (count <= 0) return null;

            Byte[] buffer = new Byte[count];
            Int32 n = Reader.ReadContentAsBase64(buffer, 0, count);

            if (n == count) return buffer;

            Byte[] data = new Byte[n];
            Buffer.BlockCopy(buffer, 0, data, 0, n);

            return data;
        }
        #endregion

        #region 有符号整数
        /// <summary>
        /// 从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。
        /// </summary>
        /// <returns></returns>
        public override short ReadInt16() { return (Int16)ReadInt32(); }

        /// <summary>
        /// 从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        public override int ReadInt32() { return Reader.ReadContentAsInt(); }

        /// <summary>
        /// 从当前流中读取 8 字节有符号整数，并使流的当前位置向前移动 8 个字节。
        /// </summary>
        /// <returns></returns>
        public override long ReadInt64() { return Reader.ReadContentAsLong(); }
        #endregion

        #region 浮点数
        /// <summary>
        /// 从当前流中读取 4 字节浮点值，并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        public override float ReadSingle() { return Reader.ReadContentAsFloat(); }

        /// <summary>
        /// 从当前流中读取 8 字节浮点值，并使流的当前位置提升 8 个字节。
        /// </summary>
        /// <returns></returns>
        public override double ReadDouble() { return Reader.ReadContentAsDouble(); }
        #endregion

        #region 字符串
        /// <summary>
        /// 从当前流中读取 count 个字符，以字符数组的形式返回数据，并根据所使用的 Encoding 和从流中读取的特定字符，提升当前位置。
        /// </summary>
        /// <param name="count">要读取的字符数。</param>
        /// <returns></returns>
        public override char[] ReadChars(int count)
        {
            // count个字符可能的最大字节数
            Int32 max = Encoding.GetMaxByteCount(count);

            // 首先按最小值读取
            Byte[] data = ReadBytes(count);

            // 相同，最简单的一种
            if (max == count) return Encoding.GetChars(data);

            // 按最大值准备一个字节数组
            Byte[] buffer = new Byte[max];
            // 复制过去
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);

            // 遍历，以下算法性能较差，将来可以考虑优化
            Int32 i = 0;
            for (i = count; i < max; i++)
            {
                Int32 n = Encoding.GetCharCount(buffer, 0, i);
                if (n >= count) break;

                buffer[i] = ReadByte();
            }

            return Encoding.GetChars(buffer, 0, i);
        }

        /// <summary>
        /// 从当前流中读取一个字符串。字符串有长度前缀，一次 7 位地被编码为整数。
        /// </summary>
        /// <returns></returns>
        public override string ReadString() { return Reader.ReadContentAsString(); }
        #endregion

        #region 其它
        /// <summary>
        /// 从当前流中读取 Boolean 值，并使该流的当前位置提升 1 个字节。
        /// </summary>
        /// <returns></returns>
        public override bool ReadBoolean() { return Reader.ReadContentAsBoolean(); }

        /// <summary>
        /// 从当前流中读取十进制数值，并将该流的当前位置提升十六个字节。
        /// </summary>
        /// <returns></returns>
        public override decimal ReadDecimal() { return Reader.ReadContentAsDecimal(); }

        /// <summary>
        /// 读取一个时间日期
        /// </summary>
        /// <returns></returns>
        public override DateTime ReadDateTime() { return Reader.ReadContentAsDateTime(); }
        #endregion
        #endregion

        #region 读取对象
        /// <summary>
        /// 尝试读取目标对象指定成员的值，通过委托方法递归处理成员
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="config">配置</param>
        /// <returns>是否读取成功</returns>
        public override bool ReadObject(Type type, ref object value, ReaderWriterConfig config)
        {
            //Reader.Read();
            //Reader.ReadStartElement();

            while (Reader.NodeType != XmlNodeType.Element) { if (!Reader.Read())return false; }
            RootName = Reader.LocalName;

            if (config == null) config = CreateConfig();
            XmlReaderWriterConfig xconfig = config as XmlReaderWriterConfig;
            if (xconfig != null && xconfig.MemberStyle == XmlMemberStyle.Element) Reader.ReadStartElement();

            Boolean rs = base.ReadObject(type, ref value, config);

            //Reader.ReadEndElement();
            Reader.Read();

            return rs;
        }

        /// <summary>
        /// 读取成员
        /// </summary>
        /// <param name="value">要读取的对象</param>
        /// <param name="member">成员</param>
        /// <param name="config">配置</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        protected override bool ReadMember(ref object value, MemberInfo member, ReaderWriterConfig config, ReadMemberCallback callback)
        {
            XmlReaderWriterConfig xconfig = config as XmlReaderWriterConfig;
            XmlMemberStyle style = xconfig != null ? xconfig.MemberStyle : MemberStyle;

            if (style == XmlMemberStyle.Attribute)
            {
                Reader.MoveToAttribute(member.Name);
            }
            else
            {
                if (Reader.IsEmptyElement)
                {
                    Reader.Read();
                    return true;
                }

                Reader.ReadStartElement(member.Name);
            }

            Boolean rs = base.ReadMember(ref value, member, config, callback);

            if (style == XmlMemberStyle.Attribute)
            {

            }
            else
                Reader.ReadEndElement();

            return rs;
        }
        #endregion

        #region 成员
        /// <summary>
        /// 已重载。过滤掉不能没有Set的属性成员
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected override MemberInfo[] OnGetMembers(Type type)
        {
            MemberInfo[] mis = base.OnGetMembers(type);
            if (mis == null || mis.Length < 1) return mis;

            List<MemberInfo> list = new List<MemberInfo>();
            foreach (MemberInfo item in mis)
            {
                if (item is PropertyInfo)
                {
                    if (!(item as PropertyInfo).CanWrite) continue;
                }
                list.Add(item);
            }
            mis = list.ToArray();

            return mis;
        }
        #endregion

        #region 设置
        /// <summary>
        /// 创建配置
        /// </summary>
        /// <returns></returns>
        protected override ReaderWriterConfig CreateConfig()
        {
            XmlReaderWriterConfig config = new XmlReaderWriterConfig();
            config.MemberStyle = MemberStyle;
            config.IgnoreDefault = IgnoreDefault;
            return config;
        }
        #endregion
    }
}