using System;

namespace XCode.Shards
{
    /// <summary>分表分库模型</summary>
    public class ShardModel
    {
        /// <summary>连接名</summary>
        public String ConnName { get; set; }

        /// <summary>表名</summary>
        public String TableName { get; set; }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{ConnName} {TableName}";
    }
}