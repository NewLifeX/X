using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    class BinaryReaderWriterConfig : ReaderWriterConfig
    {
        private Boolean _EncodeInt;
        /// <summary>编码整数</summary>
        public Boolean EncodeInt
        {
            get { return _EncodeInt; }
            set { _EncodeInt = value; }
        }

        ///// <summary>
        ///// 克隆
        ///// </summary>
        ///// <returns></returns>
        //public override ReaderWriterConfig Clone()
        //{
        //    BinaryReaderWriterConfig config = base.Clone() as BinaryReaderWriterConfig;
        //    config.EncodeInt = EncodeInt;
        //    return config;
        //}
    }
}
