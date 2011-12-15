using System;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>序列化设置</summary>
    public class ReaderWriterSetting
    {
        #region 属性
        private Encoding _Encoding;
        /// <summary>字符串编码</summary>
        public virtual Encoding Encoding
        {
            get { return _Encoding ?? (_Encoding = Encoding.UTF8); }
            set { _Encoding = value; }
        }

        private Boolean _UseTypeFullName = true;
        /// <summary>是否使用类型全名。类型全名能比程序集唯一名更节省空间</summary>
        public Boolean UseTypeFullName
        {
            get { return _UseTypeFullName; }
            set { _UseTypeFullName = value; }
        }

        private Boolean _UseObjRef = true;
        /// <summary>是否使用对象引用</summary>
        public virtual Boolean UseObjRef
        {
            get { return _UseObjRef; }
            set { _UseObjRef = value; }
        }

        private Boolean _AutoFlush;
        /// <summary>自动刷新输出</summary>
        public Boolean AutoFlush
        {
            get { return _AutoFlush; }
            set { _AutoFlush = value; }
        }
        #endregion

        #region 时间日期
        private DateTimeFormats _DateTimeFormat;
        /// <summary>时间日期格式</summary>
        public virtual DateTimeFormats DateTimeFormat
        {
            get { return _DateTimeFormat; }
            set { _DateTimeFormat = value; }
        }

        /// <summary>
        /// 编码时间日期的起始时间，固定1970-01-01
        /// </summary>
        static readonly DateTime _BaseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>编码时间日期的起始时间</summary>
        public virtual DateTime BaseDateTime
        {
            get { return _BaseDateTime; }
        }

        /// <summary>
        /// 转换时间为64位整数，默认返回毫秒数,具体返回值取决于DateTimeFormat成员的值
        /// </summary>
        /// <param name="value">时间</param>
        /// <returns></returns>
        public virtual Int64 ConvertDateTimeToInt64(DateTime value)
        {
            switch (DateTimeFormat)
            {
                case DateTimeFormats.Ticks:
                    return value.Ticks;
                case DateTimeFormats.Milliseconds:
                    return (Int64)(value - BaseDateTime).TotalMilliseconds;
                case DateTimeFormats.Seconds:
                    return (Int64)(value - BaseDateTime).Seconds;
                default:
                    break;
            }

            return value.Ticks;
        }

        /// <summary>
        /// 转换64位整数为时间
        /// </summary>
        /// <param name="value">64位整数</param>
        /// <returns></returns>
        public virtual DateTime ConvertInt64ToDateTime(Int64 value)
        {
            switch (DateTimeFormat)
            {
                case DateTimeFormats.Ticks:
                    return new DateTime(value);
                case DateTimeFormats.Milliseconds:
                    return BaseDateTime.AddMilliseconds(value);
                case DateTimeFormats.Seconds:
                    return BaseDateTime.AddSeconds(value);
                default:
                    break;
            }

            return new DateTime(value);
        }

        /// <summary>
        /// 时间日期格式
        /// </summary>
        public enum DateTimeFormats
        {
            /// <summary>
            /// 嘀嗒数。相对较精确，但是占用空间较大,非utc时间
            /// </summary>
            Ticks,

            /// <summary>
            /// 毫秒数。Json常用格式.指定时间格式为与UTC时间1970.1.1 0:0:0之间的毫秒数
            /// </summary>
            Milliseconds,

            /// <summary>
            /// 秒数。相对较不准确，但占用空间最小，能满足日常要求.指定时间格式为与UTC时间1970.1.1 0:0:0之间的秒数
            /// </summary>
            Seconds,
        }
        #endregion

        #region 类型
        //private TypeFormats _TypeFormat = TypeFormats.FullName;
        ///// <summary>类型格式</summary>
        //public virtual TypeFormats TypeFormat
        //{
        //    get { return _TypeFormat; }
        //    set { _TypeFormat = value; }
        //}

        ///// <summary>
        ///// 类型格式
        ///// </summary>
        //public enum TypeFormats
        //{
        //    /// <summary>
        //    /// 程序集唯一名。精确，但占用空间大
        //    /// </summary>
        //    AssemblyQualifiedName,

        //    /// <summary>
        //    /// 全名。常用
        //    /// </summary>
        //    FullName,

        //    /// <summary>
        //    /// 全名，扩展数组、泛型、内嵌类型
        //    /// </summary>
        //    FullNameExtend
        //}
        #endregion

        #region 大小格式
        private TypeCode _SizeFormat = TypeCode.Int32;
        /// <summary>大小格式。只能是数字，无符号整数表示使用相应的压缩编码，压缩编码仅二进制序列化有效</summary>
        public TypeCode SizeFormat
        {
            get { return _SizeFormat; }
            set
            {
                _SizeFormat = value;
                if (_SizeFormat < TypeCode.Int16)
                    _SizeFormat = TypeCode.Int16;
                else if (_SizeFormat > TypeCode.UInt64)
                    _SizeFormat = TypeCode.UInt64;
            }
        }
        #endregion
    }
}