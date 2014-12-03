using System;
using System.CodeDom.Compiler;
using NewLife;

namespace XTemplate.Templating
{
    /// <summary>模版异常</summary>
    public class TemplateException : XException
    {
        #region 属性
        private Block _Block;
        /// <summary>代码块</summary>
        internal Block Block
        {
            get { return _Block; }
            private set { _Block = value; }
        }

        private CompilerError _Error;
        /// <summary>编译器错误</summary>
        public CompilerError Error
        {
            get
            {
                if (_Error == null && Block != null)
                {
                    _Error = new CompilerError(Block.Name, Block.StartLine, Block.StartColumn, null, Message);
                    _Error.IsWarning = false;
                }
                return _Error;
            }
            internal set { _Error = value; }
        }
        #endregion

        #region 构造
        /// <summary>实例化一个模版处理异常</summary>
        /// <param name="message"></param>
        public TemplateException(String message) : base(message) { }

        /// <summary>实例化一个模版处理异常</summary>
        /// <param name="block"></param>
        /// <param name="message"></param>
        internal TemplateException(Block block, String message)
            : base(message + block.ToFullString())
        {
            Block = block;
        }
        #endregion
    }
}
