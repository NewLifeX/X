// Image.cs

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

    public enum ImageTypes
    {
        None = 0,
        Rounded = 1,
        Circle = 2,
        Polaroid = 3
    }

    [ToolboxData("<{0}:Button runat=server />")]
    [DefaultProperty("ImageUrl")]
    public class Image : System.Web.UI.WebControls.Image
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
        /// Initializes a new instance of the <see cref="Image" /> class.
        /// </summary>
        public Image()
        {
            this.ImageType = ImageTypes.None;
        }

        /// <summary>
        /// Gets or sets the type of the image.
        /// </summary>
        /// <value>
        /// The type of the image.
        /// </value>
        [Category("Appearance")]
        [DefaultValue(ImageTypes.None)]
        public ImageTypes ImageType
        {
            get { return (ImageTypes)ViewState["ImageType"]; }
            set { ViewState["ImageType"] = value; }
        }

        /// <summary>
        /// Renders the control to the specified HTML writer.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the control content.</param>
        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            this.AddCssClass(this.CssClass);

            switch (this.ImageType)
            {
                case ImageTypes.Rounded:
                    this.AddCssClass("img-rounded");
                    break;

                case ImageTypes.Circle:
                    this.AddCssClass("img-circle");
                    break;

                case ImageTypes.Polaroid:
                    this.AddCssClass("img-polaroid");
                    break;

                default:
                    break;
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
            if (!String.IsNullOrEmpty(this.sCssClass)) writer.AddAttribute(HtmlTextWriterAttribute.Class, this.sCssClass);

            base.Render(writer);
        }
    }
}