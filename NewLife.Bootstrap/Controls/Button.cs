// Button.cs

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
    public enum ButtonTypes
    {
        Default = 0,
        Primary = 1,
        Info = 2,
        Success = 3,
        Warning = 4,
        Danger = 5,
        Inverse = 6,
        Link = 7
    }

    public enum ButtonSizes
    {
        Default = 0,
        Large = 1,
        Small = 2,
        Mini = 3
    }

    [ToolboxData("<{0}:Button runat=server />")]
    [DefaultProperty("Text")]
    [ParseChildren(true, "Menu")]
    public class Button : System.Web.UI.WebControls.Button
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
        /// Gets or sets the footer.
        /// </summary>
        /// <value>
        /// The footer.
        /// </value>
        [PersistenceMode(PersistenceMode.InnerProperty)]
        [TemplateContainer(typeof(Window))]
        [TemplateInstance(TemplateInstance.Single)]
        public virtual ITemplate Menu
        {
            get;
            set;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Button" /> class.
        /// </summary>
        public Button()
        {
            this.ButtonType = ButtonTypes.Default;
            this.ButtonSize = ButtonSizes.Default;
            this.Block = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Button" /> is block.
        /// </summary>
        /// <value>
        ///   <c>true</c> if block; otherwise, <c>false</c>.
        /// </value>
        [Category("Appearance")]
        [DefaultValue(false)]
        public bool Block
        {
            get { return (bool)ViewState["Block"]; }
            set { ViewState["Block"] = value; }
        }

        /// <summary>
        /// Gets or sets the type of the button.
        /// </summary>
        /// <value>
        /// The type of the button.
        /// </value>
        [Category("Appearance")]
        [DefaultValue(ButtonTypes.Default)]
        public ButtonTypes ButtonType
        {
            get { return (ButtonTypes)ViewState["ButtonType"]; }
            set { ViewState["ButtonType"] = value; }
        }

        /// <summary>
        /// Gets or sets the size of the button.
        /// </summary>
        /// <value>
        /// The size of the button.
        /// </value>
        [Category("Appearance")]
        [DefaultValue(ButtonSizes.Default)]
        public ButtonSizes ButtonSize
        {
            get { return (ButtonSizes)ViewState["ButtonSize"]; }
            set { ViewState["ButtonSize"] = value; }
        }

        /// <summary>
        /// Renders the control to the specified HTML writer.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the control content.</param>
        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            this.AddCssClass(this.CssClass);
            this.AddCssClass("btn");
            this.AddCssClass(this.GetCssButtonType());

            if (this.Enabled == false)
            {
                this.AddCssClass("disabled");
            }

            if (this.Block == true)
            {
                this.AddCssClass("btn-block");
            }

            switch (this.ButtonSize)
            {
                case ButtonSizes.Large:
                    this.AddCssClass("btn-large");
                    break;

                case ButtonSizes.Small:
                    this.AddCssClass("btn-small");
                    break;

                case ButtonSizes.Mini:
                    this.AddCssClass("btn-mini");
                    break;

                default:
                    break;
            }

            if (!String.IsNullOrEmpty(this.sCssClass)) writer.AddAttribute(HtmlTextWriterAttribute.Class, this.sCssClass);
            base.Render(writer);
        }

        /// <summary>
        /// Renders the HTML opening tag of the control to the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            if (this.Controls[0].Controls.Count > 0)
            {
                writer.Write("<div class=\"btn-group\">");
            }
            
            base.RenderBeginTag(writer);
        }

        /// <summary>
        /// Renders the HTML closing tag of the control into the specified writer. This method is used primarily by control developers.
        /// </summary>
        /// <param name="writer">A <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
            base.RenderEndTag(writer);

            if (this.Controls[0].Controls.Count > 0)
            {
                writer.Write("<button class=\"btn " + this.GetCssButtonType() + " dropdown-toggle\" data-toggle=\"dropdown\">");
                writer.Write("<span class=\"caret\"></span>");
                writer.Write("</button>");

                this.RenderChildren(writer);
                writer.Write("</div>");
            }
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
            // Remove any controls
            this.Controls.Clear();

            // Add all footer to a container.
            var menu = new Control();
            this.Menu.InstantiateIn(menu);

            // Add container to the control collection.
            this.Controls.Add(menu);
        }

        /// <summary>
        /// Gets the type of the CSS button.
        /// </summary>
        /// <returns></returns>
        private string GetCssButtonType()
        {
            string str = "";

            switch (this.ButtonType)
            {
                case ButtonTypes.Primary:
                    str = "btn-primary";
                    break;

                case ButtonTypes.Info:
                    str = "btn-info";
                    break;

                case ButtonTypes.Success:
                    str = "btn-success";
                    break;

                case ButtonTypes.Warning:
                    str = "btn-warning";
                    break;

                case ButtonTypes.Danger:
                    str = "btn-danger";
                    break;

                case ButtonTypes.Inverse:
                    str = "btn-inverse";
                    break;

                case ButtonTypes.Link:
                    str = "btn-link";
                    break;

                default:
                    break;
            }

            return str;
        }
    }
}