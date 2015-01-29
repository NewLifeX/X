// ListItemCollection.cs

// Copyright (C) 2013 Pedro Fernandes

// This program is free software; you can redistribute it and/or modify it under the terms of the GNU 
// General Public License as published by the Free Software Foundation; either version 2 of the 
// License, or (at your option) any later version.

// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without 
// even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See 
// the GNU General Public License for more details. You should have received a copy of the GNU 
// General Public License along with this program; if not, write to the Free Software Foundation, Inc., 59 
// Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System.Collections;
using System.Web.UI;

namespace NewLife.Bootstrap.Controls
{
    public class ListItemCollection : CollectionBase
    {
        protected Control Parent = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemCollection" /> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public ListItemCollection(Control parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Indexer property for the collection that returns and sets an item
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public ListItem this[int index]
        {
            get { return (ListItem)List[index]; }
            set { List[index] = value; }
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(ListItem item)
        {
            List.Add(item);
            Parent.Controls.Add(item);
        }

        /// <summary>
        /// Inserts the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        public void Insert(int index, ListItem item)
        {
            List.Insert(index, item);
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Remove(ListItem item)
        {
            List.Remove(item);
        }

        /// <summary>
        /// Determines whether [contains] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(ListItem item)
        {
            return List.Contains(item);
        }

        /// <summary>
        /// Indexes the of.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public int IndexOf(ListItem item)
        {
            return List.IndexOf(item);
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The index.</param>
        public void CopyTo(ListItem[] array, int index)
        {
            List.CopyTo(array, index);
        }
    }
}
