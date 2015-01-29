// BoundColumn.cs

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
using System.Web.UI.WebControls;

namespace NewLife.Bootstrap.Controls
{

    [ToolboxData("<{0}:BoundColumn runat=server></{0}:BoundColumn>")]
    [ToolboxItem(false)]
    public class BoundColumn : Control, INamingContainer
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TabPage" /> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [DefaultValue("")]
        public string Header
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [Localizable(true)]
        [DefaultValue("")]
        public string FieldName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [Localizable(true)]
        public Unit? Width
        {
            get;
            set;
        }
    }
}
