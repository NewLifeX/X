namespace XCoder
{
    partial class NewModel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewModel));
            this.dgvTables = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolAddTable = new System.Windows.Forms.ToolStripButton();
            this.toolEidtTable = new System.Windows.Forms.ToolStripButton();
            this.toolDeleteTable = new System.Windows.Forms.ToolStripButton();
            this.toolAddRelation = new System.Windows.Forms.ToolStripButton();
            this.toolSaveModel = new System.Windows.Forms.ToolStripButton();
            this.toolClose = new System.Windows.Forms.ToolStripButton();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTables)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvTables
            // 
            this.dgvTables.AllowUserToAddRows = false;
            this.dgvTables.AllowUserToDeleteRows = false;
            this.dgvTables.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTables.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTables.Location = new System.Drawing.Point(0, 25);
            this.dgvTables.Name = "dgvTables";
            this.dgvTables.ReadOnly = true;
            this.dgvTables.RowTemplate.Height = 23;
            this.dgvTables.Size = new System.Drawing.Size(909, 391);
            this.dgvTables.TabIndex = 4;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolAddTable,
            this.toolEidtTable,
            this.toolDeleteTable,
            this.toolAddRelation,
            this.toolSaveModel,
            this.toolClose});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(909, 25);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolAddTable
            // 
            this.toolAddTable.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolAddTable.Image = ((System.Drawing.Image)(resources.GetObject("toolAddTable.Image")));
            this.toolAddTable.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolAddTable.Name = "toolAddTable";
            this.toolAddTable.Size = new System.Drawing.Size(60, 22);
            this.toolAddTable.Text = "添加表";
            this.toolAddTable.Click += new System.EventHandler(this.toolAddTable_Click);
            // 
            // toolEidtTable
            // 
            this.toolEidtTable.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolEidtTable.Image = ((System.Drawing.Image)(resources.GetObject("toolEidtTable.Image")));
            this.toolEidtTable.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolEidtTable.Name = "toolEidtTable";
            this.toolEidtTable.Size = new System.Drawing.Size(92, 22);
            this.toolEidtTable.Text = "编辑选中表";
            this.toolEidtTable.Click += new System.EventHandler(this.toolEidtTable_Click);
            // 
            // toolDeleteTable
            // 
            this.toolDeleteTable.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolDeleteTable.Image = ((System.Drawing.Image)(resources.GetObject("toolDeleteTable.Image")));
            this.toolDeleteTable.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolDeleteTable.Name = "toolDeleteTable";
            this.toolDeleteTable.Size = new System.Drawing.Size(60, 22);
            this.toolDeleteTable.Text = "删除表";
            this.toolDeleteTable.Click += new System.EventHandler(this.toolDeleteTable_Click);
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
            // toolSaveModel
            // 
            this.toolSaveModel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolSaveModel.Image = ((System.Drawing.Image)(resources.GetObject("toolSaveModel.Image")));
            this.toolSaveModel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolSaveModel.Name = "toolSaveModel";
            this.toolSaveModel.Size = new System.Drawing.Size(76, 22);
            this.toolSaveModel.Text = "保存模型";
            this.toolSaveModel.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // toolClose
            // 
            this.toolClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolClose.Image = ((System.Drawing.Image)(resources.GetObject("toolClose.Image")));
            this.toolClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolClose.Name = "toolClose";
            this.toolClose.Size = new System.Drawing.Size(44, 22);
            this.toolClose.Text = "关闭";
            this.toolClose.Click += new System.EventHandler(this.toolClose_Click);
            // 
            // NewModel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvTables);
            this.Controls.Add(this.toolStrip1);
            this.Name = "NewModel";
            this.Size = new System.Drawing.Size(909, 416);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTables)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvTables;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolEidtTable;
        private System.Windows.Forms.ToolStripButton toolDeleteTable;
        private System.Windows.Forms.ToolStripButton toolAddRelation;
        private System.Windows.Forms.ToolStripButton toolAddTable;
        private System.Windows.Forms.ToolStripButton toolSaveModel;
        private System.Windows.Forms.ToolStripButton toolClose;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}
