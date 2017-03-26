using System;
using System.Collections.Generic;

namespace XTemplate.Templating
{
    /// <summary>模版项。包含模版名和模版内容等基本信息，还包含运行时相关信息。</summary>
    public class TemplateItem
    {
        #region 属性
        /// <summary>模版名</summary>
        public String Name { get; set; }

        /// <summary>模版内容</summary>
        public String Content { get; set; }

        /// <summary>引用命名空间</summary>
        public List<String> Imports { get; set; } = new List<String>();

        /// <summary>是否已处理过</summary>
        internal Boolean Processed { get; set; }

        private String _ClassName;
        /// <summary>类名</summary>
        public String ClassName
        {
            get
            {
                if (String.IsNullOrEmpty(_ClassName)) _ClassName = Template.GetClassName(Name);
                return _ClassName;
            }
            set { _ClassName = value; }
        }

        /// <summary>模版头指令指定的基类名。如果为空表示没有指令指定基类</summary>
        public String BaseClassName { get; set; }

        /// <summary>模版块集合</summary>
        internal List<Block> Blocks { get; set; }

        /// <summary>源代码</summary>
        public String Source { get; set; }

        /// <summary>是否被包含，被包含的模版不生成类</summary>
        public Boolean Included { get; internal set; }

        /// <summary>模版变量集合</summary>
        public IDictionary<String, Type> Vars { get; set; } = new Dictionary<String, Type>();
        #endregion

        #region 方法
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return Name;
        }
        #endregion
    }
}