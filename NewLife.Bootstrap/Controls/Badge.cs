// Badge.cs

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
    public enum LabelTypes
    {
        Default = 0,
        Success = 1,
        Warning = 2,
        Important = 3,
        Info = 4,
        Inverse = 5
    }

    [ToolboxData("<{0}:Badge runat=server />")]
    [DefaultProperty("Text")]
    public class Badge : System.Web.UI.WebControls.Label
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
        /// Initializes a new instance of the <see cref="Badge" /> class.
        /// </summary>
        public Badge()
        {
            this.LabelType = LabelTypes.Default;
            this.Label = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Badge" /> is label.
        /// </summary>
        /// <value>
        ///   <c>true</c> if label; otherwise, <c>false</c>.
        /// </value>
        [Category("Appearance")]
        [DefaultValue(true)]
        public bool Label
        {
            get { return (bool)ViewState["Label"]; }
            set { ViewState["Label"] = value; }
        }

        /// <summary>
        /// Gets or sets the type of the label.
        /// </summary>
        /// <value>
        /// The type of the label.
        /// </value>
        [Category("Appearance")]
        [DefaultValue(LabelTypes.Default)]
        public LabelTypes LabelType
        {
            get { return (LabelTypes)ViewState["LabelType"]; }
            set { ViewState["LabelType"] = value; }
        }

        /// <summary>
        /// Renders the control to the specified HTML writer.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the control content.</param>
        protected override void Render(HtmlTextWriter writer)
        {
            this.AddCssClass(this.CssClass);

            string str = "";

            if (this.Label == true)
            {
                str += "label";
            }
            else
            {
                str += "badge";
            }

            this.AddCssClass(str);

            switch (this.LabelType)
            {
                case LabelTypes.Success:
                    this.AddCssClass(str + "-success");
                    break;

                case LabelTypes.Warning:
                    this.AddCssClass(str + "-warning");
                    break;

                case LabelTypes.Important:
                    this.AddCssClass(str + "-important");
                    break;

                case LabelTypes.Info:
                    this.AddCssClass(str + "-info");
                    break;

                case LabelTypes.Inverse:
                    this.AddCssClass(str + "-inverse");
                    break;

                default:
                    break;
            }

            if (!String.IsNullOrEmpty(this.sCssClass)) writer.AddAttribute(HtmlTextWriterAttribute.Class, this.sCssClass);
            base.Render(writer);
        }

    }

}