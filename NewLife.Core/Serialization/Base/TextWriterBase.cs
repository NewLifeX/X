using System;
using System.Net;
using NewLife.IO;

namespace NewLife.Serialization
{
    /// <summary>文本写入器基类</summary>
    /// <typeparam name="TSettings">设置类</typeparam>
    public abstract class TextWriterBase<TSettings> : WriterBase<TSettings> where TSettings : TextReaderWriterSetting, new()
    {
        #region 属性
        /// <summary>是否使用大小，如果使用，将在写入数组、集合和字符串前预先写入大小。字符串类型读写器一般带有边界，不需要使用大小</summary>
        protected override Boolean UseSize { get { return false; } }
        #endregion

        #region 基础元数据
        #region 字节
        /// <summary>将一个无符号字节写入</summary>
        /// <param name="value">要写入的无符号字节。</param>
        public override void Write(Byte value) { WriteLiteral(string.Format("{0}", value)); }
        //public override void Write(Byte value) { Write(new Byte[] { value }, 0, 1); }

        /// <summary>
        /// 将字节数组以[0xff,0xff,0xff]的格式写入
        /// </summary>
        /// <param name="buffer"></param>
        public override void Write(byte[] buffer)
        {
            if (buffer == null)
                WriteNull();
            else
                Write(buffer, 0, buffer.Length);
        }

        /// <summary>将字节数组部分写入当前流。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="index">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public override void Write(byte[] buffer, int index, int count)
        {
            if (buffer == null || buffer.Length < 1 || count <= 0 || index >= buffer.Length) return;

            if (Settings.UseBase64)
                WriteLiteral(Convert.ToBase64String(buffer, index, count));
            else
                WriteLiteral(BitConverter.ToString(buffer, index, count).Replace("-", null));

            AutoFlush();
        }
        #endregion

        #region 有符号整数
        /// <summary>
        /// 将 2 字节有符号整数写入当前流，并将流的位置提升 2 个字节。
        /// </summary>
        /// <param name="value">要写入的 2 字节有符号整数。</param>
        public override void Write(short value) { Write((Int32)value); }

        /// <summary>
        /// 将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节有符号整数。</param>
        public override void Write(int value) { Write((Int64)value); }

        /// <summary>
        /// 将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节有符号整数。</param>
        public override void Write(long value) { WriteLiteral(value.ToString()); }
        #endregion

        #region 浮点数
        /// <summary>
        /// 将 4 字节浮点值写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节浮点值。</param>
        public override void Write(float value) { WriteLiteral(value.ToString()); }

        /// <summary>
        /// 将 8 字节浮点值写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节浮点值。</param>
        public override void Write(double value) { WriteLiteral(value.ToString()); }
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
            if (chars == null || chars.Length < 1 || count <= 0 || index >= chars.Length) return;

            Write(new String(chars, index, count));
        }

        /// <summary>
        /// 写入字符串
        /// </summary>
        /// <param name="value">要写入的值。</param>
        public override void Write(String value) { WriteLiteral(value); }

        /// <summary>输出字符串字面值,不做编码处理</summary>
        /// <param name="value"></param>
        protected void WriteLiteral(String value)
        {
            Depth++;
            WriteLog("WriteLiteral", value);
            OnWriteLiteral(value);
            Depth--;
        }

        /// <summary>输出字符串字面值,不做编码处理</summary>
        /// <param name="value"></param>
        protected virtual void OnWriteLiteral(String value) { throw new NotImplementedException(); }

        /// <summary>输出空</summary>
        protected virtual void WriteNull() { }
        #endregion

        #region 其它
        /// <summary>
        /// 将单字节 Boolean 值写入
        /// </summary>
        /// <param name="value">要写入的 Boolean 值</param>
        public override void Write(Boolean value) { WriteLiteral(value ? "true" : "false"); }
        //public override void Write(Boolean value) { Write(value.ToString()); }

        /// <summary>
        /// 将一个十进制值写入当前流，并将流位置提升十六个字节。
        /// </summary>
        /// <param name="value">要写入的十进制值。</param>
        public override void Write(decimal value) { WriteLiteral(value.ToString()); }

        /// <summary>
        /// 将一个时间日期写入
        /// </summary>
        /// <param name="value"></param>
        public override void Write(DateTime value) { WriteLiteral(value.ToString("yyyy-MM-dd HH:mm:ss")); }
        #endregion
        #endregion

        #region 扩展类型
        /// <summary>写入Guid</summary>
        /// <param name="value"></param>
        protected override void OnWrite(Guid value) { Write(value.ToString()); }

        /// <summary>写入IPAddress</summary>
        /// <param name="value"></param>
        protected override void OnWrite(IPAddress value) { Write(value.ToString()); }

        /// <summary>写入IPEndPoint</summary>
        /// <param name="value"></param>
        protected override void OnWrite(IPEndPoint value) { Write(value.ToString()); }
        #endregion

        #region 写入值类型
        /// <summary>
        /// 写入值类型，只能识别基础类型，对于不能识别的类型，方法返回false
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <returns>是否写入成功</returns>
        protected override bool WriteValue(object value, Type type)
        {
            if (value != null && Settings.UseEnumName)
            {
                if (type != null && type.IsEnum)
                {
                    Write(value.ToString());
                    return true;
                }
            }
            return base.WriteValue(value, type);
        }
        #endregion

        #region 扩展
        /// <summary>把数据流转为字符串</summary>
        /// <returns></returns>
        public virtual String ToStr()
        {
            if (!Stream.CanSeek) throw new XSerializationException(null, "数据流不支持移动，无法转字符串！");

            Flush();

            // 保持位置，读取后恢复
            Int64 p = Stream.Position;
            Stream.Position = 0;
            String txt = Stream.ToStr(Settings.Encoding);
            Stream.Position = p;
            return txt;
        }
        #endregion
    }
}