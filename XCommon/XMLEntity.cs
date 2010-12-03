using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace XCommon
{
    /// <summary>
    /// XML实体基类
    /// <remarks>主要提供数据实体和XML文件之间的映射功能</remarks>
    /// </summary>
    public abstract class XMLEntity<TEntity> where TEntity : XMLEntity<TEntity>, new()
    {
        /// <summary>
        /// 从一段XML文本中加载对象
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static TEntity Load(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            xml = xml.Trim();

            XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            using (StringReader reader = new StringReader(xml))
            {
                return (serial.Deserialize(reader) as TEntity);
            }
        }

        /// <summary>
        /// 从一个XML文件中加载对象
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static TEntity LoadFile(String filename)
        {
            if (String.IsNullOrEmpty(filename) || !File.Exists(filename)) return null;

            XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                //return (serial.Deserialize(stream) as TEntity);
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return (serial.Deserialize(reader) as TEntity);
                }
            }
        }

        /// <summary>
        /// 输出XML
        /// </summary>
        /// <returns></returns>
        public virtual String ToXML()
        {
            XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    serial.Serialize((TextWriter)writer, this);
                    byte[] bts = stream.ToArray();
                    String xml = Encoding.UTF8.GetString(bts);

                    if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();

                    return xml;
                }
            }
        }

        /// <summary>
        /// 保存到文件中
        /// </summary>
        /// <param name="filename"></param>
        public virtual void Save(String filename)
        {
            if (String.IsNullOrEmpty(filename)) return;

            XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            using (FileStream stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                //serial.Serialize(stream, this);
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    serial.Serialize((TextWriter)writer, this);
                }
            }
        }
    }
}
