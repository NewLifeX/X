using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>反向工程设置</summary>
    public class NegativeSetting
    {
        #region 属性
        private Boolean _CheckOnly;
        /// <summary>是否只检查。生成日志，但不执行操作</summary>
        public Boolean CheckOnly { get { return _CheckOnly; } set { _CheckOnly = value; } }

        private Boolean _NoDelete;
        /// <summary>是否不删除。在执行反向操作的时候，所有涉及删除表和删除字段的操作均不进行。</summary>
        public Boolean NoDelete { get { return _NoDelete; } set { _NoDelete = value; } }
        #endregion
    }
}