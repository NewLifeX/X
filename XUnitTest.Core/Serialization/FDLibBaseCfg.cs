using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace XUnitTest.Serialization
{
    public class FDLibBaseCfg
    {
        /// <summary>
        /// 
        /// </summary>
        public String id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public String FDID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public String name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public String faceLibType { get; set; }

    }

    [XmlRootAttribute("FDLibBaseCfgList", Namespace = "http://www.isapi.org/ver20/XMLSchema")]
    public class FDLibBaseCfgList
    {

        public String xmlns { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [XmlAttribute]
        public String version { get; set; }

        /// <summary>
        /// FDLibBaseCfg
        /// </summary>
        [XmlElement("FDLibBaseCfg")]
        public IList<FDLibBaseCfg> FDLibBaseCfgs { get; set; }


    }

}
