using NXOpen;
using NXOpen.Assemblies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchBridgeDataModel
{

    /// <summary>
    /// a1 is the module variant connected to the foundation and without duct (tendon with compound)
    /// a2 is the point-symmetric module variant for the rest of the arch segment
    /// </summary>
    public enum ArchPanelTypeSpecification
    {
        Type1, 
        Type2
    }

    public class ModuleArchPanel
    {
        //properties
        public SubsystemArchSegment ContainingSegment { get; }
        public ArchPanelTypeSpecification Type { get; }
        public Point3d LowerYMidPoint { get; }
        public Point3d HigherYMidPoint { get; }
        public double Length { get; }
        public Part Part {get; private set;} 
        public Component Component { get; private set; }


        //constructor
        public ModuleArchPanel(SubsystemArchSegment containingSegment, ArchPanelTypeSpecification type, Point3d lowerYMidPoint, Point3d higherYMidPoint)
        {
            ContainingSegment = containingSegment;
            Type = type;
            LowerYMidPoint = lowerYMidPoint;
            HigherYMidPoint = higherYMidPoint;
            Length = Math.Round(Math.Sqrt(Math.Pow((higherYMidPoint.X - lowerYMidPoint.X), 2) + Math.Pow((higherYMidPoint.Y - lowerYMidPoint.Y), 2) + Math.Pow((higherYMidPoint.Z - lowerYMidPoint.Z), 2)),2);
        }

        public ModuleArchPanel()
        {
           
        }


        //setters for objects not available during instantiation
        public void SetPart(Part archPanelModulePart)
        {
            Part = archPanelModulePart;
        }

        public void SetComponent(Component archPanelModuleComponent)
        {
            Component = archPanelModuleComponent;
        }

    }
}
