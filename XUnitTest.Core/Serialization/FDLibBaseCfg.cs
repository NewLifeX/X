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
        public string id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string FDID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string faceLibType { get; set; }

    }

    [XmlRootAttribute("FDLibBaseCfgList", Namespace = "http://www.isapi.org/ver20/XMLSchema")]
    public class FDLibBaseCfgList
    {

        public string xmlns { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [XmlAttribute]
        public string version { get; set; }

        /// <summary>
        /// FDLibBaseCfg
        /// </summary>
        [XmlElement("FDLibBaseCfg")]
        public IList<FDLibBaseCfg> FDLibBaseCfgs { get; set; }


    }

}
