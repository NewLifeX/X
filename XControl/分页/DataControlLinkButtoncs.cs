using System;
using System.ComponentModel;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace XControl
{
    [ToolboxItem(false)]
    [SupportsEventValidation]
    internal class DataControlLinkButton : LinkButton
    {
        // Fields
        private string _callbackArgument;
        private IPostBackContainer _container;
        private bool _enableCallback;

        // Methods
        internal DataControlLinkButton(IPostBackContainer container)
        {
            this._container = container;
        }

        internal void EnableCallback(string argument)
        {
            this._enableCallback = true;
            this._callbackArgument = argument;
        }

        protected override PostBackOptions GetPostBackOptions()
        {
            if (this._container != null)
            {
                return this._container.GetPostBackOptions(this);
            }
            return base.GetPostBackOptions();
        }

        protected override void Render(HtmlTextWriter writer)
        {
            this.SetCallbackProperties();
            this.SetForeColor();
            base.Render(writer);
        }

        private void SetCallbackProperties()
        {
            if (this._enableCallback)
            {
                ICallbackContainer container = this._container as ICallbackContainer;
                if (container != null)
                {
                    string callbackScript = container.GetCallbackScript(this, this._callbackArgument);
                    if (!string.IsNullOrEmpty(callbackScript))
                    {
                        this.OnClientClick = callbackScript;
                    }
                }
            }
        }

        protected virtual void SetForeColor()
        {
            if (base.ControlStyle.ForeColor != Color.Empty)
            {
                Control parent = this;
                for (int i = 0; i < 3; i++)
                {
                    parent = parent.Parent;
                    Color foreColor = ((WebControl)parent).ForeColor;
                    if (foreColor != Color.Empty)
                    {
                        this.ForeColor = foreColor;
                        return;
                    }
                }
            }
        }

        // Properties
        public override bool CausesValidation
        {
            get
            {
                if (this._container != null)
                {
                    return false;
                }
                return base.CausesValidation;
            }
            set
            {
                if (this._container != null)
                {
                    throw new NotSupportedException(SR.GetString("CannotSetValidationOnDataControlButtons"));
                }
                base.CausesValidation = value;
            }
        }
    }
}
