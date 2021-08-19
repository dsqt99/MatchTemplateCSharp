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
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using ST4I.Vision.Core;

namespace Matching
{
    public class DataPoint
    {
        /// <summary>
        /// Angle in degree
        /// </summary>
        public float Angle;
        /// <summary>
        /// Top left point of object
        /// </summary>
        public Point TopLeft;
        /// <summary>
        /// Center point of object
        /// </summary>
        public Point Center;
        /// <summary>
        /// Caculated score
        /// </summary>
        public double Score;
    }

    public class DataRotation
    {
        /// <summary>
        /// Angle in degree
        /// </summary>
        public float Angle;
        /// <summary>
        /// Template image
        /// </summary>
        public Mat Template;
        /// <summary>
        /// Mask of image
        /// </summary>
        public Mat Mask;
    }

    public class MatchingPyramidNCC
    {
        private ValueRange<float> rotationRange = new ValueRange<float>() { Minimum = 0, Maximum = 360 };
        private Mat imageTemplate;
        private float threshScore = 0.8f;
        private int levelPyramid = 1;
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
        /// <summary>
        /// Template image
        /// </summary>
        public Mat ImageTemplate
        {
            get { return imageTemplate; }
            set
            {
                imageTemplate = value;
            }
        }
        /// <summary>
        /// Get / set level pyramid will be used for matching
        /// </summary>
        public int LevelPyramid
        {
            get
            {
                return levelPyramid;
            }
            set
            {
                if (value >= 0)
                {
                    levelPyramid = value;
                }
            }
        }
        /// <summary>
        /// Score for matching, all result must be >= ThreshScore
        /// </summary>
        public float ThreshScore
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
                if (data.Score == dataX.Score)
                {
                    if (data.Angle == dataX.Angle || data.Center == dataX.Center) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Perform higher accuracy of result
        /// </summary>
        /// <param name="result"></param>
        /// <param name="p"></param>
        /// <returns></returns>
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
            matrix.Dispose();
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

            float rad = (float)((Math.PI / 180) * angleInDegrees);
            float width_rotate = (float)(Math.Abs(width * Math.Cos(rad)) + Math.Abs(height * Math.Sin(rad)));
            float height_rotate = (float)(Math.Abs(width * Math.Sin(rad)) + Math.Abs(height * Math.Cos(rad)));
            r[0, 2] += (width_rotate - width) / 2;
            r[1, 2] += (height_rotate - height) / 2;

            Mat img_rotate = new Mat();

            if (method == 0)
                CvInvoke.WarpAffine(image, img_rotate, r, new Size((int)(width_rotate + 0.5), (int)(height_rotate + 0.5)), Inter.Nearest, Warp.Default, BorderType.Constant, new MCvScalar(0d, 0d, 0d));
            else
                CvInvoke.WarpAffine(image, img_rotate, r, new Size((int)(width_rotate + 0.5), (int)(height_rotate + 0.5)), Inter.Linear, Warp.Default, BorderType.Constant, new MCvScalar(0d, 0d, 0d));
            return img_rotate;
        }
        /// <summary>
        /// Calculate size of new image after rotation
        /// </summary>
        /// <param name="img"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        private Size CalculateSizeRotatedImage(Size imgSize, float angle)
        {
            int width = imgSize.Width;
            int height = imgSize.Height;
            float rad = (float)((Math.PI / 180) * angle);
            float width_rotate = (float)(Math.Abs(width * Math.Cos(rad)) + Math.Abs(height * Math.Sin(rad)));
            float height_rotate = (float)(Math.Abs(width * Math.Sin(rad)) + Math.Abs(height * Math.Cos(rad)));
            return new Size((int)(width_rotate + 0.5), (int)(height_rotate + 0.5));
        }
        /// <summary>
        /// Convert a datapoint to vertices
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Point[] ConvertToVertices(DataPoint data, Size imgSize)
        {
            var size = CalculateSizeRotatedImage(imgSize, data.Angle);
            int tW = size.Width, tH = size.Height;
            float angle = data.Angle;
            Point pt = data.TopLeft;
            float anglerad = (float)(angle * Math.PI / 180);
            var point1 = new Point(-imgSize.Width / 2, -imgSize.Height / 2);
            var point2 = new Point(imgSize.Width / 2, -imgSize.Height / 2);
            var point3 = new Point(imgSize.Width / 2, imgSize.Height / 2);
            var point4 = new Point(-imgSize.Width / 2, imgSize.Height / 2);
            point1 = new Point(Convert.ToInt32(point1.X * Math.Cos(2 * Math.PI - anglerad) - point1.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point1.X * Math.Sin(2 * Math.PI - anglerad) + point1.Y * Math.Cos(2 * Math.PI - anglerad)));
            point2 = new Point(Convert.ToInt32(point2.X * Math.Cos(2 * Math.PI - anglerad) - point2.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point2.X * Math.Sin(2 * Math.PI - anglerad) + point2.Y * Math.Cos(2 * Math.PI - anglerad)));
            point3 = new Point(Convert.ToInt32(point3.X * Math.Cos(2 * Math.PI - anglerad) - point3.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point3.X * Math.Sin(2 * Math.PI - anglerad) + point3.Y * Math.Cos(2 * Math.PI - anglerad)));
            point4 = new Point(Convert.ToInt32(point4.X * Math.Cos(2 * Math.PI - anglerad) - point4.Y * Math.Sin(2 * Math.PI - anglerad)), Convert.ToInt32(point4.X * Math.Sin(2 * Math.PI - anglerad) + point4.Y * Math.Cos(2 * Math.PI - anglerad)));
            point1 = new Point(point1.X + pt.X + tW / 2, point1.Y + pt.Y + tH / 2);
            point2 = new Point(point2.X + pt.X + tW / 2, point2.Y + pt.Y + tH / 2);
            point3 = new Point(point3.X + pt.X + tW / 2, point3.Y + pt.Y + tH / 2);
            point4 = new Point(point4.X + pt.X + tW / 2, point4.Y + pt.Y + tH / 2);
            return new Point[4] { point1, point2, point3, point4 };
        }
        /// <summary>
        /// Build samples image base on template
        /// </summary>
        /// <returns></returns>
        private List<List<DataRotation>> BuildTemplateSamples()
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

                Mat tplimg = tplimgs[i].Clone();
                int Wt = tplimg.Width; int Ht = tplimg.Height;
                Mat masktpl = Mat.Ones(Ht, Wt, DepthType.Cv8U, 1);
                masktpl.SetTo(new MCvScalar(255));
                float step = (float)(Math.Sqrt(2) / (Math.Sqrt(Math.Pow(Ht, 2) + Math.Pow(Wt, 2)) * Math.PI) * 360);
                rotated.Add(new DataRotation() { Angle = 0, Template = tplimg, Mask = masktpl });

                for (float angle = rotationRange.LowerValue; angle < rotationRange.UpperValue; angle += step)
                {
                    if (angle == 0) continue;
                    if (i == levelPyramid || i == 0)
                    {
                        tplimgr = Rotation(tplimg, 1, angle);
                        masktplr = Rotation(masktpl, 0, angle);
                        res += (tplimgr.Width * tplimgr.Height) * 2;
                    }
                    DataRotation ans = new DataRotation
                    {
                        Angle = angle,
                        Template = tplimgr,
                        Mask = masktplr
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
            // Dispose
            tplimgs.Dispose();
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
                double maxval = ANS[i].Score;
                Point center = ANS[i].Center;
                bool check = false;
                for (int j = 0; j < l; ++j)
                {
                    if (j == i) continue;
                    double maxvalT = ANS[j].Score;
                    Point centerT = ANS[j].Center;
                    float disc = (float)(Math.Sqrt(Math.Pow(centerT.X - center.X, 2) + Math.Pow(centerT.Y - center.Y, 2)));
                    if (disc < distance)
                    {
                        if (maxval < maxvalT)
                        {
                            check = true;
                            break;
                        }
                    }
                }
                //bool notFound = RES.FindIndex(el => (el.maxval == ANS[i].maxval) && ((el.angle == ANS[i].angle) || (el.center == ANS[i].center))) == -1;
                if (check == false && CheckAngleP(RES, ANS[i]))
                {
                    RES.Add(ANS[i]);
                }
            }
            return RES;
        }

        private List<DataPoint> Prune(List<DataPoint> listData, Size size)
        {
            List<DataPoint> res = new List<DataPoint>();
            if (listData.Count > 0)
            {
                if (listData.Count > 1)
                {
                    List<int> listIndex = new List<int>();
                    double ratio = 0.5;
                    Point p = listData[0].Center;
                    int w = (int)(size.Width * ratio + 0.5);
                    int h = (int)(size.Height * ratio + 0.5);
                    Rectangle r = new Rectangle(p.X - w / 2, p.Y - h / 2, w, h);
                    for (int i = 1; i < listData.Count; i++)
                    {
                        if (r.Contains(listData[i].Center))
                        {
                            listIndex.Add(i);
                        }
                    }

                    res.Add(listData[0]);

                    int k = 1;
                    listData.RemoveAt(0);
                    foreach (var i in listIndex)
                    {
                        listData.RemoveAt(i - k);
                        k++;
                    }
                    res.AddRange(Prune(listData, size));
                }
                else
                {
                    res.Add(listData[0]);
                }
            }
            return res;
        }

        private List<DataPoint> ReduceOverlap(List<DataPoint> ANS, int w, int h, float step)
        {
            List<DataPoint> RES = new List<DataPoint>();
            // 1. Sort
            var sorted = ANS.OrderByDescending(el => el.Score).ToList();
            RES = Prune(sorted, new Size(w, h));
            return RES;
        }

        private List<Rectangle> GetRectFromThreshImage(Mat img, double thresh)
        {
            using (Mat threshed = new Mat())
            using (Mat threshed8u = new Mat())
            {
                CvInvoke.Threshold(img, threshed, thresh, 255, Emgu.CV.CvEnum.ThresholdType.Binary);
                threshed.ConvertTo(threshed8u, DepthType.Cv8U);
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(threshed8u, contours, null, Emgu.CV.CvEnum.RetrType.List, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
                List<Rectangle> listR = new List<Rectangle>();
                for (int j = 0; j < contours.Size; j++)
                {
                    Rectangle r = CvInvoke.BoundingRectangle(contours[j]);
                    listR.Add(r);
                }
                // Dispose
                contours.Dispose();
                return listR;
            }
        }

        public struct CCStatsOp
        {
            public Rectangle Rectangle;
            public int Area;
        }

        private List<Rectangle> GetRectFromThreshImage2(Mat img, double thresh)
        {
            List<Rectangle> listR = new List<Rectangle>();
            using (Mat threshed = new Mat())
            using (Mat threshed8u = new Mat())
            {
                CvInvoke.Threshold(img, threshed, threshScore, 255, ThresholdType.Binary);
                threshed.ConvertTo(threshed8u, DepthType.Cv8U);
                var labels = new Mat();
                var stats = new Mat();
                var centroids = new Mat();
                int nlabels = CvInvoke.ConnectedComponentsWithStats(threshed8u, labels, stats, centroids);
                CCStatsOp[] statsOp = new CCStatsOp[nlabels];
                stats.CopyTo(statsOp);
                foreach (var statop in statsOp)
                {
                    listR.Add(statop.Rectangle);
                }
                labels.Dispose();
                centroids.Dispose();
                stats.Dispose();
                return listR;
            }
        }

        private List<KeyValuePair<Point, double>> PruneArea(Mat roiImg, Size size, Point offset)
        {
            List<KeyValuePair<Point, double>> listPointAndValue = new List<KeyValuePair<Point, double>>();
            double ratio = 0.5;

            double min_val = 0.0, max_val = 0.0;
            Point min_loc = new Point(), max_loc = new Point();
            CvInvoke.MinMaxLoc(roiImg, ref min_val, ref max_val, ref min_loc, ref max_loc);
            if (max_val >= threshScore)
            {
                Point p = new Point((int)(max_loc.X + offset.X + 0.5), (int)(max_loc.Y + offset.Y + 0.5));
                listPointAndValue.Add(new KeyValuePair<Point, double>(p, max_val));

                int w = (int)(size.Width * ratio + 0.5);
                int h = (int)(size.Height * ratio + 0.5);
                int x = max_loc.X - w / 2 > 0 ? max_loc.X - w / 2 : 0;
                int y = max_loc.Y - h / 2 > 0 ? max_loc.Y - h / 2 : 0;
                w = x + w < roiImg.Width ? w : roiImg.Width - x;
                h = y + h < roiImg.Height ? h : roiImg.Height - y;
                Rectangle r = new Rectangle(x, y, w, h);

                Mat roi = new Mat(roiImg, r);
                roi.SetTo(new MCvScalar(0));
                if ((w < roiImg.Width) || (h < roiImg.Height))
                {
                    //var listR = GetRectFromThreshImage(roiImg, threshScore);
                    //foreach (var newR in listR)
                    //{
                    //    Mat _roi = new Mat(roiImg, newR);
                    //    listPointAndValue.AddRange(PruneArea(_roi, size, new Point(offset.X + newR.X, offset.Y + newR.Y)));
                    //}
                    listPointAndValue.AddRange(PruneArea(roiImg, size, offset));
                }
                roi.Dispose();
            }
            return listPointAndValue;
        }

        private List<KeyValuePair<Point, double>> GetPoints(Mat scoreImage, Size size, double threshScore)
        {
            List<KeyValuePair<Point, double>> listPointAndValue = new List<KeyValuePair<Point, double>>();
            var listR = GetRectFromThreshImage(scoreImage, threshScore);
            foreach (var r in listR)
            {
                Mat roi = new Mat(scoreImage, r);
                listPointAndValue.AddRange(PruneArea(roi, size, new Point(r.X, r.Y)));
                roi.Dispose();
            }
            return listPointAndValue;
        }

        Stopwatch st = new Stopwatch();
        long countTime = 0;

        private List<DataPoint> Matching(Mat refimg, int level, List<DataPoint> ANS)
        {
            List<DataPoint> RES = new List<DataPoint>();
            List<DataRotation> listSample = listSamples[level];
            Mat tplimg = listSample[0].Template;
            int W = refimg.Width; int H = refimg.Height;
            int Wt = tplimg.Width; int Ht = tplimg.Height;

            float step = (float)(Math.Sqrt(2) / (Math.Sqrt(Math.Pow(Wt, 2) + Math.Pow(Ht, 2)) * Math.PI) * 360);

            if (level == levelPyramid)
            {
                for (int i = 0; i < listSample.Count; i++)
                {
                    DataRotation dataC = listSample[i];
                    Mat tplimg_new = dataC.Template;
                    Mat masktpl_new = dataC.Mask;
                    int tW = tplimg_new.Width; int tH = tplimg_new.Height;
                    Mat result = new Mat();
                    CvInvoke.MatchTemplate(refimg, tplimg_new, result, TemplateMatchingType.CcorrNormed, masktpl_new);
                    List<DataPoint> _res = new List<DataPoint>();
                    //var pointAndScore = GetAllPoints(result, threshScore);
                    st.Restart();
                    var pointAndScore = GetPoints(result, tplimg_new.Size, threshScore);

                    foreach (var item in pointAndScore)
                    {
                        Point p = item.Key;
                        Point center = new Point(p.X + tW / 2, p.Y + tH / 2);
                        DataPoint ans = new DataPoint
                        {
                            Angle = dataC.Angle,
                            TopLeft = p,
                            Center = center,
                            Score = item.Value
                        };
                        _res.Add(ans);
                    }

                    //var newRes = ReduceOverlap(_res, Wt, Ht, step);
                    RES.AddRange(_res);
                    st.Stop();
                    countTime += st.ElapsedMilliseconds;
                    //double minval = 0.0, maxval = 0.0;
                    //Point minloc = new Point(), maxloc = new Point();
                    //CvInvoke.MinMaxLoc(result, ref minval, ref maxval, ref minloc, ref maxloc);

                    //if (maxval >= threshScore)
                    //{
                    //    using (Mat threshed = new Mat())
                    //    using (Mat threshed8u = new Mat())
                    //    {
                    //        CvInvoke.Threshold(result, threshed, threshScore, 1, Emgu.CV.CvEnum.ThresholdType.ToZero);
                    //        CvInvoke.InRange(threshed, new ScalarArray(threshScore), new ScalarArray(1), threshed8u);
                    //        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                    //        CvInvoke.FindContours(threshed8u, contours, null, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
                    //        //Mat roi; // init roi
                    //        for (int j = 0; j < contours.Size; j++)
                    //        {
                    //            double min_val = 0.0, max_val = 0.0;
                    //            Point min_loc = new Point(), max_loc = new Point();
                    //            Rectangle r = CvInvoke.BoundingRectangle(contours[j]);
                    //            Mat roi = new Mat(result, r);
                    //            CvInvoke.MinMaxLoc(roi, ref min_val, ref max_val, ref min_loc, ref max_loc);
                    //            PointF delta = Subpixel(roi, max_loc);
                    //            Point p = new Point((int)(max_loc.X + r.X + delta.X + 0.5), (int)(max_loc.Y + r.Y + delta.Y + 0.5));
                    //            Point center = new Point(p.X + tW / 2, p.Y + tH / 2);
                    //            DataPoint ans = new DataPoint
                    //            {
                    //                Angle = dataC.Angle,
                    //                TopLeft = p,
                    //                Center = center,
                    //                Score = max_val
                    //            };
                    //            RES.Add(ans);

                    //            roi.Dispose();
                    //        }
                    //        // Dispose
                    //        contours.Dispose();
                    //    }
                    //}

                    result.Dispose();
                }
            }
            else
            {
                Mat masktpl = Mat.Ones(tplimg.Height, tplimg.Width, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
                masktpl.SetTo(new MCvScalar(255));

                foreach (DataPoint data in ANS)
                {
                    float goc = data.Angle;
                    Point loc = new Point(data.TopLeft.X * 2, data.TopLeft.Y * 2);
                    float start = goc - step * 2 > RotationRange.LowerValue ? goc - step * 2 : RotationRange.LowerValue;
                    float end = goc + step * 2 < RotationRange.UpperValue ? goc + step * 2 : RotationRange.UpperValue;
                    for (float angle = start; angle < end; angle += step)
                    {
                        Mat tplimg_new, masktpl_new;
                        bool flagDispose = false;
                        if (level > 0)
                        {
                            tplimg_new = Rotation(tplimg, 1, angle);
                            masktpl_new = Rotation(masktpl, 0, angle);
                            flagDispose = true;
                        }
                        else
                        {
                            int idx = (int)(angle / step);
                            if (idx >= listSample.Count)
                            {
                                tplimg_new = Rotation(tplimg, 1, angle);
                                masktpl_new = Rotation(masktpl, 0, angle);
                                flagDispose = true;
                            }
                            else
                            {
                                tplimg_new = listSample[idx].Template;
                                masktpl_new = listSample[idx].Mask;
                            }
                        }

                        int tW = tplimg_new.Width; int tH = tplimg_new.Height;
                        int x = loc.X - 3 > 0 ? loc.X - 3 : 0;
                        int y = loc.Y - 3 > 0 ? loc.Y - 3 : 0;
                        int _w = (tW + 6 + x) > refimg.Width ? refimg.Width - x : (tW + 6);
                        int _h = (tH + 6 + y) > refimg.Height ? refimg.Height - y : (tH + 6);

                        Mat result = new Mat();
                        Mat roi = new Mat(refimg, new Rectangle(x, y, _w, _h));
                        if ((x + tW + 6) <= W && (y + tH + 6 <= H))
                        {
                            CvInvoke.MatchTemplate(
                                roi,
                                tplimg_new,
                                result,
                                Emgu.CV.CvEnum.TemplateMatchingType.CcorrNormed,
                                masktpl_new
                            );
                            double min_val = 0.0, max_val = 0.0;
                            Point min_loc = new Point(), max_loc = new Point();
                            CvInvoke.MinMaxLoc(result, ref min_val, ref max_val, ref min_loc, ref max_loc);
                            if (max_val >= threshScore)
                            {
                                PointF delta = Subpixel(result, max_loc);
                                Point p = new Point((int)(max_loc.X + x + delta.X), (int)(max_loc.Y + y + delta.Y));
                                Point center = new Point(p.X + tW / 2, p.Y + tH / 2);
                                DataPoint ans = new DataPoint
                                {
                                    Angle = angle,
                                    TopLeft = p,
                                    Center = center,
                                    Score = max_val
                                };

                                RES.Add(ans);
                            }
                        }
                        // Dispose
                        roi.Dispose();
                        result.Dispose();
                        if (flagDispose)
                        {
                            tplimg_new.Dispose();
                            masktpl_new.Dispose();
                        }
                    }
                }
                // Dispose
                masktpl.Dispose();
            }
            //st.Restart();
            var _ress = ReduceOverlap(RES, Wt, Ht, step);
            //st.Stop();
            //countTime += st.ElapsedMilliseconds;
            //return ReduceNoise(RES, Wt, Ht, step);
            return _ress;
        }
        /// <summary>
        /// Clear samples
        /// </summary>
        private void ClearSamples()
        {
            if (listSamples != null)
            {
                foreach (var item in listSamples)
                {
                    foreach (var data in item)
                    {
                        if (data != null)
                        {
                            if (data.Template != null)
                            {
                                if (data.Template.Ptr != IntPtr.Zero)
                                {
                                    data.Template.Dispose();
                                }
                            }
                            if (data.Mask != null)
                            {
                                if (data.Mask.Ptr != IntPtr.Zero)
                                {
                                    data.Mask.Dispose();
                                }
                            }
                        }
                    }
                }
                listSamples.Clear();
            }
        }
        /// <summary>
        /// Setup template
        /// </summary>
        public void SetTemplate()
        {
            ClearSamples();
            listSamples = BuildTemplateSamples();
        }
        /// <summary>
        /// Matching object with given image 
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public DataPoint[] Matching(Mat img)
        {
            List<DataPoint> ANS = new List<DataPoint>();
            VectorOfMat refimgs = new VectorOfMat();
            CvInvoke.BuildPyramid(img, refimgs, levelPyramid);
            // Process each level
            for (int level = levelPyramid; level >= 0; level--)
            {
                ANS = Matching(refimgs[level], level, ANS);

                //var result = ANS;
                //Mat draw = new Mat();
                //CvInvoke.CvtColor(refimgs[level], draw, ColorConversion.Gray2Bgr);

                //var vertices = ConvertToVertices(result.ToArray(), listSamples[level][0].Template.Size);
                //for (int i = 0; i < vertices.Length; i++)
                //{
                //    string text = string.Format($"{result[i].Score.ToString("0.00")}");
                //    CvInvoke.Polylines(draw, vertices[i], true, new MCvScalar(0, 0, 255), 2);
                //    CvInvoke.PutText(draw, text, vertices[i][0], FontFace.HersheySimplex, level > 0 ? 1.0 / level : 1.0, new MCvScalar(0, 255, 0));
                //}

                //CvInvoke.Imwrite(string.Format("out_{0}.jpg", level), draw);
            }
            // Clear
            refimgs.Dispose();
            Console.WriteLine($"Special Elapsed: {countTime}");
            return ANS.ToArray();
        }
        /// <summary>
        /// Convert list result to vertices of object
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public Point[][] ConvertToVertices(DataPoint[] results, Size imgSize)
        {
            Point[][] vers = new Point[results.Length][];
            for (int i = 0; i < results.Length; i++)
            {
                vers[i] = ConvertToVertices(results[i], imgSize);
            }
            return vers;
        }
        /// <summary>
        /// Draw result
        /// </summary>
        /// <param name="img"></param>
        /// <param name="result"></param>
        /// <param name="color"></param>
        /// <param name="thickness"></param>
        /// <returns></returns>
        public Mat DrawResult(Mat img, DataPoint[] result, MCvScalar color, int thickness)
        {
            Mat draw = img.Clone();
            var vertices = ConvertToVertices(result, imageTemplate.Size);
            for (int i = 0; i < vertices.Length; i++)
            {
                string text = string.Format($"({vertices[i][0].X}, {vertices[i][0].Y}, {result[i].Angle})");
                CvInvoke.Polylines(draw, vertices[i], true, color, thickness);
                CvInvoke.PutText(draw, text, vertices[i][0], FontFace.HersheySimplex, 1.0, color);
            }
            return draw;
        }
    }
}