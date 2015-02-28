using System;
using System.IO;
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
    public class FileSession : NetSession
    {
        #region 属性
        private FileFormat _Inf;
        /// <summary>文件信息</summary>
        public FileFormat Inf { get { return _Inf; } set { _Inf = value; } }

        private FileStream _Stream;
        /// <summary>文件流</summary>
        public FileStream Stream { get { return _Stream; } set { _Stream = value; } }
        #endregion

        /// <summary>处理收到的数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            //base.OnReceive(e);

            var ms = e.Stream;

            // 第一个数据包解析头部
            if (Inf == null)
            {
                Inf = new FileFormat();
                try
                {
                    Inf.Read(ms);
                }
                catch (Exception ex)
                {
                    Log.Error("无法解析文件头！", ex.Message);
                    // 如果加载失败，则关闭会话
                    Dispose();
                    return;
                }

                var file = (Host as FileServer).SavedPath.CombinePath(Inf.Name).EnsureDirectory();
                Stream = file.AsFile().OpenWrite();
                WriteLog("接收文件 {0}，大小 {1:n0}字节，保存到 {2}", Inf.Name, Inf.Length, file);

                if (ms.Position >= ms.Length) return;
            }

            WriteLog("收到{0:n0}字节", ms.Length - ms.Position);
            Stream.Write(ms);
        }

        /// <summary>销毁会话</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (Stream != null)
            {
                Stream.Flush();
                Stream.SetLength(Stream.Position);
                Stream.Dispose();
            }
        }
    }
}