// AccordionContentHeaderPanel.cs

// Copyright (C) 2013 Francois Viljoen

// This program is free software; you can redistribute it and/or modify it under the terms of the GNU 
// General Public License as published by the Free Software Foundation; either version 2 of the 
// License, or (at your option) any later version.

// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without 
// even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See 
// the GNU General Public License for more details. You should have received a copy of the GNU 
// General Public License along with this program; if not, write to the Free Software Foundation, Inc., 59 
// Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.ComponentModel;
namespace NewLife.Bootstrap.Controls
{
    [ToolboxItem(false)]
    public class AccordionContentHeaderPanel : WebControl, INamingContainer
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="AccordionContentHeaderPanel"/> class.
        /// </summary>
        public AccordionContentHeaderPanel()
        {
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
            this.AddCssClass("accordion-heading");
            //<a class="accordion-toggle" data-toggle="collapse" data-parent="#accordion2" href="#collapse1">
            //writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
            //writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
            if (!String.IsNullOrEmpty(this.sCssClass)) writer.AddAttribute(HtmlTextWriterAttribute.Class, this.sCssClass);



            base.Render(writer);
        }

        /// <summary>
        /// Renders the HTML closing tag of the control into the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
            writer.RenderEndTag();
        }

        /// <summary>
        /// Renders the contents.
        /// </summary>
        /// <param name="output">The output.</param>
        protected override void RenderContents(HtmlTextWriter output)
        {
            output.Write("<a class='accordion-toggle' data-toggle='collapse' data-parent='#" + this.Parent.Parent.ClientID + "' href='#" + this.Parent.ClientID + "'>");
            base.RenderContents(output);
            output.Write("</a>");
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit(System.EventArgs e)
        {
            base.OnInit(e);

            // Initialize all child controls.
            this.CreateChildControls();
            this.ChildControlsCreated = true;
        }


    }
}