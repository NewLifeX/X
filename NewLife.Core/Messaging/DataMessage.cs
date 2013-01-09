using System;
using System.Xml.Serialization;
using NewLife.Serialization;
using System.IO;

namespace NewLife.Messaging
{
    /// <summary>指定长度的字节数据消息</summary>
    /// <remarks>
    /// 一般用于对数据进行二次包装，理论上，这是一个万能消息。
    /// 数据长度由<see cref="Data"/>决定，以编码整数来存储。
    /// </remarks>
    public class DataMessage : Message
    {
        /// <summary>消息类型</summary>
        [XmlIgnore]
        public override MessageKind Kind { get { return MessageKind.Data; } }

        //private Int32 _Length;
        ///// <summary>长度</summary>
        //public Int32 Length { get { return _Length; } set { _Length = value; } }

        private Byte[] _Data;
        /// <summary>数据</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; } }

        /// <summary>已重载。</summary>
        /// <param name="stream">数据流</param>
        /// <param name="rwkind">序列化类型</param>
        protected override void OnWrite(Stream stream, RWKinds rwkind)
        {
            // 因为不写对象引用，所以不能为null
            if (Data == null) Data = new Byte[0];

            base.OnWrite(stream, rwkind);
        }

        /// <summary>读写前设置。不使用对象引用</summary>
        /// <param name="setting"></param>
        protected override void OnReadWriteSet(ReaderWriterSetting setting)
        {
            base.OnReadWriteSet(setting);

            setting.UseObjRef = false;
        }

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var data = Data;
            return String.Format("{0} Length={1}", base.ToString(), data != null ? data.Length : 0);
        }
        #endregion
    }
}