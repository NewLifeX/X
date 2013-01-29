namespace XCoder
{
    partial class AddTable
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddTable));
            this.dgvColumns = new System.Windows.Forms.DataGridView();
            this.toolAddColumns = new System.Windows.Forms.ToolStripButton();
            this.toolEidtColumn = new System.Windows.Forms.ToolStripButton();
            this.toolAddIndex = new System.Windows.Forms.ToolStripButton();
            this.toolAddRelation = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolDelete = new System.Windows.Forms.ToolStripButton();
            this.toolSave = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.gbTable = new System.Windows.Forms.GroupBox();
            this.combDbType = new System.Windows.Forms.ComboBox();
            this.txtTableRemark = new System.Windows.Forms.TextBox();
            this.txtTableName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvColumns)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.gbTable.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvColumns
            // 
            this.dgvColumns.AllowUserToAddRows = false;
            this.dgvColumns.AllowUserToDeleteRows = false;
            this.dgvColumns.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvColumns.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.dgvColumns.Location = new System.Drawing.Point(0, 120);
            this.dgvColumns.Name = "dgvColumns";
            this.dgvColumns.ReadOnly = true;
            this.dgvColumns.RowTemplate.Height = 23;
            this.dgvColumns.Size = new System.Drawing.Size(934, 240);
            this.dgvColumns.TabIndex = 2;
            // 
            // toolAddColumns
            // 
            this.toolAddColumns.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolAddColumns.Image = ((System.Drawing.Image)(resources.GetObject("toolAddColumns.Image")));
            this.toolAddColumns.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolAddColumns.Name = "toolAddColumns";
            this.toolAddColumns.Size = new System.Drawing.Size(76, 22);
            this.toolAddColumns.Text = "添加字段";
            this.toolAddColumns.Click += new System.EventHandler(this.toolAddColumns_Click);
            // 
            // toolEidtColumn
            // 
            this.toolEidtColumn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolEidtColumn.Image = ((System.Drawing.Image)(resources.GetObject("toolEidtColumn.Image")));
            this.toolEidtColumn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolEidtColumn.Name = "toolEidtColumn";
            this.toolEidtColumn.Size = new System.Drawing.Size(108, 22);
            this.toolEidtColumn.Text = "编辑当前字段";
            this.toolEidtColumn.Click += new System.EventHandler(this.toolEidtColumn_Click);
            // 
            // toolAddIndex
            // 
            this.toolAddIndex.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolAddIndex.Image = ((System.Drawing.Image)(resources.GetObject("toolAddIndex.Image")));
            this.toolAddIndex.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolAddIndex.Name = "toolAddIndex";
            this.toolAddIndex.Size = new System.Drawing.Size(76, 22);
            this.toolAddIndex.Text = "添加索引";
            // 
            // toolAddRelation
            // 
            this.toolAddRelation.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolAddRelation.Image = ((System.Drawing.Image)(resources.GetObject("toolAddRelation.Image")));
            this.toolAddRelation.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolAddRelation.Name = "toolAddRelation";
            this.toolAddRelation.Size = new System.Drawing.Size(76, 22);
            this.toolAddRelation.Text = "添加关系";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolAddColumns,
            this.toolEidtColumn,
            this.toolDelete,
            this.toolAddIndex,
            this.toolAddRelation,
            this.toolSave,
            this.toolStripButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(934, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolDelete
            // 
            this.toolDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolDelete.Image = ((System.Drawing.Image)(resources.GetObject("toolDelete.Image")));
            this.toolDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolDelete.Name = "toolDelete";
            this.toolDelete.Size = new System.Drawing.Size(76, 22);
            this.toolDelete.Text = "删除字段";
            this.toolDelete.Click += new System.EventHandler(this.toolDelete_Click);
            // 
            // toolSave
            // 
            this.toolSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolSave.Image = ((System.Drawing.Image)(resources.GetObject("toolSave.Image")));
            this.toolSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolSave.Name = "toolSave";
            this.toolSave.Size = new System.Drawing.Size(44, 22);
            this.toolSave.Text = "保存";
            this.toolSave.Click += new System.EventHandler(this.toolSave_Click);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(44, 22);
            this.toolStripButton1.Text = "关闭";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // gbTable
            // 
            this.gbTable.Controls.Add(this.combDbType);
            this.gbTable.Controls.Add(this.txtTableRemark);
            this.gbTable.Controls.Add(this.txtTableName);
            this.gbTable.Controls.Add(this.label3);
            this.gbTable.Controls.Add(this.label2);
            this.gbTable.Controls.Add(this.label1);
            this.gbTable.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbTable.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.gbTable.Location = new System.Drawing.Point(0, 25);
            this.gbTable.Name = "gbTable";
            this.gbTable.Size = new System.Drawing.Size(934, 90);
            this.gbTable.TabIndex = 3;
            this.gbTable.TabStop = false;
            this.gbTable.Text = "表基本信息";
            // 
            // combDbType
            // 
            this.combDbType.FormattingEnabled = true;
            this.combDbType.Location = new System.Drawing.Point(111, 53);
            this.combDbType.Name = "combDbType";
            this.combDbType.Size = new System.Drawing.Size(219, 24);
            this.combDbType.TabIndex = 5;
            // 
            // txtTableRemark
            // 
            this.txtTableRemark.Location = new System.Drawing.Point(441, 21);
            this.txtTableRemark.Multiline = true;
            this.txtTableRemark.Name = "txtTableRemark";
            this.txtTableRemark.Size = new System.Drawing.Size(487, 23);
            this.txtTableRemark.TabIndex = 4;
            // 
            // txtTableName
            // 
            this.txtTableName.Location = new System.Drawing.Point(111, 18);
            this.txtTableName.Name = "txtTableName";
            this.txtTableName.Size = new System.Drawing.Size(219, 26);
            this.txtTableName.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(25, 58);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(88, 16);
            this.label3.TabIndex = 2;
            this.label3.Text = "数据库类型";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(379, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 16);
            this.label2.TabIndex = 1;
            this.label2.Text = "表说明";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(73, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "表名";
            // 
            // AddTable
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gbTable);
            this.Controls.Add(this.dgvColumns);
            this.Controls.Add(this.toolStrip1);
            this.Name = "AddTable";
            this.Size = new System.Drawing.Size(934, 360);
            ((System.ComponentModel.ISupportInitialize)(this.dgvColumns)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.gbTable.ResumeLayout(false);
            this.gbTable.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvColumns;
        private System.Windows.Forms.ToolStripButton toolAddColumns;
        private System.Windows.Forms.ToolStripButton toolEidtColumn;
        private System.Windows.Forms.ToolStripButton toolAddIndex;
        private System.Windows.Forms.ToolStripButton toolAddRelation;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.GroupBox gbTable;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtTableRemark;
        private System.Windows.Forms.TextBox txtTableName;
        private System.Windows.Forms.ComboBox combDbType;
        private System.Windows.Forms.ToolStripButton toolSave;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripButton toolDelete;
    }
}
