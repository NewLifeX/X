using System;
using System.ComponentModel;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace XControl
{
    [ToolboxItem(false)]
    [SupportsEventValidation]
    internal class DataControlPagerLinkButton : DataControlLinkButton
    {
        // Methods
        internal DataControlPagerLinkButton(IPostBackContainer container)
            : base(container)
        {
        }

        protected override void SetForeColor()
        {
            if (base.ControlStyle.ForeColor != Color.Empty)
            {
                Control parent = this;
                for (int i = 0; i < 6; i++)
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
                return false;
            }
            set
            {
                throw new NotSupportedException(SR.GetString("CannotSetValidationOnPagerButtons"));
            }
        }
    }
}
