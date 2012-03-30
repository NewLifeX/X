using System;
using System.Diagnostics;

namespace XTemplate.Templating
{
    /// <summary>代码块</summary>>
    internal sealed class Block
    {
        #region 构造
        /// <summary>实例化一个代码块对象</summary>>
        public Block() { }

        /// <summary>实例化一个代码块对象</summary>>
        /// <param name="type"></param>
        /// <param name="text"></param>
        public Block(BlockType type, String text)
        {
            _Type = type;
            _Text = text;
        }
        #endregion

        #region 行号/列数
        private Int32 _StartColumn;
        /// <summary>开始列数</summary>>
        public Int32 StartColumn
        {
            get { return _StartColumn; }
            set { _StartColumn = value; }
        }

        private Int32 _StartLine;
        /// <summary>开始行数</summary>>
        public Int32 StartLine
        {
            get { return _StartLine; }
            set { _StartLine = value; }
        }

        private Int32 _EndColumn;
        /// <summary>结束列数</summary>>
        public Int32 EndColumn
        {
            get { return _EndColumn; }
            set { _EndColumn = value; }
        }

        private Int32 _EndLine;
        /// <summary>结束行数</summary>>
        public Int32 EndLine
        {
            get { return _EndLine; }
            set { _EndLine = value; }
        }
        #endregion

        #region 基本属性
        private String _Name;
        /// <summary>文件名</summary>>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private String _Text;
        /// <summary>文本</summary>>
        public String Text
        {
            get { return _Text; }
            set { _Text = value; }
        }

        private BlockType _Type;
        /// <summary>块类型</summary>>
        public BlockType Type
        {
            get { return _Type; }
            set { _Type = value; }
        }
        #endregion

        #region 方法
        /// <summary>已重载。</summary>>
        /// <returns></returns>
        public override String ToString()
        {
            return ToFullString();
        }

        /// <summary>转为完成字符串</summary>>
        /// <returns></returns>
        public String ToFullString()
        {
            return String.Format("{0} {1}行 {2}列 ({3}) {4}", Name, StartLine, StartColumn, Type, Text);
        }
        #endregion
    }
}