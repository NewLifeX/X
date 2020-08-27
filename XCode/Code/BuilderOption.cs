using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XCode.Code
{
    /// <summary>生成器选项</summary>
    public class BuilderOption
    {
        #region 属性
        /// <summary>命名空间</summary>
        public String Namespace { get; set; }

        /// <summary>引用命名空间</summary>
        public HashSet<String> Usings { get; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        /// <summary>纯净类</summary>
        public Boolean Pure { get; set; }

        /// <summary>生成接口</summary>
        public Boolean Interface { get; set; }

        /// <summary>类名后缀。如Model/Dto等</summary>
        public String ClassPrefix { get; set; }

        /// <summary>基类</summary>
        public String BaseClass { get; set; }

        /// <summary>是否分部类</summary>
        public Boolean Partial { get; set; } = true;

        /// <summary>输出目录</summary>
        public String Output { get; set; }

        /// <summary>连接名</summary>
        public String ConnName { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public BuilderOption()
        {
            Namespace = GetType().Namespace;

            Usings.Add("System");
            Usings.Add("System.Collections.Generic");
            Usings.Add("System.ComponentModel");
            Usings.Add("System.Runtime.Serialization");
            Usings.Add("System.Web.Script.Serialization");
            Usings.Add("System.Xml.Serialization");
        }
        #endregion

        #region 方法
        /// <summary>克隆</summary>
        /// <returns></returns>
        public BuilderOption Clone() => MemberwiseClone() as BuilderOption;
        #endregion
    }
}