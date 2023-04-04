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
    /// type 1 is on top of kneeNode/foundation, tendon with compound and positive upper bolt
    /// type 2 is middle part in a column with three modules (negative voiding at bottom, positive on top, incl. duct)
    /// type 3 is part on top of another module and below the deck 
    /// type 4 is part on top of kneeNode/foundation and below the deck
    /// </summary>
    public enum ColumnPanelTypeSpecification
    {
        Type1, 
        Type2, 
        Type3,
        Type4
    }

    public class ModuleColumnPanel
    {
        //properties
        public SubsystemColumn ContainingColumn { get; }
        public ColumnPanelTypeSpecification Type { get; }
        public Point3d LowerZMidPoint { get; }
        public Point3d HigherZMidPoint { get; }
        public double Length { get; }
        public Part Part { get; private set; } 
        public Component Component { get; private set; }


        //constructor
        public ModuleColumnPanel(SubsystemColumn containingColumn, ColumnPanelTypeSpecification type, Point3d lowerZMidPoint, Point3d higherZMidPoint)
        {
            ContainingColumn = containingColumn;
            Type = type;
            LowerZMidPoint = lowerZMidPoint;
            HigherZMidPoint = higherZMidPoint;
            Length = Math.Round(Math.Sqrt(Math.Pow((higherZMidPoint.X - lowerZMidPoint.X), 2) + Math.Pow((higherZMidPoint.Y - lowerZMidPoint.Y), 2) + Math.Pow((higherZMidPoint.Z - lowerZMidPoint.Z), 2)), 2);
        }

      
        //Setters for Properties not available during instantiation of object
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
