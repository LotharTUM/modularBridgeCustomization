using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchBridgeDataModel
{
    public class SubsystemArch
    {

        //properties
        public List<SubsystemArchSegment> ArchSegments { get; } = new List<SubsystemArchSegment>();
        public List<ModuleKneeNode> KneeNodes { get; } = new List<ModuleKneeNode>();
       

        //constructor
        public SubsystemArch(List<SubsystemArchSegment> archSegments, List<ModuleKneeNode> kneeNodes)
        {
            ArchSegments = archSegments;
            KneeNodes = kneeNodes;
        }

        //getters to collect data from subordinate classes
        public List<ModuleArchPanel> GetArchPanels()
        {
            List<ModuleArchPanel> archPanels = new List<ModuleArchPanel>();
            foreach (SubsystemArchSegment archSegment in ArchSegments)
            {
                archPanels.AddRange(archSegment.YOrderedPanels);
            }
            return archPanels;
        }
        public List<Tendon> GetTendons()
        {
            List<Tendon> tendons = new List<Tendon>();

            foreach (SubsystemArchSegment archSegment in ArchSegments)
            {
                tendons.AddRange(archSegment.Tendons);
            }
            return tendons;
        }

    }
}
