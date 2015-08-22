using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Xml;

namespace NewLife.Cube
{
    /// <summary>魔方设置</summary>
    [DisplayName("魔方设置")]
    [XmlConfigFile(@"Config\\Cube.config", 15000)]
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        private Boolean _Debug;
        /// <summary>是否启用调试。默认为不启用</summary>
        [Description("调试")]
        public Boolean Debug { get { return _Debug; } set { _Debug = value; } }

        private Boolean _ShowRunTime;
        /// <summary>显示运行时间</summary>
        [Description("显示运行时间")]
        public Boolean ShowRunTime { get { return _ShowRunTime; } set { _ShowRunTime = value; } }
        #endregion

        #region 方法
        /// <summary>新建时调用</summary>
        protected override void OnNew()
        {
            Debug = Config.GetConfig<Boolean>("NewLife.Cube.Debug", false);
            ShowRunTime = Config.GetConfig<Boolean>("NewLife.Cube.ShowRunTime", XTrace.Debug);
        }
        #endregion
    }
}