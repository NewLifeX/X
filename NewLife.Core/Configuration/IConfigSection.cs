using System;
using System.Collections.Generic;

namespace NewLife.Configuration
{
    /// <summary>配置对象</summary>
    public interface IConfigSection
    {
        /// <summary>配置名</summary>
        String Key { get; set; }

        /// <summary>配置值</summary>
        String Value { get; set; }

        /// <summary>注释</summary>
        String Comment { get; set; }

        /// <summary>子级</summary>
        IList<IConfigSection> Childs { get; set; }
    }

    /// <summary>配置项</summary>
    public class ConfigSection : IConfigSection
    {
        #region 属性
        /// <summary>配置名</summary>
        public String Key { get; set; }

        /// <summary>配置值</summary>
        public String Value { get; set; }

        /// <summary>注释</summary>
        public String Comment { get; set; }

        /// <summary>子级</summary>
        public IList<IConfigSection> Childs { get; set; }
        #endregion

        #region 方法
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (Childs != null && Childs.Count > 0)
                return $"{Key}[{Childs.Count}]";
            else
                return $"{Key}={Value}";
        }
        #endregion
    }
}