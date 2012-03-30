using System;
using System.Collections.Generic;
using System.IO;

namespace NewLife.Serialization
{
    /// <summary>名值读取器。用于Http请求、Http接口响应、Cookie值等读写操作。</summary>
    public class NameValueReader : TextReaderBase<NameValueSetting>
    {
        #region 属性
        private TextReader _Reader;
        /// <summary>读取器</summary>
        public TextReader Reader
        {
            get { return _Reader ?? (_Reader = new StreamReader(Stream, Settings.Encoding)); }
            set
            {
                _Reader = value;

                StreamReader sr = _Reader as StreamReader;
                if (sr != null)
                {
                    if (Settings.Encoding != sr.CurrentEncoding) Settings.Encoding = sr.CurrentEncoding;
                    if (Stream != sr.BaseStream) Stream = sr.BaseStream;
                }
            }
        }

        /// <summary>数据流。更改数据流后，重置Reader为空，以使用新的数据流</summary>
        public override Stream Stream
        {
            get { return base.Stream; }
            set
            {
                if (base.Stream != value) _Reader = null;
                base.Stream = value;
            }
        }
        #endregion

        #region 方法
        /// <summary>备份当前环境，用于临时切换数据流等</summary>
        /// <returns>本次备份项集合</returns>
        public override IDictionary<String, Object> Backup()
        {
            var dic = base.Backup();
            dic["Reader"] = Reader;

            return dic;
        }

        /// <summary>恢复最近一次备份</summary>
        /// <returns>本次还原项集合</returns>
        public override IDictionary<String, Object> Restore()
        {
            var dic = base.Restore();
            Reader = dic["Reader"] as TextReader;

            return dic;
        }
        #endregion
    }
}