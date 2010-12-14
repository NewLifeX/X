using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Messaging;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 任务执行状态 任务就是任务，区别于其他信息
    /// </summary>
    public class TaskMessage : Message<TaskMessage>
    {
        #region 属性
        /// <summary>消息类型</summary>
        public override MessageTypes MessageType { get { return MessageTypes.Task; } }

        private Int32 _TaskID;
        /// <summary>任务ID</summary>
        public Int32 TaskID
        {
            get { return _TaskID; }
            set { _TaskID = value; }
        }

        private Int32 _State;
        /// <summary>执行状态</summary>
        public Int32 State
        {
            get { return _State; }
            set { _State = value; }
        }

        #endregion

        #region 响应
        /// <summary>
        /// 响应
        /// </summary>
        public class Response : Message<Response>
        {
            /// <summary>消息类型</summary>
            public override MessageTypes MessageType { get { return MessageTypes.TaskResponse; } }

            private Int32 _TaskID;
            /// <summary>任务ID</summary>
            public Int32 TaskID
            {
                get { return _TaskID; }
                set { _TaskID = value; }
            }

            private Int32 _State;
            /// <summary>执行状态</summary>
            public Int32 State
            {
                get { return _State; }
                set { _State = value; }
            }

            private Stream _TaskMessage;
            /// <summary>任务信息</summary>
            public Stream TaskMessage
            {
                get { return _TaskMessage; }
                set { _TaskMessage = value; }
            }
        }
        #endregion
    }
}
