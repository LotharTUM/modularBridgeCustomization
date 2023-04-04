using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NXOpen;
using NXOpen.Features;
using NXOpen.UF;
using ArchBridgeDataModel;

namespace ArchBridgeAlgorithm.Helper
{
    /// <summary>
    /// class to encapsulate tedious geometric operations
    /// </summary>
    public static class GeometryHelper
    {
        public static Point3d GetLineCenter(Line line)
        {
            Point3d start = line.StartPoint;
            Point3d end = line.EndPoint;
            Point3d midPoint = new Point3d((start.X + end.X) / 2, (start.Y + end.Y) / 2, (start.Z + end.Z) / 2);
            return midPoint; 
        }

        public static List<Point3d> GetOrderedLineInterpolationPoints(char axis, Line line, int subdivCount)
        {
            List<Point3d> interpolationPoints = new List<Point3d>();
            interpolationPoints.Add(line.StartPoint);
            interpolationPoints.Add(line.EndPoint);

            if (axis == 'Y')
            {
                interpolationPoints = interpolationPoints.OrderBy(p => p.Y).ToList();
            }

            else if (axis == 'Z')
            {
                interpolationPoints = interpolationPoints.OrderBy(p => p.Z).ToList();
            }
            
            Point3d start = interpolationPoints.First();
            Point3d end = interpolationPoints.Last();
            double diffY = end.Y - start.Y;
            double diffZ = end.Z - start.Z;

            for (int i=1; i<subdivCount; i++)
            {
                double fraction = Convert.ToDouble(i) / Convert.ToDouble(subdivCount);
                Point3d iplPoint = new Point3d(start.X, start.Y + fraction * diffY, start.Z + fraction * diffZ);
                interpolationPoints.Insert(i, iplPoint);
            }
            
            return interpolationPoints;
        }

        /// <summary>
        /// method to get geometrical data for platform-hut modularization of columns in three types + bearing
        /// </summary>
        public static List<Point3d> SubdivideColumn(Point3d startPoint, Point3d endPoint, int numberOfPanels, double panelLength, bool isArchColumn)
        {

            List<Point3d> zOrderedinterpolationPoints = new List<Point3d>();

            //basic calculation parameters and checks
            double columnHeight = Math.Abs(endPoint.Z - startPoint.Z);

            //data structure and first point
            zOrderedinterpolationPoints.Add(startPoint);
            
            zOrderedinterpolationPoints.Add(new Point3d(startPoint.X, startPoint.Y, startPoint.Z + panelLength));

            if (numberOfPanels == 2) zOrderedinterpolationPoints.Add(endPoint);
            if (numberOfPanels == 3)
            {
                zOrderedinterpolationPoints.Add(new Point3d(startPoint.X, startPoint.Y, startPoint.Z + 2*panelLength));
                zOrderedinterpolationPoints.Add(endPoint);
            }


            //if the type 3 is very small and we have a lateral column, we can just move up the box foundation a little in the terrain to spare the extra part
            double currentHeightType3 = Math.Abs(zOrderedinterpolationPoints.ElementAt(zOrderedinterpolationPoints.Count-2).Z - zOrderedinterpolationPoints.Last().Z);
            if (currentHeightType3 < 1000.0 && !isArchColumn)
            {
                List<Point3d> zOrderedinterpolationPointsTmp = new List<Point3d>();

                int i = 1;
                foreach (Point3d point in zOrderedinterpolationPoints)
                {
                    if (i < zOrderedinterpolationPoints.Count) zOrderedinterpolationPointsTmp.Add(new Point3d(point.X, point.Y, point.Z + currentHeightType3));
                    i++;
                }
                zOrderedinterpolationPoints = zOrderedinterpolationPointsTmp;
            }
            
            return zOrderedinterpolationPoints;
        }

        public static double GetInclinationToPositiveY(Line line)
        {
            double inclination = 0.0;
            Point3d[] pointsAlongY = new Point3d[2];
            pointsAlongY[0] = line.StartPoint;
            pointsAlongY[1] = line.EndPoint;
            pointsAlongY = pointsAlongY.OrderBy(p => p.Y).ToArray();
            
            Snap.Vector lineDir = new Snap.Vector(0.0, pointsAlongY[1].Y-pointsAlongY[0].Y, pointsAlongY[1].Z - pointsAlongY[0].Z);
            Snap.Vector positiveY = new Snap.Vector(0.0, 1.0, 0.0);
            Snap.Vector.Angle(lineDir, positiveY);

            //in degrees
            return inclination;
        }

        /// <summary>
        /// method to get faces where the adjacent arch panels are dry joint for trimming the imported sketch in the modular group. 
        /// The top face is extracted from the four point connector component as in the knee node there a voiding has been placed there.
        /// </summary>
        public static Face[] GetKneeNodeBoundaryFaces(Body body, bool beforeXzSymmetry)
        {
            List<Face> occurenceBodyFaces = body.GetFaces().ToList();
            UFSession ufSession = UFSession.GetUFSession();

            //weird nx objects to measure face position
            double minU, maxU, minV, maxV;
            double[] box = new double[4];
            double[] uv = new double[2];
            ModlSrfValue modlSrfValue = new NXOpen.UF.ModlSrfValue();

            //custom variables to get face with lowest/max y/z
            
            List<Tuple<int, double, double>> yOrderedFaceIndexes = new List<Tuple<int, double, double>>();
            int maxZIdx = 0;
            double maxZ = -100000.0;
            int faceIndex = 0;

            foreach (Face face in occurenceBodyFaces)
            {
                //evaluate face
                ufSession.Modl.AskFaceUvMinmax(face.Tag, box);
                minU = box[0];
                maxU = box[1];
                minV = box[2];
                maxV = box[3];
                uv[0] = (minU + maxU) / 2;
                uv[1] = (minV + maxV) / 2;

                //update
                ufSession.Modl.EvaluateFace(face.Tag, UFConstants.UF_MODL_EVAL, uv, out modlSrfValue);
                double yVal = modlSrfValue.srf_pos[1];
                double zVal = modlSrfValue.srf_pos[2];
                double srfArea = (double) Snap.Compute.Area(new Snap.NX.Face[] { face });

                //update the indices of the faces ordered according to their y by inserting their index and value in the right position in the growing list
                if (srfArea > 120000.00)
                {
                    if (yOrderedFaceIndexes.Count == 0) yOrderedFaceIndexes.Insert(0, Tuple.Create(faceIndex, yVal, srfArea));
                    else
                    {
                        //exclude very small faces of voidings
                        int compareIdx = 0;
                        while (compareIdx < yOrderedFaceIndexes.Count && yVal > yOrderedFaceIndexes.ElementAt(compareIdx).Item2) compareIdx++;
                        yOrderedFaceIndexes.Insert(compareIdx, Tuple.Create(faceIndex, yVal, srfArea));

                    }
                }
                if (zVal > maxZ) { maxZ = zVal; maxZIdx = faceIndex; }
                faceIndex++;
            }

            //get the right indices (face with second-lowest y-value for lower arch segment, face with highest y-value for higher arch segment)
            int lowerYArchSegmentFaceIdx = 0;
            int higherYArchSegmentFaceIdx = 0;
            if (beforeXzSymmetry)
            {
                lowerYArchSegmentFaceIdx = yOrderedFaceIndexes.ElementAt(1).Item1; //the first eleven faces are part of the anchorage face and the voiding for anchoring the tendon
                higherYArchSegmentFaceIdx = yOrderedFaceIndexes.ElementAt(yOrderedFaceIndexes.Count-1).Item1;
            }

            //mirrored situtation if after symmetry axis
            else
            {
                lowerYArchSegmentFaceIdx = yOrderedFaceIndexes.First().Item1;
                higherYArchSegmentFaceIdx = yOrderedFaceIndexes.ElementAt(yOrderedFaceIndexes.Count - 2).Item1;
            }

            //return these three faces from the occurence body

            Face[] relevantFaces = new Face[2] {
                occurenceBodyFaces.ElementAt(lowerYArchSegmentFaceIdx),
                //occurenceBodyFaces.ElementAt(maxZIdx),
                occurenceBodyFaces.ElementAt(higherYArchSegmentFaceIdx)
                };
            return relevantFaces; 
        }

        public static Matrix3x3 GetUnitMatrix()
        {
            Matrix3x3 unitMatrix = new Matrix3x3();
            unitMatrix.Xx = 1.0;
            unitMatrix.Xy = 0.0;
            unitMatrix.Xz = 0.0;
            unitMatrix.Yx = 0.0;
            unitMatrix.Yy = 1.0;
            unitMatrix.Yz = 0.0;
            unitMatrix.Zx = 0.0;
            unitMatrix.Zy = 0.0;
            unitMatrix.Zz = 1.0;
            return unitMatrix;
        }

        public static Matrix3x3 GetRotationMatrixAroundXZ(double InclinationToYAxisInDegree)
        {
            double sind = Math.Sin(Math.PI * InclinationToYAxisInDegree / 180.0);
            double cosd = Math.Cos(Math.PI * InclinationToYAxisInDegree / 180.0);
            NXOpen.Matrix3x3 rotationXZ = new NXOpen.Matrix3x3();
            rotationXZ.Xx = 1.0;
            rotationXZ.Xy = 0.0;
            rotationXZ.Xz = 0.0;
            rotationXZ.Yx = 0.0;
            rotationXZ.Yy = cosd;
            rotationXZ.Yz = -sind;
            rotationXZ.Zx = 0.0;
            rotationXZ.Zy = sind;
            rotationXZ.Zz = cosd;

            return rotationXZ;
        }

    }
}

