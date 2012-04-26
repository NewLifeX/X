using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using NewLife.Exceptions;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

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

        [NonSerialized]
        private Boolean _UseAddress;
        /// <summary>是否使用地址。实际使用中，没有总线结构时，地址是没有意义的</summary>
        [XmlIgnore]
        public Boolean UseAddress { get { return _UseAddress; } set { _UseAddress = value; } }

        [NonSerialized]
        private Byte _Address;
        /// <summary>地址码</summary>
        [XmlIgnore]
        public Byte Address { get { return _Address; } set { _Address = value; UseAddress = value > 0; } }

        [NonSerialized]
        private MBFunction _Function;
        /// <summary>功能码</summary>
        [XmlIgnore]
        public MBFunction Function { get { return _Function; } set { _Function = value; } }

        [NonSerialized]
        private Byte[] _ExtendData;
        /// <summary>扩展业务数据，对于某些没有实现或者未完全实现的功能码有用</summary>
        [XmlIgnore]
        public Byte[] ExtendData { get { return _ExtendData; } set { _ExtendData = value; } }
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

            if (UseAddress) writer.Write(Address);
            writer.Write((Byte)Function);

            writer.WriteObject(this);

            var dt = ExtendData;
            if (dt != null && dt.Length > 0) writer.Write(dt, 0, dt.Length);

            // 计算CRC
            ms.Position = 0;
            if (IsAscii)
            {
                // 累加后取补码
                var crc = (Byte)ms.ReadBytes().Select(e => (Int32)e).Sum();
                //crc = (Byte)(~crc + 1);
                crc = (Byte)(0x100 - crc);
                writer.Write(crc);
            }
            else
            {
                var crc = new Crc16().Update(ms).Value;
                writer.Settings.IsLittleEndian = true;
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
        /// <param name="isResponse">是否响应</param>
        /// <param name="useAddress">是否使用地址</param>
        /// <param name="isAscii">是否ASCII方式</param>
        /// <returns></returns>
        public static MBEntity Read(Stream stream, Boolean isResponse = false, Boolean useAddress = false, Boolean isAscii = false)
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

            // 读取地址和功能码
            var addr = useAddress ? reader.ReadByte() : (Byte)0;
            var func = (MBFunction)reader.ReadByte();

            var type = !isResponse ? ObjectContainer.Current.ResolveType<IModBusRequest>(func) : ObjectContainer.Current.ResolveType<IModBusResponse>(func);
            //if (type == null) throw new XException("无法识别的消息类型（Function={0}）！", func);
            if (type == null) type = typeof(MBEntity);

            if (stream.Position == stream.Length) return TypeX.CreateInstance(type, null) as MBEntity;

            MBEntity entity = null;
            try
            {
                entity = reader.ReadObject(type) as MBEntity;
                if (entity == null) entity = TypeX.CreateInstance(type, null) as MBEntity;

                // 读取剩余的数据，注意扣除校验部分
                var len = ms.Length - ms.Position - (isAscii ? 1 : 2);
                if (len > 0) entity.ExtendData = reader.ReadBytes((Int32)len);
            }
            catch (Exception ex) { throw new XException(String.Format("无法从数据流中读取{0}（Function={1}）消息！", type.Name, func), ex); }

            entity.UseAddress = useAddress;
            entity.IsAscii = isAscii;
            entity.Address = addr;
            entity.Function = func;

            // 计算Crc
            var ori = ms.Position;
            ms.Position = start;
            if (isAscii)
            {
                var crc = (Byte)ms.ReadBytes(ori - start).Select<Byte, Int32>(e => (Int32)e).Sum();
                if (~crc != reader.ReadByte()) throw new Exception("Crc校验失败！");
            }
            else
            {
                var crc = new Crc16().Update(ms, ori - start).Value;
                reader.Settings.IsLittleEndian = true;
                if (crc != reader.ReadUInt16()) throw new Exception("Crc校验失败！");
            }

            if (isAscii && (reader.ReadChar() != '\r' || reader.ReadChar() != '\n')) return null;

            return entity;
        }

        /// <summary>从流中读取消息</summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="stream"></param>
        /// <param name="isResponse">是否响应</param>
        /// <param name="useAddress">是否使用地址</param>
        /// <param name="isAscii">是否ASCII方式</param>
        /// <returns></returns>
        public static TEntity Read<TEntity>(Stream stream, Boolean isResponse = false, Boolean useAddress = false, Boolean isAscii = false) where TEntity : MBEntity
        {
            return Read(stream, isResponse, useAddress, isAscii) as TEntity;
        }

        static void Set(IReaderWriter rw)
        {
            var setting = rw.Settings as BinarySettings;
            setting.IsLittleEndian = false;
            //setting.IsBaseFirst = true;
            //setting.EncodeInt = true;
            setting.UseObjRef = false;
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

        #region 业务处理
        /// <summary>处理消息，通过数据回调来发出或收回数据</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback">用于处理数据的回调方法，其中第二个参数是用户变量。
        /// 回调方法为空时，默认采用串口通信，用户变量指定串口对象或串口名，不指定时采用COM1</param>
        /// <param name="state">需要传递给回调方法的用户变量</param>
        /// <returns></returns>
        public T Process<T>(Func<Byte[], Object, Byte[]> callback = null, Object state = null) where T : MBEntity
        {
            return (T)Process(this, callback, state);
        }

        /// <summary>处理消息，通过数据回调来发出或收回数据</summary>
        /// <param name="request">需要发出的请求消息</param>
        /// <param name="callback">用于处理数据的回调方法，其中第二个参数是用户变量。
        /// 回调方法为空时，默认采用串口通信，用户变量指定串口对象或串口名，不指定时采用COM1</param>
        /// <param name="state">需要传递给回调方法的用户变量</param>
        /// <returns></returns>
        public static MBEntity Process(MBEntity request, Func<Byte[], Object, Byte[]> callback = null, Object state = null)
        {
            var dt = request.GetStream().ReadBytes();
            if (request.IsAscii)
                WriteLog("发送：{0}", Encoding.ASCII.GetString(dt));
            else
                WriteLog("发送：{0}", BitConverter.ToString(dt));

            if (callback == null) callback = ReadSerialPort;
            var data = callback(dt, state);
            if (data == null || data.Length < 1) return null;

            WriteLog("接收：{0}", BitConverter.ToString(data));

            var ms = new MemoryStream(data);
            return Read(ms, true, request.UseAddress, request.IsAscii);
        }

        static Byte[] ReadSerialPort(Byte[] dt, Object state)
        {
            var sp = state as SerialPort;
            if (sp == null)
            {
                sp = new SerialPort(state == null ? "COM1" : "" + state);
                sp.ReadTimeout = sp.WriteTimeout = 500;
                sp.Open();
            }

            try
            {
                sp.Write(dt, 0, dt.Length);

                dt = new Byte[256];
                Int32 i = 0;
                do
                {
                    try
                    {
                        var count = sp.Read(dt, i, dt.Length - i);
                        i += count;
                    }
                    catch { }
                    //if (i >= dt.Length) break;
                    //Thread.Sleep(1000);
                } while (i < dt.Length && sp.BytesToRead > 0);
                if (i <= 0) return null;

                //var data = new Byte[i];
                //Buffer.BlockCopy(dt, 0, data, 0, data.Length);

                return dt.ReadBytes(0, i);
            }
            finally
            {
                // 如果不是外部传入，则销毁
                if (!(state is SerialPort)) sp.Dispose();
            }
        }
        #endregion

        #region 辅助
        [Conditional("DEBUG")]
        static void WriteLog(String format, params Object[] args)
        {
            if (XTrace.Debug) XTrace.WriteLine(format, args);
        }
        #endregion
    }
}