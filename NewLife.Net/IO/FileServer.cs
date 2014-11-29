using System;
using System.IO;
using NewLife.Net.Common;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;

namespace NewLife.Net.IO
{
    /// <summary>文件服务端</summary>
    public class FileServer : NetServer
    {
        #region 属性
        private String _SavedPath;
        /// <summary>保存路径</summary>
        public String SavedPath
        {
            get { return _SavedPath ?? (_SavedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")); }
            set { _SavedPath = value; }
        }
        #endregion

        #region 方法
        /// <summary>已重载。</summary>
        protected override void EnsureCreateServer()
        {
            Name = "文件服务";

            //TcpServer svr = new TcpServer(Address, Port);
            //svr.Accepted += new EventHandler<NetEventArgs>(server_Accepted);
            //// 允许同时处理多个数据包
            //svr.NoDelay = false;
            //// 使用线程池来处理事件
            //svr.UseThreadPool = true;
        }
        #endregion

        #region 事件
        void server_Accepted(object sender, NetEventArgs e)
        {
            TcpSession session = e.Socket as TcpSession;
            if (session == null) return;

            //session.NoDelay = false;
            SetEvent(session);
        }

        void SetEvent(TcpSession session)
        {
            session.Received += (sender, e) =>
            {
                TcpSession tc = sender as TcpSession;

                //Stream stream = null;
                //if (!tc.Items.Contains("Stream"))
                //{
                //    stream = new MemoryStream();
                //    tc.Items["Stream"] = stream;
                //}
                //else
                //{
                //    stream = tc.Items["Stream"] as Stream;
                //}

                //// 把数据写入流
                //e.WriteTo(stream);

                var stream = tc.Stream;
                stream.Write(e.Data, 0, e.Length);
                stream.Seek(-1 * e.Length, SeekOrigin.Current);

                // 数据太少时等下一次，不过基本上不可能。5是FileFormat可能的最小长度
                if (stream.Length < 5) return;

                FileFormat format = FileFormat.Load(stream);
            };
            //session.Error += new EventHandler<ExceptionEventArgs>(OnError);
        }
        #endregion
    }
}
