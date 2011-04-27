using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// Json设置
    /// </summary>
    public class JsonSettings : SerialSettings
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
        /// <summary>是否使用JavaScript时间格式</summary>
        public Boolean JsDateTimeFormat
        {
            get { return _JsDateTimeFormat; }
            set { _JsDateTimeFormat = value; }
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
