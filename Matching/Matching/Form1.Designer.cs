
namespace Matching
{
    partial class Form1
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
            this.RefImg = new System.Windows.Forms.Button();
            this.tplimg = new System.Windows.Forms.Button();
            this.Matching = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.SuspendLayout();
            // 
            // RefImg
            // 
            this.RefImg.Location = new System.Drawing.Point(22, 41);
            this.RefImg.Name = "RefImg";
            this.RefImg.Size = new System.Drawing.Size(124, 56);
            this.RefImg.TabIndex = 0;
            this.RefImg.Text = "Open Input";
            this.RefImg.UseVisualStyleBackColor = true;
            this.RefImg.Click += new System.EventHandler(this.RefImg_Click);
            // 
            // tplimg
            // 
            this.tplimg.Location = new System.Drawing.Point(264, 41);
            this.tplimg.Name = "tplimg";
            this.tplimg.Size = new System.Drawing.Size(117, 56);
            this.tplimg.TabIndex = 1;
            this.tplimg.Text = "Open Template";
            this.tplimg.UseVisualStyleBackColor = true;
            this.tplimg.Click += new System.EventHandler(this.Tplimg_Click);
            // 
            // Matching
            // 
            this.Matching.Location = new System.Drawing.Point(592, 40);
            this.Matching.Name = "Matching";
            this.Matching.Size = new System.Drawing.Size(120, 58);
            this.Matching.TabIndex = 2;
            this.Matching.Text = "Matching";
            this.Matching.UseVisualStyleBackColor = true;
            this.Matching.Click += new System.EventHandler(this.Matching_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 144);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(157, 123);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Location = new System.Drawing.Point(243, 144);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(157, 123);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 4;
            this.pictureBox2.TabStop = false;
            // 
            // pictureBox3
            // 
            this.pictureBox3.Location = new System.Drawing.Point(566, 144);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(157, 123);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox3.TabIndex = 5;
            this.pictureBox3.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.pictureBox3);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.Matching);
            this.Controls.Add(this.tplimg);
            this.Controls.Add(this.RefImg);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button RefImg;
        private System.Windows.Forms.Button tplimg;
        private System.Windows.Forms.Button Matching;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox3;
    }
}

