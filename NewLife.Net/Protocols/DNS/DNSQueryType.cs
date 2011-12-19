using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>查询的资源记录类型</summary>
    public enum DNSQueryType : ushort
    {
        A = 0x01, //指定计算机 IP 地址。
        NS = 0x02, //指定用于命名区域的 DNS 名称服务器。
        MD = 0x03, //指定邮件接收站（此类型已经过时了，使用MX代替）
        MF = 0x04, //指定邮件中转站（此类型已经过时了，使用MX代替）
        CNAME = 0x05, //指定用于别名的规范名称。
        SOA = 0x06, //指定用于 DNS 区域的“起始授权机构”。
        MB = 0x07, //指定邮箱域名。
        MG = 0x08, //指定邮件组成员。
        MR = 0x09, //指定邮件重命名域名。
        NULL = 0x0A, //指定空的资源记录
        WKS = 0x0B, //描述已知服务。
        PTR = 0x0C, //如果查询是 IP 地址，则指定计算机名；否则指定指向其它信息的指针。
        HINFO = 0x0D, //指定计算机 CPU 以及操作系统类型。
        MINFO = 0x0E, //指定邮箱或邮件列表信息。
        MX = 0x0F, //指定邮件交换器。
        TXT = 0x10, //指定文本信息。

        /// <summary>IPv6地址</summary>
        IPv6 = 0x1C,

        /// <summary>根</summary>
        Root = 0x1D,

        UINFO = 0x64, //指定用户信息。
        UID = 0x65, //指定用户标识符。
        GID = 0x66, //指定组名的组标识符。
        ANY = 0xFF //指定所有数据类型。
    }

    /// <summary>指定信息的协议组</summary>
    public enum QueryClass : ushort
    {
        IN = 0x01, //指定 Internet 类别。
        CSNET = 0x02, //指定 CSNET 类别。（已过时）
        CHAOS = 0x03, //指定 Chaos 类别。
        HESIOD = 0x04,//指定 MIT Athena Hesiod 类别。
        ANY = 0xFF //指定任何以前列出的通配符。
    };
}