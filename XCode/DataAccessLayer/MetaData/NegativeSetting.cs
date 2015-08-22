using System;
using System.ComponentModel;
using NewLife.Configuration;

namespace XCode.DataAccessLayer
{
    /// <summary>反向工程设置</summary>
    public class NegativeSetting
    {
        #region 属性
        private Boolean _Enable;
        /// <summary>是否启用反向工程，默认不启用。反向工程可以实现通过实体类反向更新数据库结构</summary>
        [Description("是否启用反向工程，默认不启用。反向工程可以实现通过实体类反向更新数据库结构")]
        public Boolean Enable { get { return _Enable; } set { _Enable = value; } }

        private Boolean _CheckOnly;
        /// <summary>是否只检查不操作，默认不启用。启用时，仅把更新SQL写入日志</summary>
        [Description("是否只检查不操作，默认不启用。启用时，仅把更新SQL写入日志")]
        public Boolean CheckOnly { get { return _CheckOnly; } set { _CheckOnly = value; } }

        private Boolean _NoDelete;
        /// <summary>是否启用不删除字段，默认不启用。删除字段的操作过于危险，这里可以通过设为true关闭</summary>
        [Description("是否启用不删除字段，默认不启用。删除字段的操作过于危险，这里可以通过设为true关闭")]
        public Boolean NoDelete { get { return _NoDelete; } set { _NoDelete = value; } }

        private String _Exclude;
        /// <summary>要排除的链接名和表名，多个用逗号分隔，默认空</summary>
        [Description("要排除的链接名和表名，多个用逗号分隔，默认空")]
        public String Exclude { get { return _Exclude; } set { _Exclude = value; } }
        #endregion

        #region 方法
        public void Init()
        {
            Enable = Config.GetMutilConfig<Boolean>(false, "XCode.Negative.Enable", "XCode.Schema.Enable", "DatabaseSchema_Enable");
            CheckOnly = Config.GetConfig<Boolean>("XCode.Negative.CheckOnly");
            NoDelete = Config.GetMutilConfig<Boolean>(false, "XCode.Negative.NoDelete", "XCode.Schema.NoDelete", "DatabaseSchema_NoDelete");
            Exclude = Config.GetMutilConfig<String>(null, "XCode.Negative.Exclude", "XCode.Schema.Exclude", "DatabaseSchema_Exclude");
        }
        #endregion
    }
}