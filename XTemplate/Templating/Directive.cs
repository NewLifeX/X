using System;
using System.Collections.Generic;

namespace XTemplate.Templating
{
    /// <summary>指令</summary>
    internal sealed class Directive
    {
        /// <summary>实例化一个指令对象</summary>
        /// <param name="name">名称</param>
        /// <param name="parameters">参数数组</param>
        /// <param name="block"></param>
        public Directive(String name, IDictionary<String, String> parameters, Block block)
        {
            Name = name;
            Parameters = parameters;
            Block = block;
        }

        /// <summary>块</summary>
        public Block Block { get; private set; }

        /// <summary>指令名</summary>
        public String Name { get; private set; }

        /// <summary>参数集合</summary>
        public IDictionary<String, String> Parameters { get; private set; }

        /// <summary>读取参数值</summary>
        /// <param name="name">参数名</param>
        /// <returns></returns>
        public String GetParameter(String name)
        {
            String value;
            if (TryGetParameter(name, out value)) return value;

            throw new TemplateException(Block, String.Format("{0}指令缺少{1}参数！", Name, name));
        }

        /// <summary>尝试读取参数值</summary>
        /// <param name="name">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public Boolean TryGetParameter(String name, out String value)
        {
            value = null;

            var ps = Parameters;
            if (ps == null || ps.Count < 1) return false;

            if (ps.TryGetValue(name, out value) || ps.TryGetValue(name.ToLower(), out value)) return true;

            foreach (var item in ps)
            {
                if (item.Key.EqualIgnoreCase(name))
                {
                    value = item.Value;
                    return true;
                }
            }

            return false;
        }
    }
}