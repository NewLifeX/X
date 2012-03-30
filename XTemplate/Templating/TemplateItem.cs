using System;
using System.Collections.Generic;

namespace XTemplate.Templating
{
    /// <summary>模版项。包含模版名和模版内容等基本信息，还包含运行时相关信息。</summary>>
    public class TemplateItem
    {
        #region 属性
        private String _Name;
        /// <summary>模版名</summary>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private String _Content;
        /// <summary>模版内容</summary>
        public String Content
        {
            get { return _Content; }
            set { _Content = value; }
        }

        private List<String> _Imports;
        /// <summary>引用命名空间</summary>
        internal List<String> Imports { get { return _Imports ?? (_Imports = new List<String>()); } }

        private Boolean _Processed;
        /// <summary>是否已处理过</summary>
        internal Boolean Processed
        {
            get { return _Processed; }
            set { _Processed = value; }
        }

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

        private String _BaseClassName;
        /// <summary>模版头指令指定的基类名。如果为空表示没有指令指定基类</summary>
        public String BaseClassName
        {
            get { return _BaseClassName; }
            set { _BaseClassName = value; }
        }

        private List<Block> _Blocks;
        /// <summary>模版块集合</summary>
        internal List<Block> Blocks
        {
            get { return _Blocks; }
            set { _Blocks = value; }
        }

        private String _Source;
        /// <summary>源代码</summary>
        public String Source
        {
            get { return _Source; }
            set { _Source = value; }
        }

        private Boolean _Included;
        /// <summary>是否被包含，被包含的模版不生成类</summary>
        public Boolean Included
        {
            get { return _Included; }
            internal set { _Included = value; }
        }

        private Dictionary<String, Type> _Vars;
        /// <summary>模版变量集合</summary>
        public IDictionary<String, Type> Vars { get { return _Vars ?? (_Vars = new Dictionary<String, Type>()); } }
        #endregion

        #region 方法
        /// <summary>已重载。</summary>>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
        #endregion
    }
}