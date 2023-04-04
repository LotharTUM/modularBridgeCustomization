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
    /// type 1 is for the arch
    /// type 2 is for the lateral columns
    /// </summary>
    public enum SupportTypeSpecification
    {
        type1, 
        type2, // columns
    }

    public class Support
    {
        //properties
        public Point3d SupportFaceReferencePoint { get; private set; }//midpoint for anchoring column panels
        public SupportTypeSpecification Type { get; private set; }
        public Component Component { get; private set; }


        //constructor
        public Support(SupportTypeSpecification type)
        {
            Type = type;
        }


        public void SetSupportFaceReferencePoint(Point3d supportFaceReferencePoint)
        {
            SupportFaceReferencePoint = supportFaceReferencePoint;
        }

        //setter for component not availabe at instantiation
        public void SetComponent(Component supportComponent)
        {
            Component = supportComponent;
        }
    }
}
