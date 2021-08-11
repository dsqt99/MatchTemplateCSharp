using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Diagnostics;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace Matching
{
    public partial class Form1 : Form
    {
        Mat ref_img;
        Mat tpl_img;
        public Form1()
        {
            InitializeComponent();
        }

        private void RefImg_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog op = new OpenFileDialog();
                if (op.ShowDialog() == DialogResult.OK)
                {
                    ref_img = new Mat(op.FileName);
                    pictureBox1.Image = ref_img.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Tplimg_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog op = new OpenFileDialog();
                if (op.ShowDialog() == DialogResult.OK)
                {
                    tpl_img = new Mat(op.FileName);
                    pictureBox2.Image = tpl_img.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Matching_Click(object sender, EventArgs e)
        {
            try
            {
                int maxleval = 3;
                double threshscore = 0.5;
                List<DataPoint> RES = Matching(ref_img, tpl_img, maxleval);
                Mat outputimg = DrawBoundaryResult(ref_img, tpl_img, RES, threshscore);

                pictureBox3.Image = outputimg.ToBitmap();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
