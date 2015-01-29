// Table.cs

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
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Linq;

namespace NewLife.Bootstrap.Controls
{
    [ToolboxData("<{0}:Table runat=server />")]
    [ParseChildren(true, "Footer")]
    [PersistChildren(false)]
    public class Table : DataBoundControl, INamingContainer
    {

        private System.Web.UI.WebControls.Table table = new System.Web.UI.WebControls.Table();
        private List<BoundColumn> _Columns = new List<BoundColumn>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Table" /> class.
        /// </summary>
        public Table()
        {
            this.Zebra = false;
            this.Condensed = false;
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
        /// Gets the tab pages.
        /// </summary>
        /// <value>
        /// The tab pages.
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [PersistenceMode(PersistenceMode.InnerProperty)]
        public List<BoundColumn> Columns
        {
            get { return _Columns; }
        }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        [PersistenceMode(PersistenceMode.InnerProperty)]
        [TemplateContainer(typeof(Table))]
        [TemplateInstance(TemplateInstance.Single)]
        public virtual ITemplate Footer
        {
            get;
            set;
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool Zebra
        {
            get { return (bool)ViewState["Zebra"]; }
            set { ViewState["Zebra"] = value; }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool Condensed
        {
            get { return (bool)ViewState["Condensed"]; }
            set { ViewState["Condensed"] = value; }
        }

        [Category("Appearance")]
        [DefaultValue(null)]
        public Paginator Pagination
        {
            get;
            set;
        }

        /// <summary>
        /// Sets the paginator.
        /// </summary>
        /// <param name="retrievedData">The retrieved data.</param>
        private void SetPaginator(ref System.Collections.IEnumerable retrievedData)
        {
            if (this.Pagination == null)
            {
                return;
            }

            int intItemCount = retrievedData.Cast<object>().Count();

            if (intItemCount == 0)
            {
                throw new NullReferenceException("DataSource is empty.");
            }
            else
            {
                this.Pagination.ItemCount = intItemCount;
            }

            retrievedData = retrievedData.Cast<object>()
                 .Skip(this.Pagination.PageSize * this.Pagination.CurrentPageIndex)
                 .Take(this.Pagination.PageSize);
        }
        
        /// <summary>
        /// Performs the data binding.
        /// </summary>
        /// <param name="retrievedData">The retrieved data.</param>
        protected override void PerformDataBinding(System.Collections.IEnumerable retrievedData)
        {
            table.Rows.Clear();

            base.PerformDataBinding(retrievedData);

            if (this.Columns.Count == 0)
            {
                throw new Exception("List of columns is null or empty.");
            }

            if (retrievedData == null)
            {
                return;
            }

            this.SetPaginator(ref retrievedData);

            TableRow rowHeader;
            TableRow row;

            rowHeader = new TableRow();
            rowHeader.TableSection = TableRowSection.TableHeader;

            foreach (BoundColumn boundColumn in this.Columns)
            {
                if (!DesignMode && TypeDescriptor.GetProperties(retrievedData.Cast<object>().First()).Find(boundColumn.FieldName, false) == null)
                {
                    throw new NullReferenceException(String.Format("Column with name '{0}' not founded in datasource.", boundColumn.FieldName));
                }

                TableHeaderCell cellHeader = new TableHeaderCell() { Text = boundColumn.Header };
                rowHeader.Cells.Add(cellHeader);
            }

            table.Rows.Add(rowHeader);

            foreach (object dataItem in retrievedData)
            {
                row = new TableRow();
                row.TableSection = TableRowSection.TableBody;

                foreach (BoundColumn boundColumn in this.Columns)
                {
                    PropertyDescriptor prop = TypeDescriptor.GetProperties(dataItem).Find(boundColumn.FieldName, false);

                    if (prop == null)
                    {
                        continue;
                    }

                    if (prop.GetValue(dataItem) == null)
                    {
                        row.Cells.Add(new TableCell());
                        continue;
                    }

                    switch (boundColumn.GetType().FullName)
                    {
                        case "NewLife.Bootstrap.Controls.HyperlinkColumn":
                            row.Cells.Add(CreateHyperlinkColumn(prop, dataItem, (HyperlinkColumn)boundColumn));
                            break;

                        case "NewLife.Bootstrap.Controls.DateColumn":
                            row.Cells.Add(CreateDateColumn(prop, dataItem, (DateColumn)boundColumn));
                            break;

                        case "NewLife.Bootstrap.Controls.CheckBoxColumn":
                            row.Cells.Add(CreateCheckBoxColumn(prop, dataItem, (CheckBoxColumn)boundColumn));
                            break;

                        default:
                            row.Cells.Add(CreateColumn(prop, dataItem, boundColumn));
                            break;
                    }
                }

                table.Rows.Add(row);
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
            this.AddCssClass(this.CssClass);
            this.AddCssClass("table");

            if (this.Zebra == true) this.AddCssClass("table-striped");
            if (this.Condensed == true) this.AddCssClass("table-condensed");

            table.ID = this.ID;
            table.CssClass = this.sCssClass;

            var footer = new Control();
            this.Footer.InstantiateIn(footer);

            this.Controls.Clear();
            this.Controls.Add(table);
            this.Controls.Add(footer);
        }

        /// <summary>
        /// Notifies the server control that an element, either XML or HTML, was parsed, and adds the element to the server control's <see cref="T:System.Web.UI.ControlCollection" /> object.
        /// </summary>
        /// <param name="obj">An <see cref="T:System.Object" /> that represents the parsed element.</param>
        protected override void AddParsedSubObject(object obj)
        {
            if (obj is BoundColumn)
            {
                Columns.Add((BoundColumn)obj);
                return;
            }
        }

        #region Create Columns methods

        /// <summary>
        /// Creates the column.
        /// </summary>
        /// <param name="prop">The prop.</param>
        /// <param name="dataItem">The data item.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        private TableCell CreateColumn(PropertyDescriptor prop, object dataItem, BoundColumn column)
        {
            TableCell cell = new TableCell() { Text = prop.GetValue(dataItem).ToString() };

            if (column.Width.HasValue)
            {
                cell.Width = column.Width.Value;
            }

            return cell;
        }

        /// <summary>
        /// Creates the check box column.
        /// </summary>
        /// <param name="prop">The prop.</param>
        /// <param name="dataItem">The data item.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        private TableCell CreateCheckBoxColumn(PropertyDescriptor prop, object dataItem, CheckBoxColumn column)
        {
            TableCell cell = new TableCell();

            CheckBox checkBox = new CheckBox() { Text = "" };
            checkBox.Checked = (bool)prop.GetValue(dataItem);
            cell.Controls.Add(checkBox);

            if (column.Width.HasValue)
            {
                cell.Width = column.Width.Value;
            }

            return cell;
        }

        /// <summary>
        /// Creates the date column.
        /// </summary>
        /// <param name="prop">The prop.</param>
        /// <param name="dataItem">The data item.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        private TableCell CreateDateColumn(PropertyDescriptor prop, object dataItem, DateColumn column)
        {
            TableCell cell = new TableCell() { Text = String.Format(column.DateFormatString, prop.GetValue(dataItem).ToString()) };

            if (column.Width.HasValue)
            {
                cell.Width = column.Width.Value;
            }

            return cell;
        }

        /// <summary>
        /// Creates the hyperlink column.
        /// </summary>
        /// <param name="prop">The prop.</param>
        /// <param name="dataItem">The data item.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        private TableCell CreateHyperlinkColumn(PropertyDescriptor prop, object dataItem, HyperlinkColumn column)
        {
            string strNavigationUrl = prop.GetValue(dataItem).ToString();
            strNavigationUrl = String.Format(column.NavigationUrlFormatString, strNavigationUrl);
            strNavigationUrl = this.ResolveClientUrl(strNavigationUrl);

            HyperLink a = new HyperLink() { Text = prop.GetValue(dataItem).ToString() };
            a.NavigateUrl = strNavigationUrl;
            a.Target = column.Target;

            TableCell cell = new TableCell();
            cell.Controls.Add(a);

            if (column.Width.HasValue)
            {
                cell.Width = column.Width.Value;
            }

            return cell;
        }

        #endregion
    }
}
