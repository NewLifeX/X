using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace XControl
{
    [AttributeUsage(AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Class)]
    internal sealed class WebSysDisplayNameAttribute : DisplayNameAttribute
    {
        // Fields
        private bool replaced;

        // Methods
        internal WebSysDisplayNameAttribute(string DisplayName)
            : base(DisplayName)
        {
        }

        // Properties
        public override string DisplayName
        {
            get
            {
                if (!this.replaced)
                {
                    this.replaced = true;
                    base.DisplayNameValue = SR.GetString(base.DisplayName);
                }
                return base.DisplayName;
            }
        }

        public override object TypeId
        {
            get
            {
                return typeof(DisplayNameAttribute);
            }
        }
    }
}
