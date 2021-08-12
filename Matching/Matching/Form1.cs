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
using ST4I.Vision.Core;


namespace Matching
{
    public partial class Form1 : Form
    {
        Mat ref_img, ref_imgRGB;
        readonly MatchingPyramidNCC RUN = new MatchingPyramidNCC
        {
            ThreshScore = 0.5,
            LevelPyramid = 3
        };

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
                    ref_imgRGB = new Mat(op.FileName);
                    ref_img = new Mat(op.FileName, ImreadModes.Grayscale);
                    Console.WriteLine($"Input Shape: {ref_img.Size}");
                    imbInput.Image = ref_img.Clone();
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
                    RUN.imageTemplate = new Mat(op.FileName, ImreadModes.Grayscale);
                    imbTemplate.Image = RUN.imageTemplate.Clone();
                }
                Console.WriteLine($"Template Shape: {RUN.imageTemplate.Size}");

                DateTime dtS = DateTime.Now;
                RUN.SetTemplate(new double[2] { 0, 360 });
                TimeSpan tsS = DateTime.Now - dtS;
                Console.WriteLine($"Time set template: {tsS}");
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
                DateTime dtM = DateTime.Now;

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                List<DataPoint> RES = RUN.Matching(ref_img);
                stopWatch.Stop();

                TimeSpan tsM = DateTime.Now - dtM;
                Console.WriteLine($"Time Matching: {stopWatch.ElapsedMilliseconds} ms");
                
                Mat outputimg = RUN.DrawBoundaryResult(ref_imgRGB, RES);
                
                Console.WriteLine("-----------");
                imbResult.Image = outputimg.Clone();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}