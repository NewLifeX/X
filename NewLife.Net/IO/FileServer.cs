using System;
using System.IO;
using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.IO
{
    /// <summary>文件服务端</summary>
    public class FileServer : NetServer<FileSession>
    {
        #region 属性
        private String _SavedPath;
        /// <summary>保存路径</summary>
        public String SavedPath { get { return _SavedPath ?? (_SavedPath = "Data"); } set { _SavedPath = value; } }
        #endregion

        #region 方法
        /// <summary>实例化一个文件服务</summary>
        public FileServer()
        {
            Port = 33;
            ProtocolType = ProtocolType.Tcp;

            Name = "文件服务";
        }

        /// <summary>附加服务器</summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public override bool AttachServer(ISocketServer server)
        {
            // 接收文件需要顺序
            if (server is TcpServer) (server as TcpServer).UseProcessAsync = false;
            return base.AttachServer(server);
        }
        #endregion

        #region 事件
        ///// <summary>收到连接时</summary>
        ///// <param name="session"></param>
        //protected override void OnNewSession(ISocketSession session)
        //{
        //    base.OnNewSession(session);

        //    session.Received += (sender, e) =>
        //    {
        //        var tc = sender as ISocketSession;

        //        var stream = tc.Stream;
        //        stream.Write(e.Data, 0, e.Length);
        //        stream.Seek(-1 * e.Length, SeekOrigin.Current);

        //        // 数据太少时等下一次，不过基本上不可能。5是FileFormat可能的最小长度
        //        if (stream.Length < 5) return;

        //        var format = FileFormat.Load(stream);
        //    };
        //}
        #endregion
    }

    /// <summary>文件服务会话</summary>
    public class FileSession : NetSession<FileServer>
    {
        #region 属性
        private FileFormat _Inf;
        /// <summary>文件信息</summary>
        public FileFormat Inf { get { return _Inf; } set { _Inf = value; } }

        private Int64 _Length;
        /// <summary>长度</summary>
        public Int64 Length { get { return _Length; } set { _Length = value; } }

        private FileStream _Stream;
        /// <summary>文件流</summary>
        public FileStream Stream { get { return _Stream; } set { _Stream = value; } }

        private DateTime _StartTime;
        /// <summary>开始时间</summary>
        public DateTime StartTime { get { return _StartTime; } set { _StartTime = value; } }
        #endregion

        #region 构造
        //public FileSession()
        //{
        //}

        ///// <summary>开始</summary>
        //public override void Start()
        //{
        //    base.Start();
        //    StartTime = Session.StartTime;
        //}

        /// <summary>销毁会话</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            CloseStream();
        }

        void CloseStream()
        {
            if (Stream != null)
            {
                try
                {
                    Stream.Flush();
                    Stream.SetLength(Stream.Position);
                    Stream.Dispose();
                }
                catch { }
                Stream = null;
            }
        }
        #endregion

        /// <summary>处理收到的数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            //base.OnReceive(e);

            var stream = e.Stream;

            // 第一个数据包解析头部
            if (e.Length > 0 && Inf == null)
            {
                var fi = new FileFormat();
                try
                {
                    fi.Read(stream);

                    if (fi.Checksum != fi.Crc)
                        throw new XException("文件{0}校验和错误{1:X8}!={2:X8}！", fi.Name, fi.Checksum, fi.Crc);
                }
                catch (Exception ex)
                {
                    WriteError("无法解析文件头！{0}", ex.Message);
                    // 如果加载失败，则关闭会话
                    Dispose();
                    return;
                }
                Inf = fi;
                Length = 0;
                if (StartTime == DateTime.MinValue) StartTime = Session.StartTime;

                // 加大网络缓冲区
                Session.Socket.ReceiveBufferSize = 2 * 1024 * 1024;

                var file = Host.SavedPath.CombinePath(Inf.Name).EnsureDirectory();
                Stream = file.AsFile().OpenWrite();
                WriteLog("接收{0}，{1:n0}kb", Inf.Name, Inf.Length / 1024);

                if (stream.Position >= stream.Length) return;
            }

            WriteLog("收到{0:n0}字节", stream.Length - stream.Position);
            if (e.Length > 0 && stream.Position < stream.Length)
            {
                Length += (stream.Length - stream.Position);
                if (Stream != null && Stream.CanWrite) Stream.Write(stream);
                //}
                //else
                //{
                if (Length >= Inf.Length)
                {
                    var ms = (DateTime.Now - StartTime).TotalMilliseconds;
                    var speed = Length / 1024 / ms * 1000;
                    WriteLog("{0}接收完成，{1:n0}ms，{2:n0}kb/s", Inf.Name, (Int32)ms, (Int32)speed);
                    //Dispose();

                    // 清空，方便接收下一个文件
                    Inf = null;
                    CloseStream();
                    StartTime = DateTime.MinValue;
                }
            }
        }
    }
}