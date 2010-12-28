using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace XUrlRewrite.Configuration
{
    /// <summary>
    /// Url映射配置集合
    /// </summary>
    public class UrlCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// 创建新的Url映射配置
        /// </summary>
        public UrlCollection()
        {
        }
        /// <summary>
        /// <see cref="ConfigurationElementCollection.CreateNewElement()"/>
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new UrlElement();
        }

        ///// <summary>
        ///// <see cref="ConfigurationElementCollection.GetElementKey(ConfigurationElement element)"/>
        ///// </summary>
        ///// <param name="element"></param>
        ///// <returns></returns>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((UrlElement)element).Url;
        }
        /// <summary>
        /// 添加指定Url映射配置
        /// </summary>
        /// <param name="element"></param>
        public void Add(ConfigurationElement element)
        {
            base.BaseAdd(element);
        }

        ///// <summary>
        ///// <see cref="ConfigurationElementCollection.BaseAdd(ConfigurationElement, Boolean);"/>
        ///// </summary>
        ///// <param name="element"></param>
        ///// <param name="throwIfExists"></param>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="throwIfExists"></param>
        public void Add(ConfigurationElement element, Boolean throwIfExists)
        {
            base.BaseAdd(element, throwIfExists);
        }
        /// <summary>
        /// 在指定位置添加Url映射配置
        /// </summary>
        /// <param name="index"></param>
        /// <param name="element"></param>
        public void Add(Int32 index, ConfigurationElement element)
        {
            base.BaseAdd(index, element);
        }
        /// <summary>
        /// 获取Url映射配置
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public UrlElement Get(Int32 index)
        {
            return base.BaseGet(index) as UrlElement;
        }
        /// <summary>
        /// 删除指定位置的Url映射配置
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(Int32 index)
        {
            base.BaseRemoveAt(index);
        }
    }
}
