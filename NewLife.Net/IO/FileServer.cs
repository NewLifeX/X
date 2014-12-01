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
            get { return _SavedPath ?? (_SavedPath = "Data"); }
            set { _SavedPath = value; }
        }
        #endregion

        #region 方法
        /// <summary>实例化一个文件服务</summary>
        public FileServer()
        {
            Port = 33;

            Name = "文件服务";
        }
        #endregion

        #region 事件
        /// <summary>收到连接时</summary>
        /// <param name="session"></param>
        protected override void OnAccept(ISocketSession session)
        {
            base.OnAccept(session);

            session.Received += (sender, e) =>
            {
                var tc = sender as ISocketSession;

                var stream = tc.Stream;
                stream.Write(e.Data, 0, e.Length);
                stream.Seek(-1 * e.Length, SeekOrigin.Current);

                // 数据太少时等下一次，不过基本上不可能。5是FileFormat可能的最小长度
                if (stream.Length < 5) return;

                var format = FileFormat.Load(stream);
            };
        }
        #endregion
    }
}