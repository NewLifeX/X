namespace XCoder.Tools
{
    partial class FrmGPS
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txt_latlong = new System.Windows.Forms.TextBox();
            this.btn_latlong = new System.Windows.Forms.Button();
            this.txt_16_long = new System.Windows.Forms.TextBox();
            this.txt_16_lat = new System.Windows.Forms.TextBox();
            this.txt_long = new System.Windows.Forms.TextBox();
            this.txt_lat = new System.Windows.Forms.TextBox();
            this.btn_16_latlong = new System.Windows.Forms.Button();
            this.txt_16_latlong = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txt_latlong);
            this.groupBox1.Controls.Add(this.btn_latlong);
            this.groupBox1.Controls.Add(this.txt_16_long);
            this.groupBox1.Controls.Add(this.txt_16_lat);
            this.groupBox1.Controls.Add(this.txt_long);
            this.groupBox1.Controls.Add(this.txt_lat);
            this.groupBox1.Controls.Add(this.btn_16_latlong);
            this.groupBox1.Controls.Add(this.txt_16_latlong);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(558, 149);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "GPS HEX转坐标";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(21, 113);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 10;
            this.label3.Text = "HEX Long";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 88);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 12);
            this.label2.TabIndex = 9;
            this.label2.Text = "HEX Lat";
            // 
            // txt_latlong
            // 
            this.txt_latlong.Location = new System.Drawing.Point(318, 94);
            this.txt_latlong.Name = "txt_latlong";
            this.txt_latlong.Size = new System.Drawing.Size(215, 21);
            this.txt_latlong.TabIndex = 8;
            // 
            // btn_latlong
            // 
            this.btn_latlong.Location = new System.Drawing.Point(270, 92);
            this.btn_latlong.Name = "btn_latlong";
            this.btn_latlong.Size = new System.Drawing.Size(34, 23);
            this.btn_latlong.TabIndex = 7;
            this.btn_latlong.Text = ">>";
            this.btn_latlong.UseVisualStyleBackColor = true;
            this.btn_latlong.Click += new System.EventHandler(this.btn_latlong_Click);
            // 
            // txt_16_long
            // 
            this.txt_16_long.Location = new System.Drawing.Point(77, 110);
            this.txt_16_long.Name = "txt_16_long";
            this.txt_16_long.Size = new System.Drawing.Size(177, 21);
            this.txt_16_long.TabIndex = 6;
            // 
            // txt_16_lat
            // 
            this.txt_16_lat.Location = new System.Drawing.Point(77, 83);
            this.txt_16_lat.Name = "txt_16_lat";
            this.txt_16_lat.Size = new System.Drawing.Size(177, 21);
            this.txt_16_lat.TabIndex = 5;
            // 
            // txt_long
            // 
            this.txt_long.Location = new System.Drawing.Point(318, 49);
            this.txt_long.Name = "txt_long";
            this.txt_long.Size = new System.Drawing.Size(215, 21);
            this.txt_long.TabIndex = 4;
            // 
            // txt_lat
            // 
            this.txt_lat.Location = new System.Drawing.Point(318, 22);
            this.txt_lat.Name = "txt_lat";
            this.txt_lat.Size = new System.Drawing.Size(215, 21);
            this.txt_lat.TabIndex = 3;
            // 
            // btn_16_latlong
            // 
            this.btn_16_latlong.Location = new System.Drawing.Point(270, 18);
            this.btn_16_latlong.Name = "btn_16_latlong";
            this.btn_16_latlong.Size = new System.Drawing.Size(34, 23);
            this.btn_16_latlong.TabIndex = 2;
            this.btn_16_latlong.Text = ">>";
            this.btn_16_latlong.UseVisualStyleBackColor = true;
            this.btn_16_latlong.Click += new System.EventHandler(this.btn_16_latlong_Click);
            // 
            // txt_16_latlong
            // 
            this.txt_16_latlong.Location = new System.Drawing.Point(77, 21);
            this.txt_16_latlong.Name = "txt_16_latlong";
            this.txt_16_latlong.Size = new System.Drawing.Size(177, 21);
            this.txt_16_latlong.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "HEX LatLong";
            // 
            // FrmGPS
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(582, 172);
            this.Controls.Add(this.groupBox1);
            this.Name = "FrmGPS";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "GPS坐标转换";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txt_latlong;
        private System.Windows.Forms.Button btn_latlong;
        private System.Windows.Forms.TextBox txt_16_long;
        private System.Windows.Forms.TextBox txt_16_lat;
        private System.Windows.Forms.TextBox txt_long;
        private System.Windows.Forms.TextBox txt_lat;
        private System.Windows.Forms.Button btn_16_latlong;
        private System.Windows.Forms.TextBox txt_16_latlong;
        private System.Windows.Forms.Label label1;
    }
}