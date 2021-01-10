using System;
using System.Reflection;

namespace XCode
{
    /// <summary>
    /// 要添加的位置描述，取值范围左，右，左右
    /// </summary>
    public enum AppendPositionEnum
    {
        
        /// <summary>
        /// 左
        /// </summary>
        Left = 0,
        /// <summary>
        /// 右
        /// </summary>
        Right = 1,
        /// <summary>
        /// 左右
        /// </summary>
        LeftAndRight = 2
    }
    /// <summary>
    /// 标记为导出Csv字段，该属性只能为在字段和属性上
    /// </summary>
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property,AllowMultiple =false)]
    public class MarkAsCsvColumn : System.Attribute
    {
       
        private string _appendLeftTag = "\t";
        private string _appendRightTag = "\t";
        private AppendPositionEnum _appendPosition = AppendPositionEnum.LeftAndRight;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="appendPosition"></param>
        /// <param name="appendLeftTag"></param>
        /// <param name="apendRightTag"></param>
        public MarkAsCsvColumn(AppendPositionEnum appendPosition,string appendLeftTag="\t",string apendRightTag="\t") { 
        
        }
        public string AppendLeftTag {
            get { return _appendLeftTag; }
            set { _appendLeftTag = value; }
        }
        public string AppendRightTag {
            get { return _appendRightTag; }
            set { _appendRightTag = value; }
        }
        public AppendPositionEnum AppendPosition {
            get { return _appendPosition; }
            set { _appendPosition = value; }
        }
    }
}
