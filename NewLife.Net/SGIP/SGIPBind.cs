using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Net.SGIP
{
    /// <summary>Bind操作由Bind命令和Bind_Resp应答组成。客户端首先发送Bind命令，服务器端收到Bind命令后，对命令发送方进行验证，然后返回Bind_Resp应答。</summary>
    public class SGIPBind : SGIPEntity
    {
        #region 属性
        private LoginTypes _LoginType;
        /// <summary>登录类型</summary>
        public LoginTypes LoginType { get { return _LoginType; } set { _LoginType = value; } }

        [FieldSize(16)]
        private String _LoginName;
        /// <summary>登录名</summary>
        public String LoginName { get { return _LoginName; } set { _LoginName = value; } }

        [FieldSize(16)]
        private String _LoginPassowrd;
        /// <summary>登录密码</summary>
        public String LoginPassowrd { get { return _LoginPassowrd; } set { _LoginPassowrd = value; } }

        [FieldSize(8)]
        private String _Reserve;
        /// <summary>保留</summary>
        public String Reserve { get { return _Reserve; } set { _Reserve = value; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public SGIPBind() : base(SGIPCommands.Bind) { }
        #endregion
    }

    /// <summary>Bind操作，登录类型。</summary>>
    public enum LoginTypes : byte
    {
        /// <summary>1：SP向SMG建立的连接，用于发送命令</summary>>
        SpToSmg = 1,
        /// <summary>2：SMG向SP建立的连接，用于发送命令</summary>>
        SmgToSp = 2,
        /// <summary>3：SMG之间建立的连接，用于转发命令</summary>>
        SmgToSmg = 3,
        /// <summary>4：SMG向GNS建立的连接，用于路由表的检索和维护</summary>>
        SmgToGns = 4,
        /// <summary>5：GNS向SMG建立的连接，用于路由表的更新</summary>>
        GnsToSmg = 5,
        /// <summary>6：主备GNS之间建立的连接，用于主备路由表的一致性</summary>>
        GnsToGns = 6,
        /// <summary>11：SP与SMG以及SMG之间建立的测试连接，用于跟踪测试</summary>>
        Test = 11,
        /// <summary>其它：保留</summary>>
        Unknown = 0,
    }
}
