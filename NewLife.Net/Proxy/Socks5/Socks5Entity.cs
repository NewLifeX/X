using System;
using System.Net;
using NewLife.Serialization;

namespace NewLife.Net.Proxy
{
    /// <summary>Socks5实体基类</summary>
    public abstract class Socks5Entity
    {
        private Byte _Ver = 5;
        /// <summary>版本。默认5</summary>
        public Byte Ver { get { return _Ver; } set { _Ver = value; } }
    }

    /// <summary>Socks5请求</summary>
    public class Socks5Request : Socks5Entity
    {
        private Byte _Count = 2;
        /// <summary>方法数</summary>
        public Byte Count { get { return _Count; } set { _Count = value; } }

        [FieldSize(255)]
        private Byte[] _Methods = new Byte[] { 0, 2 };
        /// <summary>方法</summary>
        public Byte[] Methods { get { return _Methods; } set { _Methods = value; } }

        #region 扩展
        const Byte NoAuth = 0;
        const Byte Auth = 2;

        /// <summary>是否支持不认证</summary>
        public Boolean SupportNoAuth
        {
            get
            {
                for (int i = 0; i < Count && _Methods != null && i < _Methods.Length; i++)
                {
                    if (_Methods[i] == NoAuth) return true;
                }
                return false;
            }
            set
            {
                for (int i = 0; i < Count && _Methods != null && i < _Methods.Length; i++)
                {
                    if (_Methods[i] == NoAuth) return;
                }
                if (_Methods == null)
                    _Methods = new Byte[1];
                else if (_Methods.Length <= Count)
                {
                    var ss = new Byte[_Methods.Length + 1];
                    _Methods.CopyTo(ss, 0);
                    _Methods = ss;
                }
                _Methods[Count] = NoAuth;
                Count++;
            }
        }

        /// <summary>是否支持用户名密码认证</summary>
        public Boolean SupportAuth
        {
            get
            {
                for (int i = 0; i < Count && _Methods != null && i < _Methods.Length; i++)
                {
                    if (_Methods[i] == Auth) return true;
                }
                return false;
            }
            set
            {
                for (int i = 0; i < Count && _Methods != null && i < _Methods.Length; i++)
                {
                    if (_Methods[i] == Auth) return;
                }
                if (_Methods == null)
                    _Methods = new Byte[1];
                else if (_Methods.Length <= Count)
                {
                    var ss = new Byte[_Methods.Length + 1];
                    _Methods.CopyTo(ss, 0);
                    _Methods = ss;
                }
                _Methods[Count] = Auth;
                Count++;
            }
        }
        #endregion
    }

    /// <summary>Socks5答复</summary>
    public class Socks5Answer : Socks5Entity
    {
        private Byte _MethodOrStatus;

        /// <summary>方法</summary>
        public Byte Method { get { return _MethodOrStatus; } set { _MethodOrStatus = value; } }

        /// <summary>认证状态</summary>
        public Byte Status { get { return _MethodOrStatus; } set { _MethodOrStatus = value; } }
    }

    /// <summary>Socks5实体</summary>
    public class Socks5Entity2 : Socks5Entity, IAccessor
    {
        private Byte _CmdOrRep;

        private Byte _Rsv;
        /// <summary>保留</summary>
        public Byte Rsv { get { return _Rsv; } set { _Rsv = value; } }

        private Socks5AddressType _AddressType = Socks5AddressType.IPv4;
        /// <summary>地址类型</summary>
        public Socks5AddressType AddressType { get { return _AddressType; } set { _AddressType = value; } }

        [NonSerialized]
        private String _Address;
        /// <summary>地址</summary>
        public String Address { get { return _Address; } set { _Address = value; } }

        [NonSerialized]
        private UInt16 _Port;
        /// <summary>端口</summary>
        public UInt16 Port { get { return _Port; } set { _Port = value; } }

        #region 扩展属性
        /// <summary>命令</summary>
        public Socks5Command Command { get { return (Socks5Command)_CmdOrRep; } set { _CmdOrRep = (Byte)value; } }

        /// <summary>响应</summary>
        public Socks5Response Response { get { return (Socks5Response)_CmdOrRep; } set { _CmdOrRep = (Byte)value; } }
        #endregion

        #region IAccessor 成员
        Boolean IAccessor.Read(IReader reader) { return false; }

        Boolean IAccessor.ReadComplete(IReader reader, Boolean success)
        {
            var rd = reader as IReader2;
            switch (AddressType)
            {
                case Socks5AddressType.IPv4:
                    Address = new IPAddress(rd.ReadBytes(4)).ToString();
                    break;
                case Socks5AddressType.DomainName:
                    Address = rd.ReadString();
                    break;
                case Socks5AddressType.IPv6:
                    Address = new IPAddress(rd.ReadBytes(16)).ToString();
                    break;
                default:
                    break;
            }
            Port = rd.ReadUInt16();
            return success;
        }

        Boolean IAccessor.Write(IWriter writer) { return false; }

        Boolean IAccessor.WriteComplete(IWriter writer, Boolean success)
        {
            var wr = writer as IWriter2;
            switch (AddressType)
            {
                case Socks5AddressType.IPv4:
                    wr.Write(IPAddress.Parse(Address).GetAddressBytes());
                    break;
                case Socks5AddressType.DomainName:
                    wr.Write(Address);
                    break;
                case Socks5AddressType.IPv6:
                    wr.Write(IPAddress.Parse(Address).GetAddressBytes());
                    break;
                default:
                    break;
            }
            wr.Write(Port);
            return success;
        }
        #endregion
    }

    /// <summary>Socks5命令</summary>
    public enum Socks5Command : byte
    {
        /// <summary>Connect</summary>
        Connect = 1,

        /// <summary>Bind</summary>
        Bind = 2,

        /// <summary>UdpAssociate</summary>
        UdpAssociate = 3,
    }

    /// <summary>Socks5响应类型</summary>
    public enum Socks5Response : byte
    {
        /// <summary>成功</summary>
        Success = 0,

        /// <summary>普通的SOCKS服务器请求失败</summary>
        GeneralSocksServerFailure,

        /// <summary>现有的规则不允许的连接</summary>
        ConnectionNotAllowed,

        /// <summary>网络不可达</summary>
        NetworkUnreachable,

        /// <summary>主机不可达</summary>
        HostUnreachable,

        /// <summary>连接被拒</summary>
        ConnectionRefused,

        /// <summary>TTL超时</summary>
        TTLExpired,

        /// <summary>不支持的命令</summary>
        CommandNotSupported,

        /// <summary>不支持的地址类型</summary>
        AddressTypeNotSupported,

        /// <summary>未定义错误</summary>
        UnknownError
    }

    /// <summary>Socks5地址类型</summary>
    public enum Socks5AddressType : byte
    {
        /// <summary>IPv4地址</summary>
        IPv4 = 1,

        /// <summary>域名</summary>
        DomainName = 3,

        /// <summary>IPv6地址</summary>
        IPv6 = 4
    }

    /// <summary>Socks5认证消息</summary>
    public class Socks5Auth : Socks5Entity
    {
        private String _UserName;
        /// <summary>用户名</summary>
        public String UserName { get { return _UserName; } set { _UserName = value; } }

        private String _Password;
        /// <summary>密码</summary>
        public String Password { get { return _Password; } set { _Password = value; } }
    }
}