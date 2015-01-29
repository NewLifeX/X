// MediaObject.cs

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
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NewLife.Bootstrap.Controls
{
    public enum ImageAlign
    {
        Left = 0,
        Right = 1
    }

    [ToolboxData("<{0}:MediaObject runat=server></{0}:MediaObject>")]
    [DefaultProperty("Title")]
    [ParseChildren(true, "Inner")]
    [PersistChildren(false)]
    public class MediaObject : WebControl, INamingContainer
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
        /// Initializes a new instance of the <see cref="MediaObject" /> class.
        /// </summary>
        public MediaObject()
        {
            this.ImageAlign = ImageAlign.Left;
        }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        [PersistenceMode(PersistenceMode.InnerProperty)]
        [TemplateContainer(typeof(MediaObject))]
        [TemplateInstance(TemplateInstance.Single)]
        public virtual ITemplate Inner
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the image align.
        /// </summary>
        /// <value>
        /// The image align.
        /// </value>
        [Category("Appearance")]
        [DefaultValue(ImageAlign.Left)]
        public ImageAlign ImageAlign
        {
            get { return (ImageAlign)ViewState["ImageAlign"]; }
            set { ViewState["ImageAlign"] = value; }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        [Category("Appearance")]
        [DefaultValue("")]
        public string Title
        {
            get { return (string)ViewState["Title"]; }
            set { ViewState["Title"] = value; }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        [Category("Appearance")]
        [DefaultValue("")]
        public string Description
        {
            get { return (string)ViewState["Description"]; }
            set { ViewState["Description"] = value; }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        [Category("Appearance")]
        [DefaultValue("")]
        [UrlProperty]
        public string ImageUrl
        {
            get { return (string)ViewState["ImageUrl"]; }
            set { ViewState["ImageUrl"] = value; }
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
        /// Renders the HTML closing tag of the control into the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
            writer.RenderEndTag();
        }

        /// <summary>
        /// Renders the control to the specified HTML writer.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the control content.</param>
        protected override void Render(HtmlTextWriter writer)
        {
            this.AddCssClass(this.CssClass);
            this.AddCssClass("media");

            writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
            if (!String.IsNullOrEmpty(this.sCssClass)) writer.AddAttribute(HtmlTextWriterAttribute.Class, this.sCssClass);

            base.Render(writer);
        }

        /// <summary>
        /// Renders the contents.
        /// </summary>
        /// <param name="output">The output.</param>
        protected override void RenderContents(HtmlTextWriter output)
        {        
            this.RenderImage(output);

            output.AddAttribute(HtmlTextWriterAttribute.Class, "media-body");
            output.RenderBeginTag(HtmlTextWriterTag.Div);

            this.RenderTitle(output);
            this.RenderDescription(output);

            output.RenderBeginTag(HtmlTextWriterTag.Div);
            this.RenderChildren(output);
            output.RenderEndTag();

            output.RenderEndTag(); // Div   
        }

        /// <summary>
        /// Renders the image.
        /// </summary>
        /// <param name="output">The output.</param>
        private void RenderImage(HtmlTextWriter output)
        {
            switch (ImageAlign)
            {
                case ImageAlign.Left:
                    output.AddAttribute(HtmlTextWriterAttribute.Class, "pull-left");
                    break;

                case ImageAlign.Right:
                    output.AddAttribute(HtmlTextWriterAttribute.Class, "pull-right");
                    break;
            }
            
            output.AddAttribute(HtmlTextWriterAttribute.Id, "#");
            output.RenderBeginTag(HtmlTextWriterTag.A);            

            output.AddAttribute(HtmlTextWriterAttribute.Class, "media-object");
            output.AddAttribute("data-src", this.ImageUrl);
            output.AddAttribute("src", this.ImageUrl);
            output.RenderBeginTag(HtmlTextWriterTag.Img);
            output.RenderEndTag(); // Img
            output.RenderEndTag(); // A
        }

        /// <summary>
        /// Renders the title.
        /// </summary>
        /// <param name="output">The output.</param>
        private void RenderTitle(HtmlTextWriter output)
        {
            output.AddAttribute(HtmlTextWriterAttribute.Class, "media-heading");
            output.RenderBeginTag(HtmlTextWriterTag.H4);
            output.Write(this.Title);
            output.RenderEndTag();
        }

        /// <summary>
        /// Renders the description.
        /// </summary>
        /// <param name="output">The output.</param>
        private void RenderDescription(HtmlTextWriter output)
        {
            output.RenderBeginTag(HtmlTextWriterTag.P);
            output.Write(this.Description);
            output.RenderEndTag();
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

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            var container = new Control();
            this.Inner.InstantiateIn(container);

            this.Controls.Clear();
            this.Controls.Add(container);
        }
    }
}
