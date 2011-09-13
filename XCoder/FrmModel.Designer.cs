namespace XCoder
{
    partial class FrmModel
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmModel));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnAddRelation = new System.Windows.Forms.Button();
            this.btnAddIndex = new System.Windows.Forms.Button();
            this.btnAddColumn = new System.Windows.Forms.Button();
            this.btnAddTable = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cbTables = new System.Windows.Forms.ComboBox();
            this.gv = new System.Windows.Forms.DataGridView();
            this.pgTable = new System.Windows.Forms.PropertyGrid();
            this.pgColumn = new System.Windows.Forms.PropertyGrid();
            this.dgvIndex = new System.Windows.Forms.DataGridView();
            this.dgvRelation = new System.Windows.Forms.DataGridView();
            this.btnCreateTableSQL = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.cbDbTypes = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvIndex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRelation)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.btnAddRelation);
            this.groupBox1.Controls.Add(this.btnAddIndex);
            this.groupBox1.Controls.Add(this.btnAddColumn);
            this.groupBox1.Controls.Add(this.btnAddTable);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cbTables);
            this.groupBox1.Location = new System.Drawing.Point(6, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(609, 44);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            // 
            // btnAddRelation
            // 
            this.btnAddRelation.Location = new System.Drawing.Point(530, 11);
            this.btnAddRelation.Name = "btnAddRelation";
            this.btnAddRelation.Size = new System.Drawing.Size(68, 27);
            this.btnAddRelation.TabIndex = 5;
            this.btnAddRelation.Text = "添加关系";
            this.btnAddRelation.UseVisualStyleBackColor = true;
            this.btnAddRelation.Click += new System.EventHandler(this.btnAddRelation_Click);
            // 
            // btnAddIndex
            // 
            this.btnAddIndex.Location = new System.Drawing.Point(439, 11);
            this.btnAddIndex.Name = "btnAddIndex";
            this.btnAddIndex.Size = new System.Drawing.Size(68, 27);
            this.btnAddIndex.TabIndex = 4;
            this.btnAddIndex.Text = "添加索引";
            this.btnAddIndex.UseVisualStyleBackColor = true;
            this.btnAddIndex.Click += new System.EventHandler(this.btnAddIndex_Click);
            // 
            // btnAddColumn
            // 
            this.btnAddColumn.Location = new System.Drawing.Point(348, 11);
            this.btnAddColumn.Name = "btnAddColumn";
            this.btnAddColumn.Size = new System.Drawing.Size(68, 27);
            this.btnAddColumn.TabIndex = 3;
            this.btnAddColumn.Text = "添加字段";
            this.btnAddColumn.UseVisualStyleBackColor = true;
            this.btnAddColumn.Click += new System.EventHandler(this.btnAddColumn_Click);
            // 
            // btnAddTable
            // 
            this.btnAddTable.Location = new System.Drawing.Point(257, 11);
            this.btnAddTable.Name = "btnAddTable";
            this.btnAddTable.Size = new System.Drawing.Size(68, 27);
            this.btnAddTable.TabIndex = 2;
            this.btnAddTable.Text = "添加表";
            this.btnAddTable.UseVisualStyleBackColor = true;
            this.btnAddTable.Click += new System.EventHandler(this.btnAddTable_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "数据表：";
            // 
            // cbTables
            // 
            this.cbTables.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTables.FormattingEnabled = true;
            this.cbTables.Location = new System.Drawing.Point(65, 14);
            this.cbTables.Name = "cbTables";
            this.cbTables.Size = new System.Drawing.Size(168, 20);
            this.cbTables.TabIndex = 0;
            this.cbTables.SelectedIndexChanged += new System.EventHandler(this.cbTables_SelectedIndexChanged);
            // 
            // gv
            // 
            this.gv.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gv.Location = new System.Drawing.Point(194, 52);
            this.gv.Name = "gv";
            this.gv.RowTemplate.Height = 23;
            this.gv.Size = new System.Drawing.Size(629, 187);
            this.gv.TabIndex = 3;
            this.gv.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.gv_RowEnter);
            // 
            // pgTable
            // 
            this.pgTable.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.pgTable.Location = new System.Drawing.Point(6, 53);
            this.pgTable.Name = "pgTable";
            this.pgTable.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.pgTable.Size = new System.Drawing.Size(182, 412);
            this.pgTable.TabIndex = 4;
            // 
            // pgColumn
            // 
            this.pgColumn.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgColumn.Location = new System.Drawing.Point(830, 53);
            this.pgColumn.Name = "pgColumn";
            this.pgColumn.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.pgColumn.Size = new System.Drawing.Size(182, 412);
            this.pgColumn.TabIndex = 5;
            // 
            // dgvIndex
            // 
            this.dgvIndex.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvIndex.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvIndex.Location = new System.Drawing.Point(194, 245);
            this.dgvIndex.Name = "dgvIndex";
            this.dgvIndex.RowTemplate.Height = 23;
            this.dgvIndex.Size = new System.Drawing.Size(629, 112);
            this.dgvIndex.TabIndex = 6;
            this.dgvIndex.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.gv_RowEnter);
            // 
            // dgvRelation
            // 
            this.dgvRelation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvRelation.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvRelation.Location = new System.Drawing.Point(194, 363);
            this.dgvRelation.Name = "dgvRelation";
            this.dgvRelation.RowTemplate.Height = 23;
            this.dgvRelation.Size = new System.Drawing.Size(629, 102);
            this.dgvRelation.TabIndex = 7;
            this.dgvRelation.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.gv_RowEnter);
            // 
            // btnCreateTableSQL
            // 
            this.btnCreateTableSQL.Location = new System.Drawing.Point(180, 11);
            this.btnCreateTableSQL.Name = "btnCreateTableSQL";
            this.btnCreateTableSQL.Size = new System.Drawing.Size(68, 27);
            this.btnCreateTableSQL.TabIndex = 6;
            this.btnCreateTableSQL.Text = "建表语句";
            this.btnCreateTableSQL.UseVisualStyleBackColor = true;
            this.btnCreateTableSQL.Click += new System.EventHandler(this.btnCreateTableSQL_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 12);
            this.label2.TabIndex = 8;
            this.label2.Text = "数据库类型：";
            // 
            // cbDbTypes
            // 
            this.cbDbTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDbTypes.FormattingEnabled = true;
            this.cbDbTypes.Location = new System.Drawing.Point(92, 14);
            this.cbDbTypes.Name = "cbDbTypes";
            this.cbDbTypes.Size = new System.Drawing.Size(82, 20);
            this.cbDbTypes.TabIndex = 7;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.btnCreateTableSQL);
            this.groupBox2.Controls.Add(this.cbDbTypes);
            this.groupBox2.Location = new System.Drawing.Point(621, 2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(391, 44);
            this.groupBox2.TabIndex = 8;
            this.groupBox2.TabStop = false;
            // 
            // FrmModel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1020, 468);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.dgvRelation);
            this.Controls.Add(this.dgvIndex);
            this.Controls.Add(this.pgColumn);
            this.Controls.Add(this.pgTable);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.gv);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FrmModel";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "数据架构管理";
            this.Load += new System.EventHandler(this.FrmModel_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvIndex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRelation)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbTables;
        private System.Windows.Forms.DataGridView gv;
        private System.Windows.Forms.PropertyGrid pgTable;
        private System.Windows.Forms.PropertyGrid pgColumn;
        private System.Windows.Forms.DataGridView dgvIndex;
        private System.Windows.Forms.DataGridView dgvRelation;
        private System.Windows.Forms.Button btnAddRelation;
        private System.Windows.Forms.Button btnAddIndex;
        private System.Windows.Forms.Button btnAddColumn;
        private System.Windows.Forms.Button btnAddTable;
        private System.Windows.Forms.Button btnCreateTableSQL;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbDbTypes;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}