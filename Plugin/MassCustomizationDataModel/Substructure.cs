using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NXOpen;

namespace ArchBridgeDataModel
{
    public class Substructure
    {
        public List<SubsystemModularGroup> ModularGroups { get; private set; } = new List<SubsystemModularGroup>();
        public Part Part { get; private set; }
        public List<ModuleTraverse> Traverses { get; private set; } = new List<ModuleTraverse>();


        public Substructure(Part part) 
        {
            Part = part;
        }

        public void AddModularGroup(SubsystemModularGroup modularGroup)
        {
            ModularGroups.Add(modularGroup);
        }

        public void AddTraverses(List<ModuleTraverse> traverses)
        {
            Traverses = traverses;
        }


        public List<ModuleKneeNode> GetKneeNodes()
        {
            List<ModuleKneeNode> kneeNodes = new List<ModuleKneeNode>();
            kneeNodes.AddRange(ModularGroups.First().Arch.KneeNodes);
            kneeNodes.AddRange(ModularGroups.Last().Arch.KneeNodes);
            return kneeNodes;
        }

    }
}
