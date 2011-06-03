using System;
using System.Collections.Generic;
using System.Collections;

namespace XTemplate.Templating
{
    /// <summary>
    /// 指令
    /// </summary>
    internal sealed class Directive
    {
        /// <summary>
        /// 实例化一个指令对象
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <param name="block"></param>
        public Directive(String name, IDictionary<String, String> parameters, Block block)
        {
            _Name = name;
            _Parameters = parameters;
            _Block = block;
        }

        private Block _Block;
        /// <summary>
        /// 块
        /// </summary>
        public Block Block { get { return _Block; } }

        private String _Name;
        /// <summary>
        /// 指令名
        /// </summary>
        public String Name { get { return _Name; } }

        private IDictionary<String, String> _Parameters;
        /// <summary>
        /// 参数集合
        /// </summary>
        public IDictionary<String, String> Parameters { get { return _Parameters; } }

        /// <summary>
        /// 尝试读取参数值
        /// </summary>
        /// <param name="name">参数名</param>
        /// <returns></returns>
        public String TryGetParameter(String name)
        {
            String str;
            if (Parameters == null || Parameters.Count < 1)
                throw new TemplateException(Block, String.Format("{0}指令缺少{1}参数！", Name, name));

            if (Parameters.TryGetValue(name, out str) || Parameters.TryGetValue(name.ToLower(), out str)) return str;

            foreach (String item in Parameters.Keys)
            {
                if (String.Equals(item, name, StringComparison.OrdinalIgnoreCase)) return Parameters[item];
            }

            throw new TemplateException(Block, String.Format("{0}指令缺少{1}参数！", Name, name));
        }
    }
}