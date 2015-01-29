// Breadcrumbs.cs

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
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NewLife.Bootstrap.Controls
{
    [ToolboxData("<{0}:Breadcrumbs runat=server></{0}:Breadcrumbs>")]
    [ToolboxBitmap(typeof(System.Web.UI.WebControls.Image))]
    [ParseChildren(true, "Items")]
    [PersistChildren(false)]
    public class Breadcrumbs : WebControl, INamingContainer
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

        private ListItemCollection _items;

        /// <summary>
        /// Initializes a new instance of the <see cref="Breadcrumbs" /> class.
        /// </summary>
        public Breadcrumbs()
        {
            _items = new ListItemCollection(this);
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [PersistenceMode(PersistenceMode.InnerProperty)]
        public ListItemCollection Items
        {
            get { return _items; }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            // Initialize all child controls.
            this.CreateChildControls();
            this.ChildControlsCreated = true; 
        }

        /// <summary>
        /// Renders the HTML opening tag of the control to the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Ul);
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
            if (_items.Count != 0)
            {
                // Disable last Divider
                _items[_items.Count - 1].Divider = false;
                _items[_items.Count - 1].Active = true;
            }

            this.AddCssClass(this.CssClass);
            this.AddCssClass("breadcrumb");

            writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, this.sCssClass);
            base.Render(writer);
        }

        /// <summary>
        /// Gets a <see cref="T:System.Web.UI.ControlCollection" /> object that represents the child controls for a specified server control in the UI hierarchy.
        /// </summary>
        /// <returns>
        /// The collection of child controls for the specified server control.
        ///   </returns>
        public override ControlCollection Controls
        {
            get
            {
                this.EnsureChildControls();
                return base.Controls;
            }
        }

        /// <summary>
        /// Notifies the server control that an element, either XML or HTML, was parsed, and adds the element to the server control's <see cref="T:System.Web.UI.ControlCollection" /> object.
        /// </summary>
        /// <param name="obj">An <see cref="T:System.Object" /> that represents the parsed element.</param>
        protected override void AddParsedSubObject(object obj)
        {
            if (obj is ListItem)
            {
                Items.Add((ListItem)obj);
                return;
            }
        }
    }
}