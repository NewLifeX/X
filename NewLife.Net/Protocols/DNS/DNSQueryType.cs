using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>查询的资源记录类型</summary>
    public enum DNSQueryType : ushort
    {
        /// <summary>指定计算机 IP 地址。</summary>
        A = 0x01,

        /// <summary>指定用于命名区域的 DNS 名称服务器。</summary>
        NS = 0x02,

        /// <summary>指定邮件接收站（此类型已经过时了，使用MX代替）</summary>
        MD = 0x03,

        /// <summary>指定邮件中转站（此类型已经过时了，使用MX代替）</summary>
        MF = 0x04,

        /// <summary>指定用于别名的规范名称。</summary>
        CNAME = 0x05,

        /// <summary>指定用于 DNS 区域的“起始授权机构”。</summary>
        SOA = 0x06,

        /// <summary>指定邮箱域名。</summary>
        MB = 0x07,

        /// <summary>指定邮件组成员。</summary>
        MG = 0x08,

        /// <summary>指定邮件重命名域名。</summary>
        MR = 0x09,

        /// <summary>指定空的资源记录</summary>
        NULL = 0x0A,

        /// <summary>描述已知服务。</summary>
        WKS = 0x0B,

        /// <summary>如果查询是 IP 地址，则指定计算机名；否则指定指向其它信息的指针。</summary>
        PTR = 0x0C,

        /// <summary>指定计算机 CPU 以及操作系统类型。</summary>
        HINFO = 0x0D,

        /// <summary>指定邮箱或邮件列表信息。</summary>
        MINFO = 0x0E,

        /// <summary>指定邮件交换器。</summary>
        MX = 0x0F,

        /// <summary>指定文本信息。</summary>
        TXT = 0x10,

        /// <summary>IPv6地址</summary>
        IPv6 = 0x1C,

        /// <summary>根</summary>
        Root = 0x1D,

        /// <summary>指定用户信息。</summary>
        UINFO = 0x64,

        /// <summary>指定用户标识符。</summary>
        UID = 0x65,

        /// <summary>指定组名的组标识符。</summary>
        GID = 0x66,

        /// <summary>指定所有数据类型。</summary>
        ANY = 0xFF
    }

    /// <summary>指定信息的协议组</summary>
    public enum DNSQueryClass : ushort
    {
        /// <summary>指定 Internet 类别。</summary>
        IN = 0x01,

        /// <summary>指定 CSNET 类别。（已过时）</summary>
        CSNET = 0x02,

        /// <summary>指定 Chaos 类别。</summary>
        CHAOS = 0x03,

        /// <summary>指定 MIT Athena Hesiod 类别。</summary>
        HESIOD = 0x04,

        /// <summary>指定任何以前列出的通配符。</summary>
        ANY = 0xFF
    };
}