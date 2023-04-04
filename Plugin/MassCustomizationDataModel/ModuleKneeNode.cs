using System;
using System.Collections.Generic;
using System.Linq;
using NXOpen;
using NXOpen.UF;
using NXOpen.Assemblies;
using NXOpen.Features;
using Snap;

namespace ArchBridgeDataModel
{
    public class ModuleKneeNode
    {
        //properties
        public SubsystemColumn Column { get; } 
        public SubsystemArchSegment ArchSegmentLowerY { get; }
        public SubsystemArchSegment ArchSegmentHigherY { get; }
        public bool KneeNodeBeforeXZSymmetry { get; }
        public double AngleLowerZArchSegment { get; }// relative to vertical axis through midpoint, see KneeNode Part File
        public double AngleHigherZArchSegment { get; } // relative to horizontal axis through midpoint, see KneeNode Part File
        public Point3d MidPoint { get; }
        public Part Part { get; private set; }
        public Component Component { get; private set; }


        //constructor
        public ModuleKneeNode(SubsystemColumn column, SubsystemArchSegment archSegmentLowerY, SubsystemArchSegment archSegmentHigherY)
        {
            //set references
            Column = column;
            ArchSegmentLowerY = archSegmentLowerY;
            ArchSegmentHigherY = archSegmentHigherY;

            UFCurve ufCurve = NXOpen.UF.UFSession.GetUFSession().Curve;
            UFCurve.IntersectInfo ufCurveIntersectInfo;
            
            //compute midpoint
            ufCurve.Intersect(
                archSegmentLowerY.SketchGeo.Tag,
                archSegmentHigherY.SketchGeo.Tag,
                new double[] { archSegmentLowerY.SketchGeo.EndPoint.X, archSegmentLowerY.SketchGeo.EndPoint.Y, archSegmentLowerY.SketchGeo.EndPoint.Z },
                out ufCurveIntersectInfo);

            MidPoint = new Point3d(ufCurveIntersectInfo.curve_point[0], ufCurveIntersectInfo.curve_point[1], ufCurveIntersectInfo.curve_point[2]);

            //assess position relative to xz-symmetry axis for each arch segment
            List<Point3d> pLowerY = new List<Point3d>() { ArchSegmentLowerY.SketchGeo.StartPoint, ArchSegmentLowerY.SketchGeo.EndPoint };
            pLowerY = pLowerY.OrderBy(p => p.Y).ToList();
            List<Point3d> pHigherY = new List<Point3d>() { ArchSegmentHigherY.SketchGeo.StartPoint, ArchSegmentHigherY.SketchGeo.EndPoint };
            pHigherY = pHigherY.OrderBy(p => p.Y).ToList();

            double deltaZSegment = pLowerY.ElementAt(1).Z - pLowerY.ElementAt(0).Z;
            if (deltaZSegment > 0 && System.Math.Abs(deltaZSegment) > 0.1) KneeNodeBeforeXZSymmetry = true; else KneeNodeBeforeXZSymmetry = false;

            // for every of the arch segments (in increasing y-order), compute an (z-)upwards-pointing vector along the line
            Vector vecLowerYSegment = new Vector(
                pLowerY.ElementAt(1).X - pLowerY.ElementAt(0).X,
                pLowerY.ElementAt(1).Y - pLowerY.ElementAt(0).Y,
                pLowerY.ElementAt(1).Z - pLowerY.ElementAt(0).Z);
            
            Vector vecHigherYSegment = new Vector(
                pHigherY.ElementAt(1).X - pHigherY.ElementAt(0).X,
                pHigherY.ElementAt(1).Y - pHigherY.ElementAt(0).Y,
                pHigherY.ElementAt(1).Z - pHigherY.ElementAt(0).Z);

            //compute and set angles exploiting the symmetry
            if (KneeNodeBeforeXZSymmetry)
            {
                AngleLowerZArchSegment = System.Math.Round(90 - (Snap.Vector.Angle(Vector.AxisZ, vecLowerYSegment) % 90), 2);
                AngleHigherZArchSegment = System.Math.Round(Snap.Vector.Angle(Vector.AxisY, vecHigherYSegment) % 90, 2);
            }
            else
            {
                vecLowerYSegment.Y = vecLowerYSegment.Y * (-1);
                vecLowerYSegment.Z = vecLowerYSegment.Z * (-1);

                vecHigherYSegment.Y = vecHigherYSegment.Y * (-1);
                vecHigherYSegment.Z = vecHigherYSegment.Z * (-1);
               
                AngleHigherZArchSegment = Snap.Vector.Angle(-Vector.AxisY, vecLowerYSegment) % 90;
                AngleLowerZArchSegment = 90-(Snap.Vector.Angle(Vector.AxisZ, vecHigherYSegment) % 90);
            }
        }


        //Setters for properties not available during instantiation of object
        public void SetPart(Part kneeNodePart)
        {
            Part = kneeNodePart;
        }
        public void SetComponent(Component kneeNodeComponent)
        {
            Component = kneeNodeComponent;
        }

    }
}
