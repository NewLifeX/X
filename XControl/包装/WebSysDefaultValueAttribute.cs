using System;
using System.ComponentModel;

namespace XControl
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class WebSysDefaultValueAttribute : DefaultValueAttribute
    {
        // Fields
        private bool _localized;
        private Type _type;

        // Methods
        internal WebSysDefaultValueAttribute(string value)
            : base(value)
        {
        }

        internal WebSysDefaultValueAttribute(Type type, string value)
            : base(value)
        {
            this._type = type;
        }

        // Properties
        public override object TypeId
        {
            get
            {
                return typeof(DefaultValueAttribute);
            }
        }

        public override object Value
        {
            get
            {
                if (!this._localized)
                {
                    this._localized = true;
                    string str = (string)base.Value;
                    if (!string.IsNullOrEmpty(str))
                    {
                        object obj2 = SR.GetString(str);
                        if (this._type != null)
                        {
                            try
                            {
                                obj2 = TypeDescriptor.GetConverter(this._type).ConvertFromInvariantString((string)obj2);
                            }
                            catch (NotSupportedException)
                            {
                                obj2 = null;
                            }
                        }
                        base.SetValue(obj2);
                    }
                }
                return base.Value;
            }
        }
    }
}
