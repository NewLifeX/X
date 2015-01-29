// NavListItem.cs

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

namespace NewLife.Bootstrap.Controls
{
    [ToolboxData("<{0}:NavListItem runat=server></{0}:NavListItem>")]
    [ToolboxItem(false)]
    public class NavListItem : Control, INamingContainer
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="NavListItem" /> class.
        /// </summary>
        public NavListItem()
        {
            this.Header = false;
            this.Divider = false;
            this.Enabled = true;
            this.Icon = "";
            this.Text = this.ID;
            this.NavigateUrl = "#";
        }

        [NotifyParentProperty(true)]
        [Browsable(true)]
        [DefaultValue("")]
        public string Icon
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="NavListItem" /> is header.
        /// </summary>
        /// <value>
        ///   <c>true</c> if header; otherwise, <c>false</c>.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [DefaultValue(false)]
        public bool Header
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="NavListItem" /> is divider.
        /// </summary>
        /// <value>
        ///   <c>true</c> if divider; otherwise, <c>false</c>.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [DefaultValue(false)]
        public bool Divider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [Localizable(true)]
        [DefaultValue("")]
        public string Text
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the navigate URL.
        /// </summary>
        /// <value>
        /// The navigate URL.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [DefaultValue("#")]
        [UrlProperty]
        public string NavigateUrl
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ListItem" /> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [DefaultValue(true)]
        public bool Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// Sends server control content to a provided <see cref="T:System.Web.UI.HtmlTextWriter" /> object, which writes the content to be rendered on the client.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the server control content.</param>
        protected override void Render(HtmlTextWriter writer)
        {
            if (this.Header == true)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "nav-header");
            }
            else if (this.Divider == true)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "divider");
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Li);

            if (this.Header == true && !String.IsNullOrEmpty(this.Icon))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "icon-" + this.Icon);
                writer.RenderBeginTag(HtmlTextWriterTag.I);
                writer.RenderEndTag();
            }

            if (this.Header == true)
            {
                writer.Write(this.Text);
            }
            else if (this.Header == false && this.Divider == false)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Href, ResolveClientUrl(this.NavigateUrl));
                writer.RenderBeginTag(HtmlTextWriterTag.A);

                if (!String.IsNullOrEmpty(this.Icon))
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "icon-" + this.Icon);
                    writer.RenderBeginTag(HtmlTextWriterTag.I);
                    writer.RenderEndTag();
                }

                writer.Write(this.Text);
                writer.RenderEndTag();
            }

            writer.RenderEndTag();
            base.Render(writer);
        }
    }
}
