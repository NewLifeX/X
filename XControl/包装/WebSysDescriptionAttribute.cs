using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace XControl
{
    [AttributeUsage(AttributeTargets.All)]
    internal class WebSysDescriptionAttribute : DescriptionAttribute
    {
        // Fields
        private bool replaced;

        // Methods
        internal WebSysDescriptionAttribute(string description)
            : base(description)
        {
        }

        // Properties
        public override string Description
        {
            get
            {
                if (!this.replaced)
                {
                    this.replaced = true;
                    base.DescriptionValue = SR.GetString(base.Description);
                }
                return base.Description;
            }
        }

        public override object TypeId
        {
            get
            {
                return typeof(DescriptionAttribute);
            }
        }
    }
}
