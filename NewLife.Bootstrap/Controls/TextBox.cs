// TextBox.cs

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

    [ToolboxData("<{0}:TextBox runat=server />")]
    [DefaultProperty("Text")]
    public class TextBox : System.Web.UI.WebControls.TextBox
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
        /// Initializes a new instance of the <see cref="TextBox" /> class.
        /// </summary>
        public TextBox()
        {
            this.PlaceHolder = "";
        }

        /// <summary>
        /// Gets or sets the place holder.
        /// </summary>
        /// <value>
        /// The place holder.
        /// </value>
        [Category("Appearance")]
        [DefaultValue("")]
        public string PlaceHolder
        {
            get { return (string)ViewState["PlaceHolder"]; }
            set { ViewState["PlaceHolder"] = value; }
        }

        /// <summary>
        /// Renders the <see cref="T:System.Web.UI.WebControls.TextBox" /> control to the specified <see cref="T:System.Web.UI.HtmlTextWriter" /> object.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> that receives the rendered output.</param>
        protected override void Render(HtmlTextWriter writer)
        {
            this.AddCssClass(this.CssClass);

            if (this.ReadOnly == true)
            {
               this.AddCssClass("uneditable-input");
            }

            if (!String.IsNullOrEmpty(this.sCssClass)) writer.AddAttribute(HtmlTextWriterAttribute.Class, this.sCssClass);
            if (!String.IsNullOrEmpty(this.PlaceHolder)) writer.AddAttribute("placeholder", this.PlaceHolder);

            base.Render(writer);
        }
    }
}