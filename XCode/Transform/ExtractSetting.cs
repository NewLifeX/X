using System;
using System.Xml.Serialization;

namespace XCode.Transform
{
    /// <summary>数据抽取参数</summary>
    public interface IExtractSetting
    {
        /// <summary>开始。大于等于</summary>
        DateTime Start { get; set; }

        /// <summary>结束。小于</summary>
        DateTime End { get; set; }

        /// <summary>时间偏移。距离实时时间的秒数，部分业务不能跑到实时</summary>
        Int32 Offset { get; set; }

        /// <summary>开始行。分页</summary>
        Int32 Row { get; set; }

        /// <summary>步进。最大区间大小，秒</summary>
        Int32 Step { get; set; }

        /// <summary>批大小</summary>
        Int32 BatchSize { get; set; }

        ///// <summary>启用</summary>
        //Boolean Enable { get; set; }
    }

    /// <summary>数据抽取参数</summary>
    public class ExtractSetting : IExtractSetting
    {
        #region 属性
        /// <summary>开始。大于等于</summary>
        [XmlIgnore]
        public DateTime Start { get; set; }

        /// <summary>结束。小于</summary>
        [XmlIgnore]
        public DateTime End { get; set; }

        /// <summary>时间偏移。距离实时时间的秒数，部分业务不能跑到实时</summary>
        [XmlIgnore]
        public Int32 Offset { get; set; }

        /// <summary>开始行。分页</summary>
        [XmlIgnore]
        public Int32 Row { get; set; }

        /// <summary>步进。最大区间大小，秒</summary>
        [XmlIgnore]
        public Int32 Step { get; set; }

        /// <summary>批大小</summary>
        [XmlIgnore]
        public Int32 BatchSize { get; set; } = 5000;

        ///// <summary>启用</summary>
        //public Boolean Enable { get; set; } = true;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ExtractSetting() { }

        /// <summary>实例化</summary>
        /// <param name="set"></param>
        public ExtractSetting(IExtractSetting set)
        {
            this.Copy(set);
        }
        #endregion
    }

    /// <summary>抽取参数帮助类</summary>
    public static class ExtractSettingHelper
    {
        /// <summary>拷贝设置参数</summary>
        /// <param name="src"></param>
        /// <param name="set"></param>
        public static IExtractSetting Copy(this IExtractSetting src, IExtractSetting set)
        {
            if (src == null | set == null) return src;

            src.Start = set.Start;
            src.End = set.End;
            src.Row = set.Row;
            src.Step = set.Step;
            src.BatchSize = set.BatchSize;
            //src.Enable = set.Enable;

            return src;
        }

        /// <summary>克隆一份设置参数</summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static IExtractSetting Clone(this IExtractSetting src)
        {
            var set = new ExtractSetting();
            set.Copy(src);

            return set;
        }
    }
}