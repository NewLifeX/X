using System;

namespace NewLife.Security
{
    /// <summary>ASN.1标签</summary>
    [Flags]
    public enum Asn1Tags
    {
        /// <summary>布尔</summary>
        Boolean = 0x01,

        /// <summary>长整数</summary>
        Integer = 0x02,

        /// <summary>比特串</summary>
        BitString = 0x03,

        /// <summary>字节串</summary>
        OctetString = 0x04,

        /// <summary>空</summary>
        Null = 0x05,

        /// <summary>OID实体标识符</summary>
        ObjectIdentifier = 0x06,

        /// <summary>外部</summary>
        External = 0x08,

        /// <summary>枚举</summary>
        Enumerated = 0x0a,

        /// <summary>序列</summary>
        Sequence = 0x10,
        //SequenceOf = 0x10, // for completeness

        /// <summary>集合</summary>
        Set = 0x11,
        //SetOf = 0x11, // for completeness

        /// <summary>数字字符串</summary>
        NumericString = 0x12,

        /// <summary>可打印字符串</summary>
        PrintableString = 0x13,

        /// <summary>T61字符串</summary>
        T61String = 0x14,

        /// <summary>视频</summary>
        VideotexString = 0x15,

        /// <summary>IA5字符串</summary>
        IA5String = 0x16,

        /// <summary>UTC时间</summary>
        UtcTime = 0x17,

        /// <summary>通用时间</summary>
        GeneralizedTime = 0x18,

        /// <summary>图形</summary>
        GraphicString = 0x19,

        /// <summary>可见字符串</summary>
        VisibleString = 0x1a,

        /// <summary>基本字符串</summary>
        GeneralString = 0x1b,

        /// <summary>全局字符串</summary>
        UniversalString = 0x1c,

        /// <summary>位图</summary>
        BmpString = 0x1e,

        /// <summary>UTF8字符串</summary>
        Utf8String = 0x0c,

        /// <summary>组合</summary>
        Constructed = 0x20,

        /// <summary>应用</summary>
        Application = 0x40,

        /// <summary>标记</summary>
        Tagged = 0x80,
    }
}