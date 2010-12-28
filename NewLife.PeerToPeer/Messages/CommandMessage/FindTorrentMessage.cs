using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 找种子
    /// </summary>
    public class FindTorrentMessage : CommandMessageBase<FindTorrentMessage>
    {
        #region 属性
        /// <summary>消息类型</summary>
        public override MessageTypes MessageType { get { return MessageTypes.FindTorrent; } }

        private String _Name;
        /// <summary>
        /// 种子名称
        /// </summary>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private String _Guid;
        /// <summary>种子标识</summary>
        public String Guid
        {
            get { return _Guid; }
            set { _Guid = value; }
        }

        #endregion

        #region 响应
        /// <summary>
        /// 响应
        /// </summary>
        public class Response : CommandMessageBase<Response>
        {
            /// <summary>消息类型</summary>
            public override MessageTypes MessageType { get { return MessageTypes.FindTorrentResponse; } }

            private String _Name;
            /// <summary>
            /// 种子名称
            /// </summary>
            public String Name
            {
                get { return _Name; }
                set { _Name = value; }
            }

            private String _Guid;
            /// <summary>
            /// 种子标识
            /// </summary>
            public String Guid
            {
                get { return _Guid; }
                set { _Guid = value; }
            }

            private String _DownLoadPath;
            /// <summary>下载地址</summary>
            public String DownLoadPath
            {
                get { return _DownLoadPath; }
                set { _DownLoadPath = value; }
            }

            private byte[] _Torrent;
            /// <summary>种子</summary>
            public byte[] Torrent
            {
                get { return _Torrent; }
                set { _Torrent = value; }
            }

            private Boolean _IsHave;
            /// <summary>服务器是否有该种子</summary>
            public Boolean IsHave
            {
                get { return _IsHave; }
                set { _IsHave = value; }
            }
        }
        #endregion
    }
}
