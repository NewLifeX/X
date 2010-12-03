using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 测试信息
    /// </summary>
    public class TestMessage : Message<TestMessage>
    {
        /// <summary>消息类型</summary>
        public override MessageTypes MessageType { get { return MessageTypes.Test; } }

        #region 属性
        private String _Str;
        /// <summary>字符串</summary>
        public String Str
        {
            get { return _Str; }
            set { _Str = value; }
        }
        #endregion

        #region 响应
        /// <summary>
        /// 邀请响应
        /// </summary>
        public class Response : Message< Response>
        {
            /// <summary>消息类型</summary>
            public override MessageTypes MessageType { get { return MessageTypes.TestResponse; } }

            private String _Str;
            /// <summary>属性说明</summary>
            public String Str
            {
                get { return _Str; }
                set { _Str = value; }
            }
        }
        #endregion
    }
}
