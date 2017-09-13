using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCode.Transform
{
    /// <summary>ETL统计接口</summary>
    public interface IETLStat
    {
        #region 性能指标
        /// <summary>总数</summary>
        Int32 Total { get; set; }

        /// <summary>成功</summary>
        Int32 Success { get; set; }

        /// <summary>改变数</summary>
        Int32 Changes { get; set; }

        /// <summary>次数</summary>
        Int32 Times { get; set; }

        ///// <summary>速度</summary>
        //Int32 Speed { get; set; }

        ///// <summary>抽取速度</summary>
        //Int32 FetchSpeed { get; set; }

        /// <summary>错误</summary>
        Int32 Error { get; set; }

        /// <summary>错误内容</summary>
        String Message { get; set; }
        #endregion
    }

    /// <summary>ETL统计</summary>
    public class ETLStat : IETLStat
    {
        #region 性能指标
        /// <summary>总数</summary>
        public Int32 Total { get; set; }

        /// <summary>成功</summary>
        public Int32 Success { get; set; }

        /// <summary>改变数</summary>
        public Int32 Changes { get; set; }

        /// <summary>次数</summary>
        public Int32 Times { get; set; }

        ///// <summary>速度</summary>
        //public Int32 Speed { get; set; }

        ///// <summary>抽取速度</summary>
        //public Int32 FetchSpeed { get; set; }

        /// <summary>错误</summary>
        public Int32 Error { get; set; }

        /// <summary>错误内容</summary>
        public String Message { get; set; }
        #endregion
    }
}