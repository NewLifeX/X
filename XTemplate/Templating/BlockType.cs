using System;

namespace XTemplate.Templating
{
    /// <summary>代码块类型</summary>
    internal enum BlockType
    {
        /// <summary>指令</summary>
        Directive,

        /// <summary>成员</summary>
        Member,

        /// <summary>模版文本</summary>
        Text,

        /// <summary>语句</summary>
        Statement,

        /// <summary>表达式</summary>
        Expression
    }
}