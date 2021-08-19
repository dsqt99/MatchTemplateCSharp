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
        MatchingPyramidNCC RUN = new MatchingPyramidNCC();

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

        private void Save_Parameter_Click(object sender, EventArgs e)
        {
            RUN.RotationRange.LowerValue = (float)nudRotationMin.Value;
            RUN.RotationRange.UpperValue = (float)nudRotationMax.Value;
            RUN.ThreshScore = (float)nudThreshScore.Value;
            RUN.LevelPyramid = (int)nudPyramidLevel.Value;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            RUN.SetTemplate();

            stopWatch.Stop();
            Console.WriteLine($"Setup template: {stopWatch.ElapsedMilliseconds} ms");
        }

        private void nudThreshScore_ValueChanged(object sender, EventArgs e)
        {
            RUN.ThreshScore = (float)nudThreshScore.Value;
        }

        private void Tplimg_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog op = new OpenFileDialog();
                if (op.ShowDialog() == DialogResult.OK)
                {
                    RUN.ImageTemplate = new Mat(op.FileName, ImreadModes.Grayscale);
                    imbTemplate.Image = RUN.ImageTemplate.Clone();
                }
                Console.WriteLine($"Template Shape: {RUN.ImageTemplate.Size}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Matching_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 1; i++)
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                var RES = RUN.Matching(ref_img);
                for (int n = 0; n < RES.Length; n++)
                {
                    Console.WriteLine($"Object {n + 1}: Score: {RES[n].Score}, TopLeft: {RES[n].TopLeft}, Angle: {RES[n].Angle}");
                }
                stopWatch.Stop();
                Console.WriteLine($"[Times {i + 1}], Elapsed Matching: {stopWatch.ElapsedMilliseconds} ms");
                Mat outputimg = RUN.DrawResult(ref_imgRGB, RES, new MCvScalar(0, 0, 255), 2);
                imbResult.Image = outputimg.Clone();
            }
        }
    }
}