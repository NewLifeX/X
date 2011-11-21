using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using System.ComponentModel;

namespace XControl
{

    /// <summary>
    /// 菜单模版项
    /// </summary>
    ///[PersistenceMode(PersistenceMode.InnerProperty)]
    public class MenuTemplateItem
    {
        private String _ConditionFieldValue;
        /// <summary>条件值</summary>
        public String ConditionFieldValue
        {
            get { return _ConditionFieldValue; }
            set { _ConditionFieldValue = value; }
        }

        private String _Template;
        /// <summary>属性说明</summary>
        [PersistenceMode(PersistenceMode.InnerProperty)]
        public String Template
        {
            get { return _Template; }
            set { _Template = value; }
        }
        //[PersistenceMode(PersistenceMode.InnerProperty), Browsable(false)]
        //public virtual String HeaderTemplate
        //{
        //    get
        //    {
        //        return null;
        //    }
        //    set
        //    {
        //    }
        //}



    }
}
