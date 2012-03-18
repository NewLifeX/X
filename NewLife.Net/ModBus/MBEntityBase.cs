using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Serialization;
using NewLife.Model;
using NewLife.Exceptions;
using NewLife.Reflection;
using System.Xml.Serialization;

namespace NewLife.Net.ModBus
{
    /// <summary>ModBus实体基类。</summary>
    public abstract class MBEntityBase
    {
        #region 属性
        [NonSerialized]
        private MBFunction _Function;
        /// <summary>功能码</summary>
        [XmlIgnore]
        public MBFunction Function { get { return _Function; } set { _Function = value; } }
        #endregion

        #region 读写
        public virtual void Write(Stream stream)
        {
            //stream.WriteByte(0x3A);
            WriteRaw(stream);
            //stream.WriteByte(0x10);
            //stream.WriteByte(0x10);
        }

        public void WriteRaw(Stream stream)
        {
            var writer = new BinaryWriterX(stream);
            Set(writer.Settings);

            //writer.Debug = true;
            //writer.EnableTraceStream();

            // 基类写入编号，保证编号在最前面
            writer.Write((Byte)Function);
            writer.WriteObject(this);
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
        /// <returns></returns>
        public static MBEntityBase Read(Stream stream)
        {
            var reader = new BinaryReaderX(stream);
            Set(reader.Settings);

            reader.Debug = true;
            reader.EnableTraceStream();

            // 读取了响应类型和消息类型后，动态创建消息对象
            var kind = (MBFunction)reader.ReadByte();
            var type = ObjectContainer.Current.ResolveType<MBEntityBase>(kind);
            if (type == null) throw new XException("无法识别的消息类型（Kind={0}）！", kind);

            if (stream.Position == stream.Length) return TypeX.CreateInstance(type, null) as MBEntityBase;

            try
            {
                return reader.ReadObject(type) as MBEntityBase;
            }
            catch (Exception ex) { throw new XException(String.Format("无法从数据流中读取{0}（Kind={1}）消息！", type.Name, kind), ex); }
        }

        /// <summary>从流中读取消息</summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static TEntity Read<TEntity>(Stream stream) where TEntity : MBEntityBase
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