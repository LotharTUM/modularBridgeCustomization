using System;
using System.Linq;
using System.Collections.Generic;
using NXOpen;
using Snap;

namespace ArchBridgeDataModel
{
    public class SubsystemArchSegment
    {
        //properties
        public Line SketchGeo { get; private set; }
        public ModuleKneeNode KneeNodeLowerY { get; private set; }
        public ModuleKneeNode KneeNodeHigherY { get; private set; }
        public List<ModuleArchPanel> YOrderedPanels { get; private set; }
        public List<Tendon> Tendons { get; private set; }
        public Support IncidentSupport { get; }
        public double InclinationToPositiveY { get; } = 0.0; //in degrees
        public bool BeforeXZSymmetryPlane { get; } = true; // by default true (level middle arch segment has this attribute set to false



        //constructors
        public SubsystemArchSegment(Line sketchGeo, bool beforeXZSymmetryPlane)
        {
            SketchGeo = sketchGeo;
            //get inclination to orient panels accordingly later
            Point3d[] pointsAlongY = new Point3d[2];
            pointsAlongY[0] = sketchGeo.StartPoint;
            pointsAlongY[1] = sketchGeo.EndPoint;
            pointsAlongY = pointsAlongY.OrderBy(p => p.Y).ToArray();
            Snap.Vector lineDir = new Vector(pointsAlongY[1].X - pointsAlongY[0].X, pointsAlongY[1].Y - pointsAlongY[0].Y, pointsAlongY[1].Z - pointsAlongY[0].Z);
            Snap.Vector positiveY = new Snap.Vector(0.0, 1.0, 0.0);
            InclinationToPositiveY = Snap.Vector.Angle(lineDir, positiveY);
            if (lineDir.Z <= 0) InclinationToPositiveY = (-1) * InclinationToPositiveY;
            BeforeXZSymmetryPlane = beforeXZSymmetryPlane;
            YOrderedPanels = new List<ModuleArchPanel>();
            Tendons = new List<Tendon>();
        }
        public SubsystemArchSegment(Line sketchGeo, Support incidentSupport, bool beforeXZSymmetryPlane)
        { 
            SketchGeo = sketchGeo;
            IncidentSupport = incidentSupport;

            //get inclination to later orient panels accordingly
            Point3d[] pointsAlongY = new Point3d[2];
            pointsAlongY[0] = sketchGeo.StartPoint;
            pointsAlongY[1] = sketchGeo.EndPoint;
            pointsAlongY = pointsAlongY.OrderBy(p => p.Y).ToArray();
            Snap.Vector lineDir = new Snap.Vector(pointsAlongY[1].X- pointsAlongY[0].X, pointsAlongY[1].Y - pointsAlongY[0].Y, pointsAlongY[1].Z - pointsAlongY[0].Z);
            Snap.Vector positiveY = new Snap.Vector(0.0, 1.0, 0.0);
            InclinationToPositiveY = Snap.Vector.Angle(lineDir, positiveY);
            if (lineDir.Z <= 0) InclinationToPositiveY = (-1) * InclinationToPositiveY;
            BeforeXZSymmetryPlane = beforeXZSymmetryPlane;
            YOrderedPanels = new List<ModuleArchPanel>();
            Tendons = new List<Tendon>(); 
        }



        //Setters for objects not available during instantiation or changed during algorithm
        public void SetSketchGeo(Line line)
        {
            SketchGeo = line;
        }
        public void SetLowerYKneeNode(ModuleKneeNode kneeNodeLowerY)
        {
            KneeNodeLowerY = kneeNodeLowerY;
        }
        public void SetBothKneeNodes(ModuleKneeNode kneeNodeLowerY, ModuleKneeNode kneeNodeHigherY)
        {
            KneeNodeLowerY = kneeNodeLowerY;
            KneeNodeHigherY = kneeNodeHigherY;
        }
        public void SetHigherYKneeNode(ModuleKneeNode kneeNodeHigherY)
        {
            KneeNodeHigherY = kneeNodeHigherY;
        }
        public void SetArchPanels(List<ModuleArchPanel> archPanels)
        {
            YOrderedPanels = archPanels;
        }
        public void SetTendons(List<Tendon> tendons)
        {
            Tendons = tendons;
        }
        public List<ModuleKneeNode> GetKneeNodes()
        {
            List<ModuleKneeNode> kneeNodes = new List<ModuleKneeNode>();
            if (KneeNodeLowerY != null)
            {
                kneeNodes.Add(KneeNodeLowerY);
            }

            if (KneeNodeHigherY != null)
            {
                kneeNodes.Add(KneeNodeHigherY);
            }
            return kneeNodes;
        }
   
    }
}
