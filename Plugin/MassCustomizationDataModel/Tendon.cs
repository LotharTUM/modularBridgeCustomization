using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NXOpen;
using NXOpen.Assemblies;

namespace ArchBridgeDataModel
{
    /// <summary>
    /// type1, // Arch segment, either anchored at a panel and a knee node or at two knee nodes 
    /// type2 // columns 

    /// </summary>
    public enum TendonTypeSpecification 
    {
        Type1, 
        Type2
    }

    public class Tendon
    {
        //properties
        public TendonTypeSpecification Type { get; }
        public bool LowerTendonInArch { get;  } //relevant for structural analysis
        public SubsystemArchSegment ArchSegmentJoined { get; }
        public SubsystemColumn ColumnJoined { get; }

        public ModuleKneeNode AnchorageForOuterArchSegment { get; } = null;
        public ModuleKneeNode LowerYAnchorageForInnerArchSegment { get; } = null;
        public ModuleKneeNode HigherYAnchorageForInnerArchSegment { get; } = null;

        public Point3d LowerMidPoint { get; }
        public Point3d HigherMidPoint { get; }
        public double Length { get; }

        public Part Part { get; private set; }
        public Component Component { get; private set; } 

        //constructors
        /// <summary>
        /// constructor for outer arch segments to be joined, with one kneeNode specified (type1)
        /// </summary>
        public Tendon(SubsystemArchSegment subsystemToBeJoined, ModuleKneeNode kneeNode, Point lowerMidPoint, Point higherMidPoint, bool lowerTendonInArch)
        {
            Type = TendonTypeSpecification.Type1;
            LowerTendonInArch = lowerTendonInArch;
            ArchSegmentJoined = subsystemToBeJoined;
            AnchorageForOuterArchSegment = kneeNode;
            LowerMidPoint = new Point3d(lowerMidPoint.Coordinates.X, lowerMidPoint.Coordinates.Y, lowerMidPoint.Coordinates.Z);
            HigherMidPoint = new Point3d(higherMidPoint.Coordinates.X, higherMidPoint.Coordinates.Y, higherMidPoint.Coordinates.Z);
            Length = Math.Round(Math.Sqrt(
                Math.Pow((higherMidPoint.Coordinates.X - lowerMidPoint.Coordinates.X), 2) +
                Math.Pow((higherMidPoint.Coordinates.Y - lowerMidPoint.Coordinates.Y), 2) +
                Math.Pow((higherMidPoint.Coordinates.Z - lowerMidPoint.Coordinates.Z), 2)), 2);
    }

        /// <summary>
        /// constructor for inner arch segments to be joined, with two kneeNodes specified (type1)
        /// </summary>
        public Tendon(SubsystemArchSegment subsystemToBeJoined, ModuleKneeNode lowerYKneeNode, ModuleKneeNode higherYKneeNode, Point lowerMidPoint, Point higherMidPoint, bool lowerTendonInArch)
        {
            Type = TendonTypeSpecification.Type1;
            LowerTendonInArch = lowerTendonInArch;
            ArchSegmentJoined = subsystemToBeJoined;
            LowerYAnchorageForInnerArchSegment = lowerYKneeNode;
            HigherYAnchorageForInnerArchSegment = higherYKneeNode;

            LowerMidPoint = new Point3d(lowerMidPoint.Coordinates.X, lowerMidPoint.Coordinates.Y, lowerMidPoint.Coordinates.Z);
            HigherMidPoint = new Point3d(higherMidPoint.Coordinates.X, higherMidPoint.Coordinates.Y, higherMidPoint.Coordinates.Z);
            Length = Math.Round(Math.Floor(Math.Sqrt(
                Math.Pow((higherMidPoint.Coordinates.X - lowerMidPoint.Coordinates.X), 2) +
                Math.Pow((higherMidPoint.Coordinates.Y - lowerMidPoint.Coordinates.Y), 2) +
                Math.Pow((higherMidPoint.Coordinates.Z - lowerMidPoint.Coordinates.Z), 2))), 2);
        }

        /// <summary>
        /// constructor for columns to be joined, without kneeNodes (type2)
        /// </summary>
        public Tendon(SubsystemColumn subsystemToBeJoined, Point lowerMidPoint, Point higherMidPoint)
        {
            Type = TendonTypeSpecification.Type2;
            ColumnJoined = subsystemToBeJoined;
            LowerMidPoint = new Point3d(lowerMidPoint.Coordinates.X, lowerMidPoint.Coordinates.Y, lowerMidPoint.Coordinates.Z);
            HigherMidPoint = new Point3d(higherMidPoint.Coordinates.X, higherMidPoint.Coordinates.Y, higherMidPoint.Coordinates.Z);
            Length = Math.Sqrt(
                Math.Pow((higherMidPoint.Coordinates.X - lowerMidPoint.Coordinates.X), 2) +
                Math.Pow((higherMidPoint.Coordinates.Y - lowerMidPoint.Coordinates.Y), 2) +
                Math.Pow((higherMidPoint.Coordinates.Z - lowerMidPoint.Coordinates.Z), 2));
        }

    
        //setters for objects not available during instantiation
        public void SetPart(Part tendonAsPart)
        {
            Part = tendonAsPart;
        }

        public void SetComponent(Component tendonAsComponent)
        {
            Component = tendonAsComponent;
        }

    }
}
