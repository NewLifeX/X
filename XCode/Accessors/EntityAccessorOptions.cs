using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.Accessors
{
    /// <summary>实体访问器设置选项</summary>
    public enum EntityAccessorOptions
    {
        /// <summary>是否所有字段</summary>
        AllFields,

        /// <summary>请求</summary>
        Request,

        /// <summary>最大文件大小，默认10M</summary>
        MaxLength,

        Container,

        /// <summary>前缀</summary>
        ItemPrefix,

        /// <summary>数据流</summary>
        Stream,

        /// <summary>编码</summary>
        Encoding
    }
}