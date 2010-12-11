using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 找种子
    /// </summary>
    public class FindTorrentMessage : Message<FindTorrentMessage>
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

        #endregion

        #region 响应
        /// <summary>
        /// 响应
        /// </summary>
        public class Response : Message<Response>
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

            private String _Address;
            /// <summary>
            /// 下载地址（Tracker服务器地址）
            /// </summary>
            public String Address
            {
                get { return _Address; }
                set { _Address = value; }
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

            private Int32 _Length;
            /// <summary>
            /// 大小
            /// </summary>
            public Int32 Length
            {
                get { return _Length; }
                set { _Length = value; }
            }

            private Int32 _FileNumber;
            /// <summary>
            /// 文件数
            /// </summary>
            public Int32 FileNumber
            {
                get { return _FileNumber; }
                set { _FileNumber = value; }
            }

            private Int32 _BlockNumber;
            /// <summary>
            /// 分块数
            /// </summary>
            public Int32 BlockNumber
            {
                get { return _BlockNumber; }
                set { _BlockNumber = value; }
            }

            private Int32 _BlockSize;
            /// <summary>
            /// 分块大小
            /// </summary>
            public Int32 BlockSize
            {
                get { return _BlockSize; }
                set { _BlockSize = value; }
            }
        }
        #endregion
    }
}
