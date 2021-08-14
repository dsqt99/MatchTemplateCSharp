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
        public float angle;
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
        public float angle;
        public Mat tplimgR;
        public Mat masktplR;
    }

    public class MatchingPyramidNCC
    {
        private ValueRange<float> rotationRange;
        private Mat imageTemplate;
        private double threshScore;
        private int levelPyramid;
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

        public Mat ImageTemplate
        {
            get { return imageTemplate; }
            set
            {
                imageTemplate = value;
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

        // Check angle disapearence
        private bool CheckAngleP(List<DataPoint> check, DataPoint dataX)
        {
            foreach (DataPoint data in check)
            {
                if (data.maxval == dataX.maxval)
                {
                    if (data.angle == dataX.angle || data.center == dataX.center) return false;
                }
            }
            return true;
        }

        // Subpixel 
        private PointF Subpixel(Mat result, Point p)
        {
            Matrix<Byte> matrix = new Matrix<Byte>(result.Height, result.Width, result.NumberOfChannels);
            result.CopyTo(matrix);
            float deltaX = 0, deltaY = 0;
            if (p.X > 0 && p.X < result.Width - 1 && (2 * matrix[p.Y, p.X - 1] - 4 * matrix[p.Y, p.X] + 2 * matrix[p.Y, p.X + 1]) != 0)
                deltaX = (matrix[p.Y, p.X - 1] - matrix[p.Y, p.X + 1]) / (2 * matrix[p.Y, p.X - 1] - 4 * matrix[p.Y, p.X] + 2 * matrix[p.Y, p.X + 1]);
            if (p.Y > 0 && p.Y < result.Height - 1 && (2 * matrix[p.Y + 1, p.X] - 4 * matrix[p.Y, p.X] + 2 * matrix[p.Y - 1, p.X]) != 0)
                deltaY = (matrix[p.Y + 1, p.X] - matrix[p.Y - 1, p.X]) / (2 * matrix[p.Y + 1, p.X] - 4 * matrix[p.Y, p.X] + 2 * matrix[p.Y - 1, p.X]);
            PointF res = new PointF(deltaX, deltaY);
            return res;
        }

        // Rotation Image
        private Mat Rotation(Mat image, int method, float angleInDegrees)
        {
            int width = image.Width;
            int height = image.Height;

            Matrix<float> r = new Matrix<float>(2, 3);
            PointF center = new PointF((float)width / 2, (float)height / 2);
            CvInvoke.GetRotationMatrix2D(center, angleInDegrees, 1.0, r);

            float rad = (float) ((Math.PI / 180) * angleInDegrees);
            float width_rotate = (float) (Math.Abs(width * Math.Cos(rad)) + Math.Abs(height * Math.Sin(rad)));
            float height_rotate = (float) (Math.Abs(width * Math.Sin(rad)) + Math.Abs(height * Math.Cos(rad)));
            r[0, 2] += (width_rotate - width) / 2;
            r[1, 2] += (height_rotate - height) / 2;

            Mat img_rotate = new Mat();

            if (method == 0)
                CvInvoke.WarpAffine(image, img_rotate, r, new Size((int)width_rotate, (int)height_rotate), Inter.Nearest, Warp.Default, BorderType.Constant, new MCvScalar(0d, 0d, 0d, 0d));
            else
                CvInvoke.WarpAffine(image, img_rotate, r, new Size((int)width_rotate, (int)height_rotate), Inter.Linear, Warp.Default, BorderType.Constant, new MCvScalar(0d, 0d, 0d, 0d));
            return img_rotate;
        }

        // Reangle box template
        private void RecangleX(Mat outimg, int tW, int tH, DataPoint data)
        {
            float angle = data.angle;
            Point pt = data.topleft;
            Point center = data.center;
            float anglerad = (float)(angle * Math.PI / 180);
            var point1 = new Point(-imageTemplate.Width / 2, -imageTemplate.Height / 2);
            var point2 = new Point(imageTemplate.Width / 2, -imageTemplate.Height / 2);
            var point3 = new Point(imageTemplate.Width / 2, imageTemplate.Height / 2);
            var point4 = new Point(-imageTemplate.Width / 2, imageTemplate.Height / 2);
            point1 = new Point(Convert.ToInt32(point1.X * Math.Cos(2 * Math.PI - anglerad) - point1.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point1.X * Math.Sin(2 * Math.PI - anglerad) + point1.Y * Math.Cos(2 * Math.PI - anglerad)));
            point2 = new Point(Convert.ToInt32(point2.X * Math.Cos(2 * Math.PI - anglerad) - point2.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point2.X * Math.Sin(2 * Math.PI - anglerad) + point2.Y * Math.Cos(2 * Math.PI - anglerad)));
            point3 = new Point(Convert.ToInt32(point3.X * Math.Cos(2 * Math.PI - anglerad) - point3.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point3.X * Math.Sin(2 * Math.PI - anglerad) + point3.Y * Math.Cos(2 * Math.PI - anglerad)));
            point4 = new Point(Convert.ToInt32(point4.X * Math.Cos(2 * Math.PI - anglerad) - point4.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point4.X * Math.Sin(2 * Math.PI - anglerad) + point4.Y * Math.Cos(2 * Math.PI - anglerad)));
            point1 = new Point(point1.X + pt.X + tW / 2, point1.Y + pt.Y + tH / 2);
            point2 = new Point(point2.X + pt.X + tW / 2, point2.Y + pt.Y + tH / 2);
            point3 = new Point(point3.X + pt.X + tW / 2, point3.Y + pt.Y + tH / 2);
            point4 = new Point(point4.X + pt.X + tW / 2, point4.Y + pt.Y + tH / 2);

            string Text = "(" + Convert.ToString(center.X) + ", " + Convert.ToString(center.Y) + "), " + Convert.ToString(angle);
            CvInvoke.PutText(outimg, Text, point1, FontFace.HersheySimplex, 1.0, new Bgr(Color.Red).MCvScalar);

            CvInvoke.Line(outimg, new Point(center.X - 10, center.Y), new Point(center.X + 10, center.Y), new Bgr(Color.Red).MCvScalar, 2);
            CvInvoke.Line(outimg, new Point(center.X, center.Y - 10), new Point(center.X, center.Y + 10), new Bgr(Color.Red).MCvScalar, 2);

            CvInvoke.Line(outimg, point1, point2, new Bgr(Color.Red).MCvScalar, 2);
            CvInvoke.Line(outimg, point2, point3, new Bgr(Color.Red).MCvScalar, 2);
            CvInvoke.Line(outimg, point3, point4, new Bgr(Color.Red).MCvScalar, 2);
            CvInvoke.Line(outimg, point4, point1, new Bgr(Color.Red).MCvScalar, 2);
        }

        // Built list Rotation
        private List<List<DataRotation>> ListRotation()
        {
            List<List<DataRotation>> rotations = new List<List<DataRotation>>();
            VectorOfMat tplimgs = new VectorOfMat();
            double res = 0;

            CvInvoke.BuildPyramid(imageTemplate, tplimgs, levelPyramid);

            for (int i = levelPyramid; i >= 0; i--)
            {
                List<DataRotation> rotated = new List<DataRotation>();
                Mat tplimgr = new Mat();
                Mat masktplr = new Mat();

                Mat tplimg = tplimgs[i];
                int Wt = tplimg.Width; int Ht = tplimg.Height;
                Mat masktpl = Mat.Ones(Ht, Wt, DepthType.Cv8U, 1) * 255;
                float step = (float) (Math.Sqrt(2) / (Math.Sqrt(Math.Pow(Ht, 2) + Math.Pow(Wt, 2)) * Math.PI) * 360);
                rotated.Add(new DataRotation() { angle = 0, tplimgR = tplimg, masktplR = masktpl });

                for (float angle = rotationRange.LowerValue; angle < rotationRange.UpperValue; angle += step)
                {
                    if (angle == 0) continue;
                    if (i == levelPyramid || i == 0)
                    {
                        tplimgr = Rotation(tplimg, 1, angle);
                        masktplr = Rotation(masktpl, 0, angle);
                        res += (tplimgr.Width*tplimgr.Height) * 2;
                    }
                    DataRotation ans = new DataRotation
                    {
                        angle = angle,
                        tplimgR = tplimgr,
                        masktplR = masktplr
                    };
                    if (res > 2147483648)
                    {
                        rotations.Add(rotated);
                        rotations.Reverse();
                        return rotations;
                    }
                    rotated.Add(ans);
                }
                rotations.Add(rotated);
            }
            rotations.Reverse();
            return rotations;
        }

        // Reduce Noise Point
        private List<DataPoint> ReduceNoise(List<DataPoint> ANS, int w, int h, float step)
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
                    float disc = (float) (Math.Sqrt(Math.Pow(centerT.X - center.X, 2) + Math.Pow(centerT.Y - center.Y, 2)));
                    if (disc < distance)
                    {
                        if (maxval < maxvalT)
                        {
                            check = true;
                            break;
                        }
                    }
                }
                if (check == false && CheckAngleP(RES, ANS[i]))
                {
                    RES.Add(ANS[i]);
                }
            }
            return RES;
        }

        // Comparation
        private List<DataPoint> Comparation(Mat src_refimg, int level, List<DataPoint> ANS)
        {
            List<DataPoint> RES = new List<DataPoint>();
            VectorOfMat refimgs = new VectorOfMat();
            List<DataRotation> listSample = listSamples[level];

            CvInvoke.BuildPyramid(src_refimg, refimgs, levelPyramid);

            Mat refimg = refimgs[level];
            Mat tplimg = listSample[0].tplimgR;

            Mat masktpl = Mat.Ones(tplimg.Height, tplimg.Width, Emgu.CV.CvEnum.DepthType.Cv8U, 1) * 255;

            int W = refimg.Width; int H = refimg.Height;
            int Wt = tplimg.Width; int Ht = tplimg.Height;

            float step = (float) (Math.Sqrt(2) / (Math.Sqrt(Math.Pow(Wt, 2) + Math.Pow(Ht, 2)) * Math.PI) * 360);

            if (level == levelPyramid)
            {
                for (int i = 0; i < listSample.Count; i ++)
                {
                    DataRotation dataC = listSample[i];
                    Mat tplimg_new = dataC.tplimgR;
                    Mat masktpl_new = dataC.masktplR;
                    int tW = tplimg_new.Width; int tH = tplimg_new.Height;

                    Mat result = new Mat();

                    CvInvoke.MatchTemplate(refimg, tplimg_new, result, Emgu.CV.CvEnum.TemplateMatchingType.CcorrNormed, masktpl_new);

                    double minval = 0.0, maxval = 0.0;
                    Point minloc = new Point(), maxloc = new Point();
                    CvInvoke.MinMaxLoc(result, ref minval, ref maxval, ref minloc, ref maxloc);

                    if (maxval >= threshScore)
                    {
                        Mat threshed = new Mat(), threshed8u = new Mat();

                        CvInvoke.Threshold(result, threshed, threshScore, 1, Emgu.CV.CvEnum.ThresholdType.ToZero);
                        //CvInvoke.Imshow("res", threshed);
                        //CvInvoke.WaitKey(0);
                        CvInvoke.InRange(threshed, new ScalarArray(threshScore), new ScalarArray(1), threshed8u);

                        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();

                        CvInvoke.FindContours(threshed8u, contours, null, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                        for (int j = 0; j < contours.Size; j++)
                        {
                            double min_val = 0.0, max_val = 0.0;
                            Point min_loc = new Point(), max_loc = new Point();

                            Rectangle r = CvInvoke.BoundingRectangle(contours[j]);
                            Mat roi = new Mat(result, r);
                            CvInvoke.MinMaxLoc(roi, ref min_val, ref max_val, ref min_loc, ref max_loc);
                            PointF delta = Subpixel(roi, max_loc);
                            Point p = new Point((int)(max_loc.X + r.X + delta.X), (int)(max_loc.Y + r.Y + delta.Y));
                            Point center = new Point(p.X + tW / 2, p.Y + tH / 2);
                            DataPoint ans = new DataPoint
                            {
                                angle = dataC.angle,
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
                    float goc = data.angle;
                    Point loc = new Point(data.topleft.X * 2, data.topleft.Y * 2);
                    float start = goc - step * 2 > RotationRange.LowerValue ? goc - step * 2 : RotationRange.LowerValue;
                    float end = goc + step * 2 < RotationRange.UpperValue ? goc + step * 2 : RotationRange.UpperValue;

                    for (float angle = start; angle < end; angle += step)
                    {
                        if (level > 0)
                        {
                            tplimg_new = Rotation(tplimg, 1, angle);
                            masktpl_new = Rotation(masktpl, 0, angle);
                        }
                        else
                        {
                            int idx = (int)(angle / step);
                            if (idx >= listSample.Count)
                            {
                                tplimg_new = Rotation(tplimg, 1, angle);
                                masktpl_new = Rotation(masktpl, 0, angle);
                            }
                            else
                            {
                                tplimg_new = listSample[idx].tplimgR;
                                masktpl_new = listSample[idx].masktplR;
                            }
                        }

                        int tW = tplimg_new.Width; int tH = tplimg_new.Height;

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
                        PointF delta = Subpixel(roi, max_loc);
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
            //foreach (DataPoint data in RES)
            //{
            //    data.Printf();
            //}
            //Console.WriteLine("----");
            return ReduceNoise(RES, Wt, Ht, step);
        }

        public void SetTemplate()
        {
            if (listSamples != null)
            {
                foreach (var item in listSamples)
                {
                    foreach (var data in item)
                    {
                        if (data != null)
                        {
                            if (data.tplimgR != null)
                            {
                                if (data.tplimgR.Ptr != IntPtr.Zero)
                                {
                                    data.tplimgR.Dispose();
                                }
                            }
                            if (data.masktplR != null)
                            {
                                if (data.masktplR.Ptr != IntPtr.Zero)
                                {
                                    data.masktplR.Dispose();
                                }
                            }
                        }
                    }
                }
            }
            listSamples = ListRotation();
        }

        public List<DataPoint> Matching(Mat src_refimg)
        {
            List<DataPoint> ANS = new List<DataPoint>();

            // Process each level
            for (int level = levelPyramid; level >= 0; level--)
            {
                // Time levels
                // Console.WriteLine($"Level: {level}");
                ANS = Comparation(src_refimg, level, ANS);
            }
            return ANS;
        }

        public Mat DrawBoundaryResult(Mat src_refimg, List<DataPoint> RES)
        {
            Mat outimg = src_refimg.Clone();

            foreach (DataPoint data in RES)
            {
                if (data.maxval >= 0.9)
                {
                    data.Printf();
                    Mat tplimg_new = Rotation(imageTemplate, 1, data.angle);
                    int tW = tplimg_new.Width; int tH = tplimg_new.Height;
                    RecangleX(outimg, tW, tH, data);
                }
            }
            return outimg;
        }
    }
}