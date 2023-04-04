using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NXOpen;

namespace ArchBridgeDataModel
{
    public class SubsystemModularGroup
    {
        //properties
        public SubsystemArch Arch { get; }
        public List<SubsystemColumn> Columns { get; } = new List<SubsystemColumn>();
        public List<Support> Supports { get; } = new List<Support>();
        public Part Part { get; }


        //constructor
        public SubsystemModularGroup(Part part, SubsystemArch arch, List<SubsystemColumn> columns, List<Support> supports)
        {
            Arch = arch;
            Columns = columns;
            Supports = supports;
            Part = part;
        }

        public List<ModuleArchPanel> GetArchPanels()
        {
            List<ModuleArchPanel> archPanels = new List<ModuleArchPanel>();
            foreach (SubsystemArchSegment archSegment in Arch.ArchSegments)
            {
                archPanels.AddRange(archSegment.YOrderedPanels);
            }
            return archPanels;
        }

        //getters to collect data from all columns subordinated to group
        public List<ModuleColumnPanel> GetColumnPanels()
        {
            List<ModuleColumnPanel> columnPanels = new List<ModuleColumnPanel>();
            foreach (SubsystemColumn column in Columns)
            {
                columnPanels.AddRange(column.ZOrderedPanels);
            }
            return columnPanels;
        }


        public List<Tendon> GetTendons()
        {
            List<Tendon> tendons = new List<Tendon>();
            tendons.AddRange(Arch.GetTendons());
            foreach (SubsystemColumn column in Columns)
            {
                tendons.AddRange(column.Tendons);
            }
            return tendons;
        }
    }
}
