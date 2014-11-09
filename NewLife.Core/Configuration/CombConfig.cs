using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using NewLife.Xml;

namespace NewLife.Configuration
{
    /// <summary>CombGuid 配置</summary>
    [Description("CombGuid 配置")]
    [XmlConfigFile(@"Config\Comb.config")]
    public class CombConfig : XmlConfig<CombConfig>
    {
        #region -- 属性 --

        //private DateTime _BaseDate;

        ///// <summary>基准日期</summary>
        //[DisplayName("基准日期")]
        //[Description("用于生成CombGuid的基准日期")]
        //public DateTime BaseDate { get { return _BaseDate; } set { _BaseDate = value; } }

        private Int32 _LastDays;

        /// <summary>上次系统生成CombGuid时的天数</summary>
        [DisplayName("上次系统生成CombGuid时的天数")]
        public Int32 LastDays { get { return _LastDays; } set { _LastDays = value; } }

        private Int32 _LastTenthMilliseconds;

        /// <summary>上次系统生成CombGuid时的时间，单位：100纳秒</summary>
        [DisplayName("上次系统生成CombGuid时的时间，单位：100纳秒")]
        public Int32 LastTenthMilliseconds { get { return _LastTenthMilliseconds; } set { _LastTenthMilliseconds = value; } }

        #endregion

        #region -- 构造 --

        /// <summary>实例化</summary>
        public CombConfig()
        {
            //BaseDate = new DateTime(1970, 1, 1);
        }

        #endregion
    }
}
