using System;
using NewLife.Linq;
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
using System.Reflection;
using System.Diagnostics;

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

        #region 构造、注册
        static MBEntity()
        {
            Init();
        }

        /// <summary>初始化</summary>
        static void Init()
        {
            var container = ObjectContainer.Current;
            var asm = Assembly.GetExecutingAssembly();
            // 搜索已加载程序集里面的消息类型
            foreach (var item in AssemblyX.FindAllPlugins(typeof(IModBusRequest), true))
            {
                var msg = TypeX.CreateInstance(item) as IModBusRequest;
                if (msg != null) container.Register(typeof(IModBusRequest), item, null, msg.Function);
            }
            foreach (var item in AssemblyX.FindAllPlugins(typeof(IModBusResponse), true))
            {
                var msg = TypeX.CreateInstance(item) as IModBusResponse;
                if (msg != null) container.Register(typeof(IModBusResponse), item, null, msg.Function);
            }
        }
        #endregion

        #region 读写
        /// <summary>把当前对象写入数据，包括可能的起始符和结束符</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            // ASCII模式，先写入内存流
            var ms = new MemoryStream();
            var writer = new BinaryWriterX(ms);
            Set(writer);

            writer.WriteObject(this);

            // 计算CRC
            ms.Position = 0;
            if (IsAscii)
            {
                // 累加后取补码
                var crc = (Byte)ms.ReadBytes().Cast<Int32>().Sum();
                writer.Write(~crc);
            }
            else
            {
                var crc = new Crc32().Update(ms).Value;
                writer.Write(crc);
            }

            // ASCII模式，需要转为HEX字符编码
            if (IsAscii)
            {
                ms.Position = 0;
                var data = ms.ReadBytes();
                data = Encoding.ASCII.GetBytes(data.ToHex());

                writer.Stream = stream;
                writer.Write(':');
                writer.Write(data, 0, data.Length);
                writer.Write('\r');
                writer.Write('\n');
            }
            else
            {
                ms.Position = 0;
                ms.CopyTo(stream);
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

            var start = ms.Position;
            var reader = new BinaryReaderX(ms);
            Set(reader);

            if (isAscii && reader.ReadChar() != ':') return null;

            //var entity = reader.ReadObject<MBEntity>();

            // 读取了响应类型和消息类型后，动态创建消息对象
            var kind = (MBFunction)reader.ReadByte();
            var type = ObjectContainer.Current.ResolveType<MBEntity>(kind);
            if (type == null) throw new XException("无法识别的消息类型（Function={0}）！", kind);

            if (stream.Position == stream.Length) return TypeX.CreateInstance(type, null) as MBEntity;

            MBEntity entity = null;
            try
            {
                entity = reader.ReadObject(type) as MBEntity;
            }
            catch (Exception ex) { throw new XException(String.Format("无法从数据流中读取{0}（Function={1}）消息！", type.Name, kind), ex); }

            entity.IsAscii = isAscii;

            // 计算Crc
            var ori = ms.Position;
            ms.Position = start;
            if (isAscii)
            {
                var crc = (Byte)ms.ReadBytes(ori - start).Cast<Int32>().Sum();
                if (~crc != reader.ReadByte()) throw new Exception("Crc校验失败！");
            }
            else
            {
                var crc = new Crc32().Update(ms, ori - start).Value;
                if (crc != reader.ReadUInt16()) throw new Exception("Crc校验失败！");
            }

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

        static void Set(IReaderWriter rw)
        {
            var settting = rw.Settings;
            //setting.IsBaseFirst = true;
            //setting.EncodeInt = true;
            //setting.UseObjRef = true;
            //setting.UseTypeFullName = false;

            //SetDebug(rw);
        }

        [Conditional("DEBUG")]
        static void SetDebug(IReaderWriter rw)
        {
            rw.Debug = true;
            rw.EnableTraceStream();
        }
        #endregion
    }
}