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
        //private Guid _FileGuid;
        ///// <summary>
        ///// 文件Guid
        ///// </summary>
        //public Guid FileGuid
        //{
        //    get { return _FileGuid; }
        //    set { _FileGuid = value; }
        //}

        //private Int32 _Position;
        ///// <summary>
        ///// 位置
        ///// </summary>
        //public Int32 Position
        //{
        //    get { return _Position; }
        //    set { _Position = value; }
        //}

        //private Int32 _Size;
        ///// <summary>
        ///// 大小
        ///// </summary>
        //public Int32 Size
        //{
        //    get { return _Size; }
        //    set { _Size = value; }
        //}
        private int _BlockIndex;
        /// <summary>
        /// 请求文件传输需要的块索引
        /// </summary>
        public int BlockIndex
        {
            get { return _BlockIndex; }
            set { _BlockIndex = value; }
        }
        private int[] _OwnedBlocks;
        /// <summary>
        /// 节点所拥有的块
        /// </summary>
        public int[] OwnedBlocks
        {
            get { return _OwnedBlocks; }
            set { _OwnedBlocks = value; }
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
            private int _BlockIndex;
            /// <summary>
            /// 响应文件传输需要的块
            /// </summary>
            public int BlockIndex
            {
                get { return _BlockIndex; }
                set { _BlockIndex = value; }
            }

            private int _ForwardTcpPort;
            /// <summary>
            /// 重定向的端口,如果小于等于0则表示例外,不能连接目标端口,参考TranFileMessage.ResponseCode
            /// </summary>
            public int ForwardTcpPort
            {
                get { return _ForwardTcpPort; }
                set { _ForwardTcpPort = value; }
            }
            private int[] _OwnedBlocks;
            /// <summary>
            /// 节点所拥有的块
            /// </summary>
            public int[] OwnedBlocks
            {
                get { return _OwnedBlocks; }
                set { _OwnedBlocks = value; }
            }
        }
        /// <summary>
        /// 传输文件响应代码,其实际值需要是小于等于0,因为大于0用于表示重定向的端口
        /// </summary>
        public enum ResponseCode
        {
            Unkown = 0,
            NotFound = -1,
            ClientLimit = -2
        }
        #endregion
    }
}
