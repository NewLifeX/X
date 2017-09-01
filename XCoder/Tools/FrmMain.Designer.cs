namespace XCoder.Tools
{
    partial class FrmMain
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
            this.btn_gps = new System.Windows.Forms.Button();
            this.btn_Include = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_gps
            // 
            this.btn_gps.Location = new System.Drawing.Point(22, 12);
            this.btn_gps.Name = "btn_gps";
            this.btn_gps.Size = new System.Drawing.Size(75, 23);
            this.btn_gps.TabIndex = 1;
            this.btn_gps.Text = "GPS坐标转换";
            this.btn_gps.UseVisualStyleBackColor = true;
            this.btn_gps.Click += new System.EventHandler(this.btn_gps_Click);
            // 
            // btn_Include
            // 
            this.btn_Include.Location = new System.Drawing.Point(131, 11);
            this.btn_Include.Name = "btn_Include";
            this.btn_Include.Size = new System.Drawing.Size(101, 23);
            this.btn_Include.TabIndex = 2;
            this.btn_Include.Text = "Xcode实体包含";
            this.btn_Include.UseVisualStyleBackColor = true;
            this.btn_Include.Click += new System.EventHandler(this.btn_Include_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btn_Include);
            this.panel1.Controls.Add(this.btn_gps);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(674, 47);
            this.panel1.TabIndex = 3;
            // 
            // panel2
            // 
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 47);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(674, 422);
            this.panel2.TabIndex = 4;
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(674, 469);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "小工具";
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btn_gps;
        private System.Windows.Forms.Button btn_Include;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
    }
}