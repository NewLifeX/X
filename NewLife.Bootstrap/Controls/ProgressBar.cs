// ProgressBar.cs

// Copyright (C) 2013 Pedro Fernandes

// This program is free software; you can redistribute it and/or modify it under the terms of the GNU 
// General Public License as published by the Free Software Foundation; either version 2 of the 
// License, or (at your option) any later version.

// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without 
// even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See 
// the GNU General Public License for more details. You should have received a copy of the GNU 
// General Public License along with this program; if not, write to the Free Software Foundation, Inc., 59 
// Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NewLife.Bootstrap.Controls
{
    public enum ProgressBarStyles
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Danger = 3
    }

    [ToolboxData("<{0}:ProgressBar runat=server />")]
    public class ProgressBar : WebControl, INamingContainer
    {

        #region CssClass method

        string sCssClass = "";

        /// <summary>
        /// Adds the CSS class.
        /// </summary>
        /// <param name="cssClass">The CSS class.</param>
        private void AddCssClass(string cssClass)
        {
            if (String.IsNullOrEmpty(this.sCssClass))
            {
                this.sCssClass = cssClass;
            }
            else
            {
                this.sCssClass += " " + cssClass;
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBar" /> class.
        /// </summary>
        public ProgressBar()
        {
            this.Animated = false;
            this.Striped = false;
            this.Value = Unit.Percentage(50);
            this.ProgressBarStyle = ProgressBarStyles.Info;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        [Category("Behavior")]
        public Unit Value
        {
            get { return (Unit)ViewState["Value"]; }
            set { ViewState["Value"] = value; }
        }

        /// <summary>
        /// Gets or sets the progress bar style.
        /// </summary>
        /// <value>
        /// The progress bar style.
        /// </value>
        [Category("Appearance")]
        [DefaultValue(ProgressBarStyles.Info)]
        public ProgressBarStyles ProgressBarStyle
        {
            get { return (ProgressBarStyles)ViewState["ProgressBarStyle"]; }
            set { ViewState["ProgressBarStyle"] = value; }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool Animated
        {
            get { return (bool)ViewState["Animated"]; }
            set { ViewState["Animated"] = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ProgressBar" /> is striped.
        /// </summary>
        /// <value>
        ///   <c>true</c> if striped; otherwise, <c>false</c>.
        /// </value>
        [Category("Appearance")]
        [DefaultValue(false)]
        public bool Striped
        {
            get { return (bool)ViewState["Striped"]; }
            set { ViewState["Striped"] = value; }
        }

        /// <summary>
        /// Renders the HTML opening tag of the control to the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
        }

        /// <summary>
        /// Renders the control to the specified HTML writer.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the control content.</param>
        protected override void Render(HtmlTextWriter writer)
        {
            this.AddCssClass(this.CssClass);
            this.AddCssClass("progress");

            if (this.Striped == true) this.AddCssClass("progress-striped");
            if (this.Animated == true) this.AddCssClass("active");
            
            switch (this.ProgressBarStyle)
            {
                case ProgressBarStyles.Success:
                    this.AddCssClass("progress-success");
                    break;

                case ProgressBarStyles.Warning:
                    this.AddCssClass("progress-warning");
                    break;

                case ProgressBarStyles.Danger:
                    this.AddCssClass("progress-danger");
                    break;

                default:
                    this.AddCssClass("progress-info");
                    break;
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
            if (!String.IsNullOrEmpty(this.sCssClass)) writer.AddAttribute(HtmlTextWriterAttribute.Class, this.sCssClass);

            base.Render(writer);
        }

        /// <summary>
        /// Renders the contents of the control to the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        protected override void RenderContents(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "bar");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, this.Value.ToString());

            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderEndTag();
        }

        /// <summary>
        /// Renders the HTML closing tag of the control into the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
            writer.RenderEndTag();
        }
    }
}
