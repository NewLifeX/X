using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Messaging;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 任务执行状态
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

        private ExecutionEngineException _State;
        /// <summary>执行状态</summary>
        public ExecutionEngineException State
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

            private ExecutionEngineException _State;
            /// <summary>执行状态</summary>
            public ExecutionEngineException State
            {
                get { return _State; }
                set { _State = value; }
            }

            private Message _TaskMessage;
            /// <summary>任务信息</summary>
            public Message TaskMessage
            {
                get { return _TaskMessage; }
                set { _TaskMessage = value; }
            }
        }
        #endregion
    }
}
