using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Net.DNS
{
    /// <summary>DNS实体类基类</summary>
    /// <remarks>
    /// 参考博客园 @看那边的人 <a target="_blank" href="http://www.cnblogs.com/topdog/archive/2011/11/15/2250185.html">DIY一个DNS查询器：了解DNS协议</a> <a target="_blank" href="http://www.cnblogs.com/topdog/archive/2011/11/21/2257597.html">DIY一个DNS查询器：程序实现</a>
    /// </remarks>
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
        public virtual String Name { get { return Question.Name; } set { Question.Name = value; } }

        /// <summary>查询类型</summary>
        public virtual DNSQueryType Type { get { return Question.Type; } set { Question.Type = value; } }

        /// <summary>协议组</summary>
        public virtual DNSQueryClass Class { get { return Question.Class; } set { Question.Class = value; } }

        /// <summary>获取响应</summary>
        /// <param name="create"></param>
        /// <returns></returns>
        internal protected DNSRecord GetAnswer(Boolean create = false)
        {
            var type = Question.Type;
            if (Answers == null || Answers.Length < 1)
            {
                if (!create) return null;

                Answers = new DNSRecord[] { CreateRecord(type) };
            }

            foreach (var item in Answers)
            {
                if (item.Type == type) return item;
            }

            return Answers[0];
        }
        #endregion

        #region 读写
        /// <summary>把当前对象写入到数据流中去</summary>
        /// <param name="stream"></param>
        /// <param name="forTcp">是否是Tcp，Tcp需要增加整个流长度</param>
        public void Write(Stream stream, Boolean forTcp = false)
        {
            if (forTcp)
            {
                var ms = new MemoryStream();
                WriteRaw(ms);

                Byte[] data = BitConverter.GetBytes((Int16)ms.Length);
                Array.Reverse(data);
                stream.Write(data, 0, data.Length);
                ms.WriteTo(stream);
            }
            else
                WriteRaw(stream);
        }

        /// <summary>把当前对象写入到数据流中去</summary>
        /// <param name="stream"></param>
        public void WriteRaw(Stream stream)
        {
            BinaryWriterX writer = new BinaryWriterX();
            writer.Settings.IsLittleEndian = false;
            writer.Settings.UseObjRef = false;
            writer.Settings.Encoding = Encoding.Default;
            writer.Stream = stream;
#if DEBUG
            //if (NetHelper.Debug)
            //{
            //    writer.Debug = true;
            //    writer.EnableTraceStream();
            //}
#endif
            writer.WriteObject(this);
        }

        /// <summary>获取当前对象的数据流</summary>
        /// <param name="forTcp">是否是Tcp，Tcp需要增加整个流长度</param>
        /// <returns></returns>
        public Stream GetStream(Boolean forTcp = false)
        {
            var ms = new MemoryStream();
            Write(ms, forTcp);
            ms.Position = 0;

            return ms;
        }

        /// <summary>从数据中读取对象</summary>
        /// <param name="data"></param>
        /// <param name="forTcp">是否是Tcp，Tcp需要增加整个流长度</param>
        /// <returns></returns>
        public static DNSEntity Read(Byte[] data, Boolean forTcp = false)
        {
            if (data == null || data.Length < 1) return null;

            return Read(new MemoryStream(data), forTcp);
        }

        /// <summary>从数据流中读取对象，返回<see cref="DNS_A"/>、<see cref="DNS_PTR"/>等真实对象</summary>
        /// <param name="stream"></param>
        /// <param name="forTcp">是否是Tcp，Tcp需要增加整个流长度</param>
        /// <returns></returns>
        public static DNSEntity Read(Stream stream, Boolean forTcp = false)
        {
            // 跳过2个字节的长度
            if (forTcp)
            {
                //stream.Seek(2, SeekOrigin.Current);
                // 必须全部先读出来，否则内部的字符串映射位移不正确
                Byte[] data = new Byte[2];
                stream.Read(data, 0, data.Length);
                // 网络序变为主机序
                Array.Reverse(data);
                var len = BitConverter.ToInt16(data, 0);
                data = new Byte[len];
                stream.Read(data, 0, data.Length);

                stream = new MemoryStream(data);
            }

            // 先读取
            return ReadRaw(stream);
        }

        /// <summary>从数据流中读取对象，返回DNSEntity对象</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static DNSEntity ReadRaw(Stream stream)
        {
            BinaryReaderX reader = new BinaryReaderX();
            reader.Settings.IsLittleEndian = false;
            reader.Settings.UseObjRef = false;
            reader.Settings.Encoding = Encoding.Default;
            reader.Stream = stream;
#if DEBUG
            //if (NetHelper.Debug)
            //{
            //    reader.Debug = true;
            //    reader.EnableTraceStream();
            //}
#endif
            return reader.ReadObject<DNSEntity>();
        }
        #endregion

        #region 注册子类型
        static Dictionary<DNSQueryType, Type> entitytypes = new Dictionary<DNSQueryType, Type>();
        static DNSEntity()
        {
            foreach (var item in AssemblyX.FindAllPlugins(typeof(DNSRecord)))
            {
                if (item == typeof(DNSRecord)) continue;

                var dr = TypeX.CreateInstance(item) as DNSRecord;
                if (dr != null) entitytypes.Add(dr.Type, item);
            }
        }

        /// <summary>创建指定类型的记录</summary>
        /// <param name="qt"></param>
        /// <returns></returns>
        public static DNSRecord CreateRecord(DNSQueryType qt)
        {
            Type type = null;
            if (!entitytypes.TryGetValue(qt, out type) || type == null) return null;

            return TypeX.CreateInstance(type) as DNSRecord;
        }
        #endregion

        #region IAccessor 成员
        /// <summary>从读取器中读取数据到对象。接口实现者可以在这里完全自定义行为（返回true），也可以通过设置事件来影响行为（返回false）</summary>
        /// <param name="reader">读取器</param>
        /// <returns>是否读取成功，若返回成功读取器将不再读取该对象</returns>
        public virtual bool Read(IReader reader)
        {
            reader.OnMemberReading += new EventHandler<ReadMemberEventArgs>(reader_OnMemberReading);
            reader.OnItemReading += new EventHandler<ReadItemEventArgs>(reader_OnItemReading);
            return false;
        }

        void reader_OnItemReading(object sender, ReadItemEventArgs e)
        {
            if (e.Type == typeof(DNSRecord))
            {
                var reader = sender as IReader;
                var p = reader.Stream.Position;
                String name = GetNameAccessor(reader).Read(reader.Stream, 0);
                DNSQueryType qt = (DNSQueryType)reader.ReadValue(typeof(DNSQueryType));
                // 退回去，让序列化自己读
                reader.Stream.Position = p;
                //Type type = null;
                //if (entitytypes.TryGetValue(qt, out type) && type != null) e.Type = type;
                //var value = TypeX.CreateInstance(e.Type) as DNSRecord;
                var value = CreateRecord(qt);
                if (value != null)
                    e.Type = value.GetType();
                else
                    value = new DNSRecord();
                value.Name = name;
                //value.Type = qt;
                e.Value = value;
            }
        }

        /// <summary>从读取器中读取数据到对象后执行。接口实现者可以在这里取消Read阶段设置的事件</summary>
        /// <param name="reader">读取器</param>
        /// <param name="success">是否读取成功</param>
        /// <returns>是否读取成功</returns>
        public virtual bool ReadComplete(IReader reader, bool success) { return success; }

        /// <summary>把对象数据写入到写入器。接口实现者可以在这里完全自定义行为（返回true），也可以通过设置事件来影响行为（返回false）</summary>
        /// <param name="writer">写入器</param>
        /// <returns>是否写入成功，若返回成功写入器将不再读写入对象</returns>
        public virtual bool Write(IWriter writer)
        {
            writer.OnMemberWriting += new EventHandler<WriteMemberEventArgs>(writer_OnMemberWriting);
            return false;
        }

        /// <summary>把对象数据写入到写入器后执行。接口实现者可以在这里取消Write阶段设置的事件</summary>
        /// <param name="writer">写入器</param>
        /// <param name="success">是否写入成功</param>
        /// <returns>是否写入成功</returns>
        public virtual bool WriteComplete(IWriter writer, bool success) { return success; }
        #endregion

        #region 特殊处理字符串
        void reader_OnMemberReading(object sender, ReadMemberEventArgs e)
        {
            var reader = sender as IReader;
            // TXT记录的Text字段不采用DNS字符串
            if (e.Type == typeof(String) && e.Member.Name != "_Text")
            {
                Object ps = reader.Items["Position"];
                Int64 p = ps is Int64 ? (Int64)ps : 0;
                e.Member[e.Value] = GetNameAccessor(reader).Read(reader.Stream, p);
                //reader.WriteLog("ReadMember", "_Name", "String", e.Member[e.Value]);
                e.Success = true;
            }
            else if (e.Type == typeof(TimeSpan))
            {
                e.Member[e.Value] = new TimeSpan(0, 0, reader.ReadInt32());
                e.Success = true;
            }
        }

        void writer_OnMemberWriting(object sender, WriteMemberEventArgs e)
        {
            var writer = sender as IWriter;
            // TXT记录的Text字段不采用DNS字符串
            if (e.Type == typeof(String) && e.Member.Name != "_Text")
            {
                //writer.WriteLog("WriteMember", "_Name", "String", e.Member[e.Value]);
                Object ps = writer.Items["Position"];
                Int64 p = ps is Int64 ? (Int64)ps : 0;
                p += writer.Stream.Position;
                GetNameAccessor(writer).Write(writer.Stream, (String)e.Member[e.Value], p);
                e.Success = true;
            }
            else if (e.Type == typeof(TimeSpan))
            {
                var ts = (TimeSpan)e.Member[e.Value];
                writer.Write((Int32)ts.TotalSeconds);
                e.Success = true;
            }
        }

        [DebuggerHidden]
        internal static DNSNameAccessor GetNameAccessor(IReaderWriter rw)
        {
            var accessor = rw.Items["Names"] as DNSNameAccessor;
            if (accessor == null) rw.Items.Add("Names", accessor = new DNSNameAccessor());

            return accessor;
        }
        #endregion

        #region 辅助
        /// <summary>复制</summary>
        /// <param name="entity"></param>
        public virtual DNSEntity CloneFrom(DNSEntity entity)
        {
            var de = this;
            de.Header = entity.Header;
            de.Questions = entity.Questions;
            de.Answers = entity.Answers;
            de.Authoritis = entity.Authoritis;
            de.Additionals = entity.Additionals;
            return de;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        [DebuggerHidden]
        public override string ToString()
        {
            if (!Response)
            {
                if (_Questions != null && _Questions.Length > 0)
                    return _Questions[0].ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                if (_Questions != null && _Questions.Length > 0)
                    sb.AppendFormat("[{0}]", _Questions[0]);

                if (Answers != null && Answers.Length > 0)
                {
                    foreach (var item in Answers)
                    {
                        if (sb.Length > 0) sb.Append(" ");
                        sb.Append(item);
                    }
                }
                else if (Authoritis != null && Authoritis.Length > 0)
                {
                    foreach (var item in Authoritis)
                    {
                        if (sb.Length > 0) sb.Append(" ");
                        sb.Append(item);
                    }
                }
                else if (Header.ResponseCode == DNSRcodeType.NameError)
                    sb.Append("No such name");

                return String.Format("Response {0}", sb);
            }

            return base.ToString();
        }
        #endregion
    }
}