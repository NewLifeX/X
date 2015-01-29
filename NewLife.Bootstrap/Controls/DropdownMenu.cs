// DropdownMenu.cs

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
    [ToolboxData("<{0}:DropdownMenu runat=server></{0}:DropdownMenu>")]
    public class DropdownMenu : BulletedList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DropdownMenu" /> class.
        /// </summary>
        public DropdownMenu()
        {
            this.RightToLeft = false;
        }

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
        /// Gets or sets the type of the alert.
        /// </summary>
        /// <value>
        /// The type of the alert.
        /// </value>
        [Category("Appearance")]
        [DefaultValue(false)]
        public bool RightToLeft
        {
            get { return (bool)ViewState["RightToLeft"]; }
            set { ViewState["RightToLeft"] = value; }
        }

        /// <summary>
        /// Writes the <see cref="T:System.Web.UI.WebControls.BulletedList" /> control content to the specified <see cref="T:System.Web.UI.HtmlTextWriter" /> object for display on the client.
        /// </summary>
        /// <param name="writer">An <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        protected override void Render(HtmlTextWriter writer)
        {
            this.AddCssClass(this.CssClass);

            if (this.Parent.Parent != null && this.Parent.Parent.GetType() == typeof(NavBar))
            {
                this.AddCssClass("nav");
            }
            else
            {
                this.AddCssClass("dropdown-menu");

                writer.AddAttribute("role", "menu");
                writer.AddAttribute("aria-labelledby", "dropdownMenu");
            }

            if (this.RightToLeft == true) this.AddCssClass("pull-right");
            if (!String.IsNullOrEmpty(this.sCssClass)) writer.AddAttribute(HtmlTextWriterAttribute.Class, this.sCssClass);

            base.Render(writer);
        }

        /// <summary>
        /// Renders the list items of a <see cref="T:System.Web.UI.WebControls.BulletedList" /> control as bullets into the specified <see cref="T:System.Web.UI.HtmlTextWriter" />.
        /// </summary>
        /// <param name="writer">An <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        protected override void RenderContents(HtmlTextWriter writer)
        {
            string strClass = "";

            foreach (System.Web.UI.WebControls.ListItem item in this.Items)
            {
                strClass = "";

                if (item.Enabled == false)
                {
                    strClass += " disabled";
                    strClass = strClass.Trim();

                    item.Value = "#";
                }

                if (item.Selected == true && item.Enabled == true)
                {
                    strClass += " active";
                    strClass = strClass.Trim();
                }

                if (!String.IsNullOrEmpty(strClass)) writer.AddAttribute(HtmlTextWriterAttribute.Class, strClass);
                writer.RenderBeginTag(HtmlTextWriterTag.Li);

                if (this.DisplayMode == BulletedListDisplayMode.HyperLink || this.DisplayMode == BulletedListDisplayMode.LinkButton)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Href, item.Value);
                }
                else
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
                }

                writer.AddAttribute(HtmlTextWriterAttribute.Tabindex, "-1");
                writer.RenderBeginTag(HtmlTextWriterTag.A);
                writer.Write(item.Text);
                writer.RenderEndTag();

                writer.RenderEndTag();
            }
        }
    }
}
