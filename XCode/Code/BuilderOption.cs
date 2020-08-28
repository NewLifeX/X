using System;
using System.Collections.Generic;

namespace XCode.Code
{
    /// <summary>生成器选项</summary>
    public class BuilderOption
    {
        #region 属性
        /// <summary>命名空间</summary>
        public String Namespace { get; set; }

        /// <summary>引用命名空间</summary>
        public IList<String> Usings { get; set; } = new List<String>();

        /// <summary>纯净类。去除属性上的Description等特性</summary>
        public Boolean Pure { get; set; }

        /// <summary>纯净接口。不带其它特性</summary>
        public Boolean Interface { get; set; }

        /// <summary>类名模板。其中{name}替换为Table.Name，如{name}Model/I{name}Dto等</summary>
        public String ClassTemplate { get; set; }

        /// <summary>基类。可能包含基类和接口，其中{name}替换为Table.Name</summary>
        public String BaseClass { get; set; }

        /// <summary>是否分部类。默认true</summary>
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
        public BuilderOption Clone()
        {
            var option = MemberwiseClone() as BuilderOption;
            var list = new List<String>();
            list.AddRange(Usings);
            option.Usings = list;

            return option;
        }
        #endregion
    }
}