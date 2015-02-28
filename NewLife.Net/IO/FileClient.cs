using System;
using System.IO;
using NewLife.Log;

namespace NewLife.Net.IO
{
    /// <summary>文件客户端</summary>
    public class FileClient : DisposeBase
    {
        #region 属性
        private ISocketClient _Client;
        /// <summary>客户端连接</summary>
        public ISocketClient Client { get { return _Client; } set { _Client = value; } }
        #endregion

        #region 构造
        /// <summary>销毁客户端</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (Client != null) Client.Dispose();
        }
        #endregion

        #region 方法
        /// <summary>连接文件服务器</summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public void Connect(String hostname, Int32 port)
        {
            if (Client == null)
            {
                var tcp = new TcpSession();
                tcp.UseProcessAsync = false;
                Client = tcp;
                tcp.Remote.Port = port;
                tcp.Remote.Host = hostname;

                tcp.Open();
            }
        }

        /// <summary>发送文件</summary>
        /// <param name="fileName"></param>
        public void SendFile(String fileName)
        {
            SendFile(fileName, null);
        }

        void SendFile(String fileName, String root)
        {
            var ff = new FileFormat(fileName, root);
            WriteLog("{2} 发送文件{0}，{1:n0}字节", ff.Name, ff.Length, Client.Local);
            Client.Send(ff.GetHeader());
            //Client.Send(ff.Stream);
            using (var fs = fileName.AsFile().OpenRead())
            {
                Client.Send(fs);
            }
        }

        /// <summary>发送目录</summary>
        /// <param name="directoryName"></param>
        public void SendDirectory(String directoryName)
        {
            foreach (String item in Directory.GetFiles(directoryName, "*.*", SearchOption.AllDirectories))
            {
                SendFile(item, directoryName);
            }
        }
        #endregion

        #region 日志
#if DEBUG
        private ILog _Log = XTrace.Log;
#else
        private ILog _Log = Logger.Null;
#endif
        /// <summary>日志对象</summary>
        public ILog Log { get { return _Log; } set { _Log = value; } }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log != null) Log.Info(format, args);
        }
        #endregion
    }
}