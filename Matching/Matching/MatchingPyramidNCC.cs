﻿using System;
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

    public struct PointD
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
        private ValueRange<double> rotationRange = new ValueRange<double> { Minimum = 0, Maximum = 360, LowerValue = 0, UpperValue = 0 };
        private Mat imageTemplate;
        private double threshScore;
        private int levelPyramid = 4;
        private List<List<DataRotation>> listSamples = new List<List<DataRotation>>();

        /// <summary>
        /// Rotation range in degree
        /// </summary>
        public ValueRange<double> RotationRange
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

        public int LevelPyramid
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

        // Check angle disapearence
        private DataRotation CheckAngleR(List<DataRotation> check, double angle, double step)
        {
            foreach (DataRotation data in check)
            {
                if (Math.Abs(data.angle - angle) < step) return data;
            }
            DataRotation ans = new DataRotation { angle = -1 };
            return ans;
        }

        // Check angle disapearence
        private DataPoint CheckAngleP(List<DataPoint> check, double angle, double step)
        {
            foreach (DataPoint data in check)
            {
                if (Math.Abs(data.angle - angle) < step) return data;
            }
            DataPoint ans = new DataPoint { angle = -1 };
            return ans;
        }

        // Subpixel 
        private PointD Subpixel(Mat result, Point p)
        {
            Matrix<Byte> matrix = new Matrix<Byte>(result.Rows, result.Cols, result.NumberOfChannels);
            result.CopyTo(matrix);
            double deltaX = 0, deltaY = 0;
            if (p.X > 0 && p.X < result.Cols - 1 && (2 * matrix[p.Y, p.X - 1] - 4 * matrix[p.Y, p.X] + 2 * matrix[p.Y, p.X + 1]) != 0)
                deltaX = (matrix[p.Y, p.X - 1] - matrix[p.Y, p.X + 1]) / (2 * matrix[p.Y, p.X - 1] - 4 * matrix[p.Y, p.X] + 2 * matrix[p.Y, p.X + 1]);
            if (p.Y > 0 && p.Y < result.Rows - 1 && (2 * matrix[p.Y + 1, p.X] - 4 * matrix[p.Y, p.X] + 2 * matrix[p.Y - 1, p.X]) != 0)
                deltaY = (matrix[p.Y + 1, p.X] - matrix[p.Y - 1, p.X]) / (2 * matrix[p.Y + 1, p.X] - 4 * matrix[p.Y, p.X] + 2 * matrix[p.Y - 1, p.X]);
            PointD res = new PointD(deltaX, deltaY);
            return res;
        }

        // Rotation Image
        private Mat Rotation(Mat image, double angleInDegrees)
        {
            int width = image.Width;
            int height = image.Height;

            Matrix<double> r = new Matrix<double>(2, 3);
            PointF center = new PointF((float)width / 2, (float)height / 2);
            CvInvoke.GetRotationMatrix2D(center, angleInDegrees, 1.0, r);

            double rad = (Math.PI / 180) * angleInDegrees;
            double width_rotate = Math.Abs(width * Math.Cos(rad)) + Math.Abs(height * Math.Sin(rad));
            double height_rotate = Math.Abs(width * Math.Sin(rad)) + Math.Abs(height * Math.Cos(rad));
            r[0, 2] += (width_rotate - width) / 2;
            r[1, 2] += (height_rotate - height) / 2;

            Mat img_rotate = new Mat();
            CvInvoke.WarpAffine(image, img_rotate, r, new Size((int)width_rotate, (int)height_rotate), Inter.Linear, Warp.Default, BorderType.Constant, new MCvScalar(0d, 0d, 0d, 0d));
            return img_rotate;
        }

        // Reangle box template
        private void RecangleX(Mat outimg, int W, int H, int Wt, int Ht, DataPoint data)
        {
            double angle = data.angle;
            Point pt = data.topleft;
            Point center = data.center;
            double anglerad = (double)(angle * Math.PI / 180);
            var point1 = new Point(-W / 2, -H / 2);
            var point2 = new Point(W / 2, -H / 2);
            var point3 = new Point(W / 2, H / 2);
            var point4 = new Point(-W / 2, H / 2);
            point1 = new Point(Convert.ToInt32(point1.X * Math.Cos(2 * Math.PI - anglerad) - point1.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point1.X * Math.Sin(2 * Math.PI - anglerad) + point1.Y * Math.Cos(2 * Math.PI - anglerad)));
            point2 = new Point(Convert.ToInt32(point2.X * Math.Cos(2 * Math.PI - anglerad) - point2.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point2.X * Math.Sin(2 * Math.PI - anglerad) + point2.Y * Math.Cos(2 * Math.PI - anglerad)));
            point3 = new Point(Convert.ToInt32(point3.X * Math.Cos(2 * Math.PI - anglerad) - point3.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point3.X * Math.Sin(2 * Math.PI - anglerad) + point3.Y * Math.Cos(2 * Math.PI - anglerad)));
            point4 = new Point(Convert.ToInt32(point4.X * Math.Cos(2 * Math.PI - anglerad) - point4.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point4.X * Math.Sin(2 * Math.PI - anglerad) + point4.Y * Math.Cos(2 * Math.PI - anglerad)));
            point1 = new Point(point1.X + pt.X + Wt / 2, point1.Y + pt.Y + Ht / 2);
            point2 = new Point(point2.X + pt.X + Wt / 2, point2.Y + pt.Y + Ht / 2);
            point3 = new Point(point3.X + pt.X + Wt / 2, point3.Y + pt.Y + Ht / 2);
            point4 = new Point(point4.X + pt.X + Wt / 2, point4.Y + pt.Y + Ht / 2);

            string Text = "(" + Convert.ToString(center.X) + ", " + Convert.ToString(center.Y) + "), " + Convert.ToString(angle);
            CvInvoke.PutText(outimg, Text, point1, FontFace.HersheySimplex, 1.0, new Bgr(Color.Red).MCvScalar);

            CvInvoke.Line(outimg, point1, point2, new Bgr(Color.Red).MCvScalar, 2);
            CvInvoke.Line(outimg, point2, point3, new Bgr(Color.Red).MCvScalar, 2);
            CvInvoke.Line(outimg, point3, point4, new Bgr(Color.Red).MCvScalar, 2);
            CvInvoke.Line(outimg, point4, point1, new Bgr(Color.Red).MCvScalar, 2);
        }

        // Built list Rotation
        private List<List<DataRotation>> ListRotation(Mat src_tplimg, int maxlevel, ValueRange<double> rotationRange)
        {
            List<List<DataRotation>> rotations = new List<List<DataRotation>>();
            List<DataRotation> rotated = new List<DataRotation>();
            double step = 0;
            VectorOfMat tplimgs = new VectorOfMat();
            CvInvoke.BuildPyramid(src_tplimg, tplimgs, maxlevel);
            for (int i = maxlevel; i >= 0; i--)
            {
                Mat tplimgr = new Mat();
                Mat masktplr = new Mat();
                double step_save = step;
                Mat tplimg = tplimgs[i];
                int Wt = tplimg.Cols; int Ht = tplimg.Rows;
                Mat masktpl = Mat.Ones(Ht, Wt, DepthType.Cv8U, 1) * 255;
                step = Math.Sqrt(2) / (Math.Sqrt(Math.Pow(Ht, 2) + Math.Pow(Wt, 2)) * Math.PI) * 360;
                if (i == maxlevel)
                {
                    for (double angle = rotationRange.LowerValue; angle < rotationRange.UpperValue; angle += step)
                    {
                        DataRotation ans = new DataRotation
                        {
                            angle = angle,
                            tplimgR = tplimgr,
                            masktplR = masktplr,
                        };
                        rotated.Add(ans);
                    }
                }
                else
                {
                    double k = step_save;
                    List<DataRotation> rotated_new = new List<DataRotation>();
                    foreach (DataRotation data in rotated)
                    {
                        double goc = data.angle;
                        double start = (goc - k > 0) ? goc - k : 0;
                        double end = (goc + k < 360) ? goc + k : 360;
                        for (double angle = start; angle < end; angle += step)
                        {
                            if (angle == 360) angle = 0;
                            if (CheckAngleR(rotated_new, angle, step).angle == -1)
                            {
                                if (i == 0)
                                {
                                    tplimgr = Rotation(tplimg, angle);
                                    masktplr = Rotation(masktpl, angle);
                                }
                                DataRotation ans = new DataRotation
                                {
                                    angle = angle,
                                    tplimgR = tplimgr,
                                    masktplR = masktplr
                                };
                                rotated_new.Add(ans);
                                //count += 1;
                            }

                        }
                    }
                    rotated = new List<DataRotation>(rotated_new);
                }
                rotations.Add(rotated);
            }
            rotations.Reverse();
            Console.WriteLine("Done build Rotation !!!");
            return rotations;
        }

        // Reduce Noise Point
        private List<DataPoint> ReduceNoise(List<DataPoint> ANS, int w, int h, double step)
        {
            List<DataPoint> RES = new List<DataPoint>();
            int l = ANS.Count;
            int distance = Math.Min(w, h);
            for (int i = 0; i < l; ++i)
            {
                double maxval = ANS[i].maxval;
                Point center = ANS[i].center;
                bool check = false;
                for (int j = 0; j < l; ++j)
                {
                    if (j == i) continue;
                    double maxvalT = ANS[j].maxval;
                    Point centerT = ANS[j].center;
                    double disc = Math.Sqrt(Math.Pow(centerT.X - center.X, 2) + Math.Pow(centerT.Y - center.Y, 2));
                    if (disc < distance)
                    {
                        if (maxval < maxvalT)
                        {
                            check = true;
                            break;
                        }
                    }
                }
                if (check == false && CheckAngleP(RES, ANS[i].angle, step).angle == -1)
                {
                    RES.Add(ANS[i]);
                }
            }
            return RES;
        }

        // Comparation
        private List<DataPoint> Comparation(Mat refimg, Mat tplimg, Mat masktpl, int level, int maxlevel, double threshold, double step, double step_save,
                                        List<DataPoint> ANS, List<List<DataRotation>> listSamples)
        {
            List<DataPoint> RES = new List<DataPoint>();
            int W = refimg.Cols; int H = refimg.Rows;

            if (level == maxlevel)
            {
                for (double angle = 0; angle < 360; angle += step)
                {
                    Mat tplimg_new = Rotation(tplimg, angle);
                    Mat masktpl_new = Rotation(masktpl, angle);
                    int tW = tplimg_new.Cols; int tH = tplimg_new.Rows;

                    Mat result = new Mat();

                    CvInvoke.MatchTemplate(refimg, tplimg_new, result, Emgu.CV.CvEnum.TemplateMatchingType.CcorrNormed, masktpl_new);

                    double minval = 0.0, maxval = 0.0;
                    Point minloc = new Point(), maxloc = new Point();
                    CvInvoke.MinMaxLoc(result, ref minval, ref maxval, ref minloc, ref maxloc);

                    if (maxval > threshold)
                    {
                        Mat threshed = new Mat(), threshed8u = new Mat();

                        CvInvoke.Threshold(result, threshed, threshold, 1, Emgu.CV.CvEnum.ThresholdType.ToZero);
                        CvInvoke.InRange(threshed, new ScalarArray(threshold), new ScalarArray(1), threshed8u);

                        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                        Mat m = new Mat();

                        CvInvoke.FindContours(threshed8u, contours, m, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                        for (int i = 0; i < contours.Size; i++)
                        {
                            double min_val = 0.0, max_val = 0.0;
                            Point min_loc = new Point(), max_loc = new Point();

                            Rectangle r = CvInvoke.BoundingRectangle(contours[i]);
                            Mat roi = new Mat(result, r);
                            CvInvoke.MinMaxLoc(roi, ref min_val, ref max_val, ref min_loc, ref max_loc);
                            PointD delta = Subpixel(roi, max_loc);
                            Point p = new Point((int)(max_loc.X + r.X + delta.X), (int)(max_loc.Y + r.Y + delta.Y));
                            Point center = new Point(p.X + tW / 2, p.Y + tH / 2);
                            DataPoint ans = new DataPoint
                            {
                                angle = angle,
                                topleft = p,
                                center = center,
                                maxval = max_val
                            };
                            RES.Add(ans);
                        }
                    }
                }
            }
            else
            {
                foreach (DataPoint data in ANS)
                {
                    Mat tplimg_new, masktpl_new;
                    double goc = data.angle;
                    Point loc = new Point(data.topleft.X * 2, data.topleft.Y * 2);
                    double start = goc - step_save > 0 ? goc - step_save : 0;
                    double end = goc + step_save < 360 ? goc + step_save : 360;

                    for (double angle = start; angle < end; angle += step)
                    {
                        if (level >= 1)
                        {
                            tplimg_new = Rotation(tplimg, angle);
                            masktpl_new = Rotation(masktpl, angle);
                        }
                        else
                        {
                            List<DataRotation> rotation_ = listSamples[level];
                            DataRotation dataC = CheckAngleR(rotation_, angle, step);
                            if (dataC.angle == -1)
                            {
                                tplimg_new = Rotation(tplimg, angle);
                                masktpl_new = Rotation(masktpl, angle);
                            }
                            else
                            {
                                tplimg_new = dataC.tplimgR;
                                masktpl_new = dataC.masktplR;
                            }
                        }

                        int tW = tplimg_new.Cols; int tH = tplimg_new.Rows;

                        int x = loc.X - 3 > 0 ? loc.X - 3 : 0;
                        int y = loc.Y - 3 > 0 ? loc.Y - 3 : 0;

                        Mat roi = new Mat();

                        if (x + tW + 6 <= W && y + tH + 6 <= H)
                        {
                            CvInvoke.MatchTemplate(
                                new Mat(refimg, new Rectangle(x, y, tW + 6, tH + 6)),
                                tplimg_new,
                                roi,
                                Emgu.CV.CvEnum.TemplateMatchingType.CcorrNormed,
                                masktpl_new
                            );
                        }

                        double min_val = 0.0, max_val = 0.0;
                        Point min_loc = new Point(), max_loc = new Point();
                        CvInvoke.MinMaxLoc(roi, ref min_val, ref max_val, ref min_loc, ref max_loc);
                        PointD delta = Subpixel(roi, max_loc);
                        Point p = new Point((int)(max_loc.X + x + delta.X), (int)(max_loc.Y + y + delta.Y));
                        Point center = new Point(p.X + tW / 2, p.Y + tH / 2);
                        DataPoint ans = new DataPoint
                        {
                            angle = angle,
                            topleft = p,
                            center = center,
                            maxval = max_val
                        };
                        RES.Add(ans);
                    }
                }
            }
            return RES;
        }

        public void SetTemplate(Mat img, double threshScore)
        {
            SetTemplate(img, threshScore, levelPyramid, new double[2] { 0, 360 });
        }

        /// <summary>
        /// Setup template, support pyramid and rotation
        /// </summary>
        /// <param name="img"></param>
        /// <param name="levelPyramid"></param>
        /// <param name="rotationRange"></param>
        public void SetTemplate(Mat img, double threshScore, int levelPyramid, double[] rotationRange)
        {
            imageTemplate = img;
            LevelPyramid = levelPyramid;
            if (rotationRange.Length >= 1)
            {
                RotationRange.LowerValue = rotationRange[0];
                RotationRange.UpperValue = rotationRange[1];
            }
            listSamples = ListRotation(img, levelPyramid, RotationRange);
        }
        /// <summary>
        /// Matching with pyramid
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public List<DataPoint> Matching(Mat src_refimg, Mat src_tplimg, int maxlevel)
        {
            VectorOfMat refimgs = new VectorOfMat();
            VectorOfMat tplimgs = new VectorOfMat();

            Mat refimg, tplimg, masktpl;
            List<DataPoint> ANS = new List<DataPoint>();
            SetTemplate(src_tplimg, threshScore);

            // Build Gaussian pyramid
            CvInvoke.BuildPyramid(src_refimg, refimgs, maxlevel);
            CvInvoke.BuildPyramid(src_tplimg, tplimgs, maxlevel);

            double step = 0.0f;
            double step_save;

            // Process each levelstd::
            for (int level = maxlevel; level >= 0; level--)
            {
                refimg = refimgs[level];
                tplimg = tplimgs[level];
                masktpl = Mat.Ones(tplimg.Rows, tplimg.Cols, Emgu.CV.CvEnum.DepthType.Cv8U, 1) * 255;

                int Wt = tplimg.Cols; int Ht = tplimg.Rows;
                step_save = step;
                step = Math.Sqrt(2) / (Math.Sqrt(Math.Pow(Wt, 2) + Math.Pow(Ht, 2)) * Math.PI) * 360;

                // Time levels
                ANS = Comparation(refimg, tplimg, masktpl, level, maxlevel, threshScore, step, step_save, ANS, listSamples);
                ANS = ReduceNoise(ANS, Wt, Ht, step);
            }
            return ANS;
        }

        public Mat DrawBoundaryResult(Mat refimg, Mat tplimg, int maxlevel, double threshscore)
        {
            List<DataPoint> RES;

            Mat outimg = refimg.Clone();
            int Wt = tplimg.Cols; int Ht = tplimg.Rows;

            // Matching
            RES = Matching(refimg, tplimg, maxlevel);

            foreach (DataPoint data in RES)
            {
                if (data.maxval > threshscore)
                {
                    data.Printf();
                    Mat tplimg_new = Rotation(tplimg, data.angle);
                    int tW = tplimg_new.Cols; int tH = tplimg_new.Rows;
                    RecangleX(outimg, Wt, Ht, tW, tH, data);
                }
            }
            return outimg;
        }

    }

}