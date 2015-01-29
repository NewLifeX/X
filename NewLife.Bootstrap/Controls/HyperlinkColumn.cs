// HyperlinkColumn.cs

// Copyright (C) 2013 Pedro Fernandes

// This program is free software; you can redistribute it and/or modify it under the terms of the GNU 
// General Public License as published by the Free Software Foundation; either version 2 of the 
// License, or (at your option) any later version.

// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without 
// even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See 
// the GNU General Public License for more details. You should have received a copy of the GNU 
// General Public License along with this program; if not, write to the Free Software Foundation, Inc., 59 
// Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System.ComponentModel;
using System.Web.UI;

namespace NewLife.Bootstrap.Controls
{
    [ToolboxData("<{0}:HyperlinkColumn runat=server></{0}:HyperlinkColumn>")]
    [ToolboxItem(false)]
    public class HyperlinkColumn : BoundColumn
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="HyperlinkColumn" /> class.
        /// </summary>
        public HyperlinkColumn()
        {
            this.NavigationUrlFormatString = "{0}";
            this.Target = "_top";
        }

        /// <summary>
        /// Gets or sets the navigation URL format string.
        /// </summary>
        /// <value>
        /// The navigation URL format string.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [DefaultValue("{0}")]
        [UrlProperty]
        public string NavigationUrlFormatString
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the navigation URL field.
        /// </summary>
        /// <value>
        /// The navigation URL field.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [DefaultValue("")]
        [UrlProperty]
        public string NavigationUrlField
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>
        /// The target.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [DefaultValue("_top")]
        [UrlProperty]
        public string Target
        {
            get;
            set;
        }

    }
}
