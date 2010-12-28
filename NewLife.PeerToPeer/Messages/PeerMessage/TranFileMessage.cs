using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 文件传输消息
    /// </summary>
    public class TranFileMessage : PeerMessageBase<TranFileMessage>
    {
        /// <summary>消息类型</summary>
        public override MessageTypes MessageType { get { return MessageTypes.TranFile; } }

        #region 属性
        private Guid _FileGuid;
        /// <summary>
        /// 文件Guid
        /// </summary>
        public Guid FileGuid
        {
            get { return _FileGuid; }
            set { _FileGuid = value; }
        }

        private Int32 _Position;
        /// <summary>
        /// 位置
        /// </summary>
        public Int32 Position
        {
            get { return _Position; }
            set { _Position = value; }
        }

        private Int32 _Size;
        /// <summary>
        /// 大小
        /// </summary>
        public Int32 Size
        {
            get { return _Size; }
            set { _Size = value; }
        }
        #endregion

        #region 响应
        /// <summary>
        /// 文件传输响应消息
        /// </summary>
        public class Response : PeerMessageBase<Response>
        {
            /// <summary>消息类型</summary>
            public override MessageTypes MessageType { get { return MessageTypes.TranFileResponse; } }


        }
        #endregion
    }
}
