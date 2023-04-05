using NXOpen;
using NXOpen.Assemblies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchBridgeDataModel
{
    public class ColumnPanelAnchorage
    {
        public SubsystemColumn RelatedColumn { get; }
        //public Point3d LowerZMidPoint { get; }
        public double ColumnLength { get; }
       
        public Part Part { get; private set; }
        public Component Component { get; private set; }


        //constructor
        public ColumnPanelAnchorage(SubsystemColumn relatedColumn)
        {
            RelatedColumn = relatedColumn;
            //LowerZMidPoint = lowerZMidPoint;
            ColumnLength = relatedColumn.SketchGeo.GetLength();
        }


        //Setters for Properties not available during instantiation of object
        public void SetPart(Part anchoragePart)
        {
            Part = anchoragePart;
        }

        public void SetComponent(Component anchorageComponent)
        {
            Component = anchorageComponent;
        }
    }
}
