using NXOpen;
using NXOpen.Assemblies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchBridgeDataModel
{
    public class ModuleTraverse
    {
        //properties
        public ModuleKneeNode LowerXKneeNode { get; }
        public Point3d LowerXReferencePoint { get; }

        public Part Part { get; private set; }
        public Component Component { get; private set; }


        //constructor
        public ModuleTraverse(ModuleKneeNode lowerXKneeNode, Point3d lowerXReferencePoint)
        {
            LowerXKneeNode = lowerXKneeNode;
            LowerXReferencePoint = lowerXReferencePoint;
        }

        //Setters for Properties not available during instantiation of object
        public void SetPart(Part traversePart)
        {
            Part = traversePart;
        }

        public void SetComponent(Component traverseComponent)
        {
            Component = traverseComponent;
        }
    }
}
