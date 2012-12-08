using System;
using System.ComponentModel;

namespace XControl
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class WebCategoryAttribute : CategoryAttribute
    {
        // Methods
        internal WebCategoryAttribute(string category)
            : base(category)
        {
        }

        protected override string GetLocalizedString(string value)
        {
            string localizedString = base.GetLocalizedString(value);
            if (localizedString == null)
            {
                localizedString = SR.GetString("Category_" + value);
            }
            return localizedString;
        }

        // Properties
        public override object TypeId
        {
            get
            {
                return typeof(CategoryAttribute);
            }
        }
    }
}
