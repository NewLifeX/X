using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace NewLife.Xml
{
    /// <summary>Xml实体基类</summary>
    /// <remarks>主要提供数据实体和XML文件之间的映射功能</remarks>
    public abstract class XmlEntity<TEntity> where TEntity : XmlEntity<TEntity>, new()
    {
        /// <summary>从一段XML文本中加载对象</summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static TEntity Load(String xml) { return xml.ToXmlEntity<TEntity>(); }

        /// <summary>从一个XML文件中加载对象</summary>
        /// <param name="filename">若为空，则默认为类名加xml后缀</param>
        /// <returns></returns>
        public static TEntity LoadFile(String filename)
        {
            if (String.IsNullOrEmpty(filename)) filename = typeof(TEntity).Name + ".xml";
            return filename.ToXmlFileEntity<TEntity>() ?? new TEntity();
        }

        /// <summary>输出XML</summary>
        /// <returns></returns>
        public virtual String ToXml()
        {
            // 去掉默认命名空间xmlns:xsd和xmlns:xsi
            //return ToXml("", "");
            return this.ToXml("", "");
        }

        /// <summary>输出内部XML</summary>
        /// <returns></returns>
        public virtual String ToInnerXml() { return this.ToXml("", "", true); }

        /// <summary>保存到文件中</summary>
        /// <param name="filename">若为空，则默认为类名加xml后缀</param>
        public virtual void Save(String filename)
        {
            if (String.IsNullOrEmpty(filename)) filename = typeof(TEntity).Name + ".xml";

            this.ToXmlFile(filename);
        }
    }
}