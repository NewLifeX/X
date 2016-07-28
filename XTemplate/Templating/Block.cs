using System;

namespace XTemplate.Templating
{
    /// <summary>代码块</summary>
    internal sealed class Block
    {
        #region 构造
        /// <summary>实例化一个代码块对象</summary>
        public Block() { }

        /// <summary>实例化一个代码块对象</summary>
        /// <param name="type">类型</param>
        /// <param name="text"></param>
        public Block(BlockType type, String text)
        {
            Type = type;
            Text = text;
        }
        #endregion

        #region 行号/列数
        /// <summary>开始列数</summary>
        public Int32 StartColumn { get; set; }

        /// <summary>开始行数</summary>
        public Int32 StartLine { get; set; }

        /// <summary>结束列数</summary>
        public Int32 EndColumn { get; set; }

        /// <summary>结束行数</summary>
        public Int32 EndLine { get; set; }
        #endregion

        #region 基本属性
        /// <summary>文件名</summary>
        public String Name { get; set; }

        /// <summary>文本</summary>
        public String Text { get; set; }

        /// <summary>块类型</summary>
        public BlockType Type { get; set; }
        #endregion

        #region 方法
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return ToFullString();
        }

        /// <summary>转为完成字符串</summary>
        /// <returns></returns>
        public String ToFullString()
        {
            return String.Format("{0} {1}行 {2}列 ({3}) {4}", Name, StartLine, StartColumn, Type, Text);
        }
        #endregion
    }
}