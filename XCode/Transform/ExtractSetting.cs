using System;

namespace XCode.Transform
{
    /// <summary>数据抽取参数</summary>
    public interface IExtractSetting
    {
        /// <summary>开始。大于等于</summary>
        DateTime Start { get; set; }

        /// <summary>开始行。分页</summary>
        Int32 Row { get; set; }

        /// <summary>批大小</summary>
        Int32 BatchSize { get; set; }

        /// <summary>启用</summary>
        Boolean Enable { get; set; }
    }

    /// <summary>数据抽取参数</summary>
    public class ExtractSetting : IExtractSetting
    {
        /// <summary>开始。大于等于</summary>
        public DateTime Start { get; set; }

        /// <summary>开始行。分页</summary>
        public Int32 Row { get; set; }

        /// <summary>批大小</summary>
        public Int32 BatchSize { get; set; } = 5000;

        /// <summary>启用</summary>
        public Boolean Enable { get; set; } = true;
    }
}