using System;
namespace System.Runtime
{
#if !NET4
    /// <summary>指示此特性应用于的 .NET Framework 类库方法不可能受服务版本的影响，因此它可以在本机映像生成器 (NGen) 格式的映像间内联。</summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    sealed class TargetedPatchingOptOutAttribute : Attribute
    {
        private string m_reason;
        /// <summary>获取此特性应用于的方法被认为可以在本机映像生成器 (NGen) 格式的映像间内联的原因。</summary>
        /// <returns>此方法被认为可以在 NGen 格式的映像间内联的原因。</returns>
        public string Reason
        {
            get
            {
                return this.m_reason;
            }
        }
        /// <summary>初始化 <see cref="T:System.Runtime.TargetedPatchingOptOutAttribute" /> 类的新实例。</summary>
        /// <param name="reason">
        ///   <see cref="T:System.Runtime.TargetedPatchingOptOutAttribute" /> 特性应用于的方法被认为可以在本机映像生成器 (NGen) 格式的映像间内联的原因。</param>
        public TargetedPatchingOptOutAttribute(string reason)
        {
            this.m_reason = reason;
        }
        private TargetedPatchingOptOutAttribute()
        {
        }
    }
#endif
}