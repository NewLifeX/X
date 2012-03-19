using System;
using NewLife.IO;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Serialization;
using NewLife.Model;
using NewLife.Exceptions;
using NewLife.Reflection;
using System.Xml.Serialization;
using NewLife.Security;

namespace NewLife.Net.ModBus
{
    /// <summary>ModBus实体</summary>
    public class MBEntity
    {
        #region 属性
        [NonSerialized]
        private Boolean _IsAscii;
        /// <summary>使用ASCII通讯，默认RTU</summary>
        [XmlIgnore]
        public Boolean IsAscii { get { return _IsAscii; } set { _IsAscii = value; } }

        private UInt16 _Address;
        /// <summary>地址码</summary>
        public UInt16 Address { get { return _Address; } set { _Address = value; } }

        private MBFunction _Function;
        /// <summary>功能码</summary>
        public MBFunction Function { get { return _Function; } set { _Function = value; } }
        #endregion

        #region 读写
        /// <summary>把当前对象写入数据，包括可能的起始符和结束符</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            var ms = stream;
            // ASCII模式，先写入内存流
            if (IsAscii) ms = new MemoryStream();

            var writer = new BinaryWriterX(ms);
            Set(writer.Settings);

            //writer.Debug = true;
            //writer.EnableTraceStream();

            if (IsAscii) writer.Write(':');

            writer.WriteObject(this);

            if (IsAscii)
            {
                writer.Write('\r');
                writer.Write('\n');
            }

            // ASCII模式，需要转为HEX字符编码
            if (IsAscii)
            {
                ms.Position = 0;
                var data = ms.ReadBytes();
                data = Encoding.ASCII.GetBytes(data.ToHex());

                stream.Write(data, 0, data.Length);
            }
        }

        /// <summary>序列化为数据流</summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            var ms = new MemoryStream();
            Write(ms);
            ms.Position = 0;
            return ms;
        }

        /// <summary>从流中读取消息</summary>
        /// <param name="stream"></param>
        /// <param name="isAscii">是否ASCII方式</param>
        /// <returns></returns>
        public static MBEntity Read(Stream stream, Boolean isAscii = false)
        {
            // ASCII模式，需要先从HEX字符转回来
            var ms = stream;
            if (isAscii)
            {
                var data = stream.ReadBytes();
                data = DataHelper.FromHex(Encoding.ASCII.GetString(data));
                ms = new MemoryStream(data);
            }

            var reader = new BinaryReaderX(ms);
            Set(reader.Settings);

            reader.Debug = true;
            reader.EnableTraceStream();

            if (isAscii && reader.ReadChar() != ':') return null;

            var entity = reader.ReadObject<MBEntity>();
            entity.IsAscii = isAscii;

            if (isAscii && (reader.ReadChar() != '\r' || reader.ReadChar() != '\n')) return null;

            return entity;
        }

        /// <summary>从流中读取消息</summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static TEntity Read<TEntity>(Stream stream) where TEntity : MBEntity
        {
            return Read(stream) as TEntity;
        }

        static void Set(BinarySettings setting)
        {
            //setting.IsBaseFirst = true;
            //setting.EncodeInt = true;
            //setting.UseObjRef = true;
            //setting.UseTypeFullName = false;
        }
        #endregion
    }
}