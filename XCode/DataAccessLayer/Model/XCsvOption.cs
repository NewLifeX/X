using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using NewLife;


namespace XCode.DataAccessLayer
{
    [Serializable]
    [DisplayName("Csv选项")]
    [Description("Csv选项")]
    [XmlRoot("CsvOption")]
    class XCsvOption : SerializableDataMember, ICsvOption, ICloneable
    {
        /// <summary>
        /// 要标记的列名
        /// </summary>
        [XmlAttribute]
        [DisplayName("要标记的列名")]
        [Description("要标记的列名")]
        public String Column { get; set; }
        /// <summary>指示添加csv标记的位置</summary>
        [XmlAttribute]
        [DisplayName("位置选项")]
        [Description("位置选项")]
        public AppendPositionEnum MarkPosition { get; set; }

       /* [XmlAttribute("MarkPosition")]
        [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
        public string MarkPositionStr
        {
            get { return MarkPosition.ToString(); }
            set { MarkPosition = (AppendPositionEnum)Enum.Parse(typeof(AppendPositionEnum),value); }
        }*/
        /// <summary>左侧标记符</summary>
        [XmlAttribute]
        [DisplayName("左侧标记符")]
        [Description("左侧标记符")]
        public String AppendLeftTag { get; set; }
        /// <summary>左侧标记符</summary>
        [XmlAttribute]
        [DisplayName("右侧标记符")]
        [Description("右侧标记符")]
        public String AppendRightTag { get; set; }
        /// <summary>所属列</summary>
        [XmlIgnore, IgnoreDataMember]
        public IDataColumn DataColumn { get; set; }
        

        public Object Clone() => Clone(DataColumn);
        /// <summary>克隆</summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public ICsvOption Clone(IDataColumn column)
        {
            var csvOption = base.MemberwiseClone() as XCsvOption;
            csvOption.DataColumn = column;

            return csvOption;
        }
    }
}
