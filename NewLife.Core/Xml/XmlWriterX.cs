using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using NewLife.Collections;
using NewLife.Reflection;
using NewLife.Serialization;
using System.Collections;

namespace NewLife.Xml
{
    /// <summary>
    /// Xml写入器
    /// </summary>
    public class XmlWriterX : WriterBase
    {
        #region 属性
        private XmlWriter _Writer;
        /// <summary>写入器</summary>
        public XmlWriter Writer
        {
            get { return _Writer; }
            set { _Writer = value; }
        }

        private String _RootName;
        /// <summary>根元素名</summary>
        public String RootName
        {
            get { return _RootName; }
            set { _RootName = value; }
        }

        private Boolean _MemberAsAttribute;
        /// <summary>成员作为属性</summary>
        public Boolean MemberAsAttribute
        {
            get { return _MemberAsAttribute; }
            set { _MemberAsAttribute = value; }
        }

        private Boolean _IgnoreDefault;
        /// <summary>忽略默认</summary>
        public Boolean IgnoreDefault
        {
            get { return _IgnoreDefault; }
            set { _IgnoreDefault = value; }
        }
        #endregion

        #region 基础元数据
        #region 字节
        /// <summary>
        /// 将一个无符号字节写入
        /// </summary>
        /// <param name="value">要写入的无符号字节。</param>
        public override void Write(Byte value)
        {
            Write(new Byte[] { value });
        }

        /// <summary>
        /// 将字节数组部分写入当前流。
        /// </summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="index">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public override void Write(byte[] buffer, int index, int count)
        {
            if (buffer == null || buffer.Length < 1 || count <= 0 || index >= buffer.Length) return;

            Writer.WriteBase64(buffer, index, count);
        }
        #endregion

        #region 有符号整数
        /// <summary>
        /// 将 2 字节有符号整数写入当前流，并将流的位置提升 2 个字节。
        /// </summary>
        /// <param name="value">要写入的 2 字节有符号整数。</param>
        public override void Write(short value) { Writer.WriteValue(value); }

        /// <summary>
        /// 将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节有符号整数。</param>
        public override void Write(int value) { Writer.WriteValue(value); }

        /// <summary>
        /// 将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节有符号整数。</param>
        public override void Write(long value) { Writer.WriteValue(value); }
        #endregion

        #region 浮点数
        /// <summary>
        /// 将 4 字节浮点值写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节浮点值。</param>
        public override void Write(float value) { Writer.WriteValue(value); }

        /// <summary>
        /// 将 8 字节浮点值写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节浮点值。</param>
        public override void Write(double value) { Writer.WriteValue(value); }
        #endregion

        #region 字符串
        /// <summary>
        /// 将字符数组部分写入当前流，并根据所使用的 Encoding（可能还根据向流中写入的特定字符），提升流的当前位置。
        /// </summary>
        /// <param name="chars">包含要写入的数据的字符数组。</param>
        /// <param name="index">chars 中开始写入的起始点。</param>
        /// <param name="count">要写入的字符数。</param>
        public override void Write(char[] chars, int index, int count)
        {
            if (chars == null || chars.Length < 1 || count <= 0 || index >= chars.Length)
            {
                //Write(0);
                return;
            }

            Writer.WriteChars(chars, index, count);
        }

        /// <summary>
        /// 写入字符串
        /// </summary>
        /// <param name="value">要写入的值。</param>
        public override void Write(string value)
        {
            Writer.WriteString(value);
        }
        #endregion

        #region 其它
        /// <summary>
        /// 将单字节 Boolean 值写入
        /// </summary>
        /// <param name="value">要写入的 Boolean 值</param>
        public override void Write(Boolean value) { Writer.WriteValue(value); }

        /// <summary>
        /// 将一个十进制值写入当前流，并将流位置提升十六个字节。
        /// </summary>
        /// <param name="value">要写入的十进制值。</param>
        public override void Write(decimal value) { Writer.WriteValue(value); }

        /// <summary>
        /// 将一个时间日期写入
        /// </summary>
        /// <param name="value"></param>
        public override void Write(DateTime value) { Writer.WriteValue(value); }
        #endregion
        #endregion

        #region 写入对象
        /// <summary>
        /// 已重载。写入文档的开头和结尾
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteObject(object value, Type type, WriteObjectCallback callback)
        {
            if (Depth > 1) return base.WriteObject(value, type, callback);

            if (String.IsNullOrEmpty(RootName))
            {
                if (type == null && value != null) type = value.GetType();
                if (type != null) RootName = type.Name;
            }

            Writer.WriteStartDocument();
            Writer.WriteStartElement(RootName);

            Boolean rs = base.WriteObject(value, type, callback);

            Writer.WriteEndElement();
            Writer.WriteEndDocument();
            return rs;
        }

        /// <summary>
        /// 写入成员
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="member">成员</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected override bool WriteMember(object value, IObjectMemberInfo member, WriteObjectCallback callback)
        {
            // 检查成员的值，如果是默认值，则不输出
            if (IgnoreDefault && IsDefault(value, member)) return true;

            if (MemberAsAttribute)
                Writer.WriteStartAttribute(member.Name);
            else
                Writer.WriteStartElement(member.Name);

            Boolean rs = base.WriteMember(value, member, callback);

            //if (MemberAsAttribute)
            //    Writer.WriteEndAttribute();
            //else
            //    Writer.WriteEndElement();
            if (!MemberAsAttribute) Writer.WriteEndElement();

            return rs;
        }
        #endregion

        #region 枚举
        //public override bool WriteEnumerable(IEnumerable value, Type type, WriteObjectCallback callback)
        //{
        //    if (value != null) type = value.GetType();

        //    Writer.WriteStartElement(type.GetElementType().Name + "s");

        //    Boolean rs = base.WriteEnumerable(value, type, callback);

        //    Writer.WriteEndElement();

        //    return rs;
        //}
        #endregion
    }
}