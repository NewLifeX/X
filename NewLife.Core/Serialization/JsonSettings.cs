using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// Json设置
    /// </summary>
    public class JsonSettings : StringReaderWriterSetting
    {
        #region 属性
        private Boolean _Indent;
        /// <summary>缩进</summary>
        public Boolean Indent
        {
            get { return _Indent; }
            set { _Indent = value; }
        }

        private Boolean _JsDateTimeFormat;

        /// <summary>
        /// 是否使用JavaScript时间格式
        ///  为true将得到new Date(Date.UTC(2011,4,4,7,34,40,279))类似的Date类型,方便查看
        ///  为false将得到new Date(1304494480279)类似的Date类型,其中数字部分代表的意义取决于DateTimeFormat的配置,数据长度小
        /// 
        /// 始终传递的是UTC时间,而在客户端new Date时将自动转换成客户端时间
        /// </summary>
        public Boolean JsDateTimeFormat
        {
            get { return _JsDateTimeFormat; }
            set { _JsDateTimeFormat = value; }
        }

        private bool _JsEncodeUnicode;
        /// <summary>
        /// 是否编码字符串中Unicode字符为\uXXXX的格式
        /// 
        /// 可以避免乱码问题,但是会增加数据长度
        /// </summary>
        public Boolean JsEncodeUnicode
        {
            get { return _JsEncodeUnicode; }
            set { _JsEncodeUnicode = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        public JsonSettings()
        {
            // 指定时间的格式
            DateTimeFormat = DateTimeFormats.Milliseconds;
        }
        #endregion
    }
}
