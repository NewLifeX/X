using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Serialization;
using NewLife.Net.Sockets;
using NewLife.Net;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>DNS实体类基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class DNSBase<TEntity> : DNSEntity where TEntity : DNSBase<TEntity>
    {
        #region 读写
        /// <summary>从数据流中读取对象</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public new static TEntity Read(Stream stream)
        {
            BinaryReaderX reader = new BinaryReaderX();
            reader.Settings.IsLittleEndian = false;
            reader.Settings.UseObjRef = false;
            reader.Stream = stream;
#if DEBUG
            if (NetHelper.Debug)
            {
                reader.Debug = true;
                reader.EnableTraceStream();
            }
#endif
            return reader.ReadObject<TEntity>();
        }
        #endregion
    }

    /// <summary>DNS实体类基类</summary>
    public class DNSEntity : IAccessor
    {
        #region 属性
        private DNSHeader _Header = new DNSHeader();
        /// <summary>头部</summary>
        public DNSHeader Header { get { return _Header; } set { _Header = value; } }

        [FieldSize("_Header._Questions")]
        private DNSRecord[] _Questions;
        /// <summary>请求段</summary>
        public DNSRecord[] Questions { get { return _Questions; } set { _Questions = value; } }

        [FieldSize("_Header._Answers")]
        private DNSRecord[] _Answers;
        /// <summary>回答段</summary>
        public DNSRecord[] Answers { get { return _Answers; } set { _Answers = value; } }

        [FieldSize("_Header._Authorities")]
        private DNSRecord[] _Authoritis;
        /// <summary>授权段</summary>
        public DNSRecord[] Authoritis { get { return _Authoritis; } set { _Authoritis = value; } }

        [FieldSize("_Header._Additionals")]
        private DNSRecord[] _Additionals;
        /// <summary>附加段</summary>
        public DNSRecord[] Additionals { get { return _Additionals; } set { _Additionals = value; } }
        #endregion

        #region 扩展属性
        /// <summary>是否响应</summary>
        public Boolean Response { get { return Header.Response; } set { Header.Response = value; } }

        DNSRecord Question
        {
            get
            {
                if (Questions == null || Questions.Length < 1) Questions = new DNSRecord[] { new DNSRecord() };

                return Questions[0];
            }
        }

        /// <summary>名称</summary>
        public String Name { get { return Question.Name; } set { Question.Name = value; } }

        /// <summary>查询类型</summary>
        public DNSQueryType Type { get { return Question.Type; } set { Question.Type = value; } }

        /// <summary>协议组</summary>
        public DNSQueryClass Class { get { return Question.Class; } set { Question.Class = value; } }

        protected DNSRecord GetAnswer(Boolean create = false)
        {
            if (Answers == null || Answers.Length < 1)
            {
                if (!create) return null;

                Answers = new DNSRecord[] { new DNSRecord() };

            }

            var type = Question.Type;
            foreach (var item in Answers)
            {
                if (item.Type == type) return item;
            }

            return Questions[0];
        }

        /// <summary>生存时间。指示RDATA中的资源记录在缓存的生存时间。</summary>
        public TimeSpan TTL
        {
            get
            {
                var aw = GetAnswer();
                return aw != null ? aw.TTL : TimeSpan.MinValue;
            }
            set { GetAnswer(true).TTL = value; }
        }

        /// <summary>数据字符串</summary>
        public String DataString
        {
            get
            {
                var aw = GetAnswer();
                return aw != null ? aw.DataString : null;
            }
            set { GetAnswer(true).DataString = value; }
        }
        #endregion

        #region 读写
        /// <summary>把当前对象写入到数据流中去</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            BinaryWriterX writer = new BinaryWriterX();
            writer.Settings.IsLittleEndian = false;
            writer.Settings.UseObjRef = false;
            writer.Stream = stream;
#if DEBUG
            if (NetHelper.Debug)
            {
                writer.Debug = true;
                writer.EnableTraceStream();
            }
#endif
            writer.WriteObject(this);
        }

        /// <summary>获取当前对象的数据流</summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            var ms = new MemoryStream();
            Write(ms);
            ms.Position = 0;

            return ms;
        }

        /// <summary>从数据流中读取对象</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static DNSEntity Read(Stream stream)
        {
            BinaryReaderX reader = new BinaryReaderX();
            reader.Settings.IsLittleEndian = false;
            reader.Settings.UseObjRef = false;
            reader.Stream = stream;
#if DEBUG
            if (NetHelper.Debug)
            {
                reader.Debug = true;
                reader.EnableTraceStream();
            }
#endif
            return reader.ReadObject<DNSEntity>();
        }
        #endregion

        #region IAccessor 成员
        /// <summary>
        /// 从读取器中读取数据到对象。接口实现者可以在这里完全自定义行为（返回true），也可以通过设置事件来影响行为（返回false）
        /// </summary>
        /// <param name="reader">读取器</param>
        /// <returns>是否读取成功，若返回成功读取器将不再读取该对象</returns>
        public virtual bool Read(IReader reader) { return false; }

        /// <summary>
        /// 从读取器中读取数据到对象后执行。接口实现者可以在这里取消Read阶段设置的事件
        /// </summary>
        /// <param name="reader">读取器</param>
        /// <param name="success">是否读取成功</param>
        /// <returns>是否读取成功</returns>
        public virtual bool ReadComplete(IReader reader, bool success) { return success; }

        /// <summary>
        /// 把对象数据写入到写入器。接口实现者可以在这里完全自定义行为（返回true），也可以通过设置事件来影响行为（返回false）
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <returns>是否写入成功，若返回成功写入器将不再读写入对象</returns>
        public virtual bool Write(IWriter writer) { return false; }

        /// <summary>
        /// 把对象数据写入到写入器后执行。接口实现者可以在这里取消Write阶段设置的事件
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="success">是否写入成功</param>
        /// <returns>是否写入成功</returns>
        public virtual bool WriteComplete(IWriter writer, bool success) { return success; }
        #endregion

        #region 辅助
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!Response)
            {
                if (_Questions != null && _Questions.Length > 0)
                    return _Questions[0].ToString();
            }
            else
            {
                var aw = GetAnswer();
                if (aw != null) return aw.ToString();
            }

            return base.ToString();
        }
        #endregion
    }
}