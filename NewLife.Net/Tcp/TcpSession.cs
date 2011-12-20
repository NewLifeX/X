using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Tcp
{
    /// <summary>Tcp会话。用于TcpServer接受新连接后创建会话</summary>
    public class TcpSession : TcpClientX
    {
        #region 属性
        private Int32 _ID;
        /// <summary>标识</summary>
        public Int32 ID { get { return _ID; } internal set { _ID = value; } }
        #endregion

        #region 接收
        /// <summary>
        /// 开始异步接收，同时处理传入的事件参数，里面可能有接收到的数据
        /// </summary>
        /// <param name="e"></param>
        internal new void ReceiveAsync(NetEventArgs e)
        {
            if (e.BytesTransferred > 0)
                OnReceive(e);
            else
                base.ReceiveAsync(e);
        }
        #endregion
    }
}