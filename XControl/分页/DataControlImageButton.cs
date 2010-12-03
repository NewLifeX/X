using System;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace XControl
{
    [ToolboxItem(false)]
    [SupportsEventValidation]
    internal sealed class DataControlImageButton : ImageButton
    {
        // Fields
        private string _callbackArgument;
        private IPostBackContainer _container;
        private bool _enableCallback;

        // Methods
        internal DataControlImageButton(IPostBackContainer container)
        {
            this._container = container;
        }

        internal void EnableCallback(string argument)
        {
            this._enableCallback = true;
            this._callbackArgument = argument;
        }

        protected sealed override PostBackOptions GetPostBackOptions()
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

        // Properties
        public override bool CausesValidation
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotSupportedException(SR.GetString("CannotSetValidationOnDataControlButtons"));
            }
        }
    }
}
