// DateColumn.cs

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
    [ToolboxData("<{0}:DateColumn runat=server></{0}:DateColumn>")]
    [ToolboxItem(false)]
    public class DateColumn : BoundColumn
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateColumn" /> class.
        /// </summary>
        public DateColumn()
        {
            this.DateFormatString = "{0:dd/MM/yyyy hh:mm}";
        }

        /// <summary>
        /// Gets or sets the date format string.
        /// </summary>
        /// <value>
        /// The date format string.
        /// </value>
        [NotifyParentProperty(true)]
        [Browsable(true)]
        [DefaultValue("{0:dd/MM/yyyy hh:mm}")]
        [UrlProperty]
        public string DateFormatString
        {
            get;
            set;
        }
    }
}
