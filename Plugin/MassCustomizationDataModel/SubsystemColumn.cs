using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NXOpen;

namespace ArchBridgeDataModel
{
    public class SubsystemColumn
    {

        //properties
        public Line SketchGeo { get; private set; }
        public List<ModuleColumnPanel> ZOrderedPanels { get; private set; } = new List<ModuleColumnPanel>();
        public ModuleKneeNode KneeNode { get; private set; }
        //public ModuleBearing Bearing { get; private set; }
        public Support Support { get; private set; }
        public List<Tendon> Tendons { get; private set; } = new List<Tendon>();
        public bool BeforeXZSymmetryPlane { get; private set; } = true;
        public bool IsArchColumn { get; private set; } = true;


        //Constructors
        public SubsystemColumn(Line sketchGeo, bool beforeXZSymmetry, bool isArchColumn)
        {
            SketchGeo = sketchGeo;
            Support = null;
            KneeNode = null;
            BeforeXZSymmetryPlane = beforeXZSymmetry;
            IsArchColumn = isArchColumn;
        }

        public SubsystemColumn(Line sketchGeo, Support support, bool beforeXZSymmetry, bool isArchColumn)
        {
            SketchGeo = sketchGeo;
            Support = support;
            KneeNode = null;
            BeforeXZSymmetryPlane = beforeXZSymmetry;
            IsArchColumn = isArchColumn;
        }


        // Setters for objects not available during instantation or modified during algorithm (e.g. sketch geo after trimming)
        public void SetSketchGeo(Line line)
        {
            SketchGeo = line;
        }
        public void SetKneeNode(ModuleKneeNode kneeNode)
        {
            KneeNode = kneeNode;
        }
        public void SetColumnPanels(List<ModuleColumnPanel> columnPanels)
        {
            ZOrderedPanels = columnPanels;
        }

        public void SetTendons(List<Tendon> tendons)
        {
            Tendons = tendons;
        }
      
    }
}
