using System;
using System.Diagnostics;
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
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using ST4I.Vision.Core;

namespace Matching
{
    public class DataPoint
    {
        public double angle;
        public Point topleft;
        public Point center;
        public double maxval;

        public void Printf()
        {
            Console.WriteLine($"angle, topleft, center, maxval: {angle} {topleft} {center} {maxval}");
        }
    }

    public class DataRotation
    {
        public double angle;
        public Mat tplimgR;
        public Mat masktplR;
    }

    public class PointD
    {
        public double X;
        public double Y;
        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
    public class MatchingPyramidNCC
    {
        private ValueRange<float> rotationRange = new ValueRange<float> { Minimum = 0, Maximum = 360, LowerValue = 0, UpperValue = 0 };
        private Mat imageTemplate;
        private double threshScore;
        private uint levelPyramid = 0;
        private List<List<DataRotation>> listSamples = new List<List<DataRotation>>();

        /// <summary>
        /// Rotation range in degree
        /// </summary>
        public ValueRange<float> RotationRange
        {
            get
            {
                return rotationRange;
            }
            set
            {
                rotationRange = value;
            }
        }

        public uint LevelPyramid
        {
            get
            {
                return levelPyramid;
            }
            set
            {
                levelPyramid = value;
            }
        }

        public double ThreshScore
        {
            get { return threshScore; }
            set
            {
                threshScore = value;
            }
        }

        /// <summary>
        /// Set template
        /// </summary>
        /// <param name="img"></param>
        /// <param name="threshScore"></param>
        public void SetTemplate(Mat img, float threshScore)
        {
            SetTemplate(img, threshScore, 0, new float[2] { 0, 0 });
        }

        /// <summary>
        /// Setup template, support pyramid and rotation
        /// </summary>
        /// <param name="img"></param>
        /// <param name="levelPyramid"></param>
        /// <param name="rotationRange"></param>
        public void SetTemplate(Mat img, float threshScore, uint levelPyramid, float[] rotationRange)
        {
            imageTemplate = img;
            LevelPyramid = levelPyramid;
            if (rotationRange.Length >= 1)
            {
                RotationRange.LowerValue = rotationRange[0];
                RotationRange.UpperValue = rotationRange[1];
            }
            //listSamples = ListRotation(img, levelPyramid);
        }
        /// <summary>
        /// Matching with pyramid
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public List<DataPoint> Matching(Mat input)
        {
            //List<DataPoint> RES = FastTemplateMatchPyramid(refimg, imageTemplate, listSamples, LevelPyramid);

            return new List<DataPoint>();
        }

        public Mat DrawBoundaryResult(Mat img)
        {
            // Base on detection points => draw result
            return null;
        }

    }

}