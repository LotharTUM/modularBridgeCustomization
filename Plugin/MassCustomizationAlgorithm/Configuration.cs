using System.IO;
using System.Linq;

namespace ArchBridgeAlgorithm.Helper
{
    public class Configuration
    {
        //properties
        public string LocalPathToNxParts;
        public string LocalPathToPrefixFile;
        public string KneeNodeDir;
        public string ArchPanelDir;
        public string ColumnPanelDir;
        public string FoundationDir;
        public string TendonDir;
        public string ContainerDir;

        public string ArchPanelType1Name;
        public string ArchPanelType2Name;
        public string ArchPanelType3Name;

        public string ColumnPanelType1Name;
        public string ColumnPanelType2Name;
        public string ColumnPanelType3Name;


        //prefix for naming components of single algorithm runthrough
        public char ComponentNamePrefix = ' ';

        public Configuration(string localPathToNxParts)
        {
            // get all subdirs relative to the local path of the nx parts 
            Directory.SetCurrentDirectory(localPathToNxParts);
            LocalPathToNxParts = localPathToNxParts;
            KneeNodeDir = string.Format("{0}\\Substructure\\KneeNodes", localPathToNxParts);
            ArchPanelDir = string.Format("{0}\\Substructure\\PanelModules\\ArchPanels", localPathToNxParts);
            ColumnPanelDir = string.Format("{0}\\Substructure\\PanelModules\\ColumnPanels", localPathToNxParts);
            FoundationDir = string.Format("{0}\\Foundations", localPathToNxParts);
            TendonDir = string.Format("{0}\\\\Substructure\\Tendons", localPathToNxParts);
            ContainerDir = string.Format("{0}\\Substructure\\ArchAndColumnGroups", localPathToNxParts);


            //during a nx session, no component with the same name can be added twice to an assembly
            //therefore we add a prefix to all parts added in order to be able to apply the algorithm multiple times without needing to restart nx
            //a txt-file with alphabetically ordered prefixes is in 
            LocalPathToPrefixFile = string.Format("{0}\\..\\Plugin\\MassCustomizationAlgorithm\\bin\\Debug\\prefix.txt", localPathToNxParts);
            ComponentNamePrefix = GetPrefix(LocalPathToPrefixFile);
           
        }

        public void UpdateComponentNamePrefix()
        {
            ComponentNamePrefix = GetPrefix(LocalPathToPrefixFile);
        }

        private char GetPrefix(string prefixFilePath)
        {
            string content;

            using (StreamReader reader = new StreamReader(prefixFilePath))
            {
                content = reader.ReadLine();
                ComponentNamePrefix = content.ElementAt(0);
                content = content.Substring(1, content.Length - 1);
            }

            using (StreamWriter writer = new StreamWriter(prefixFilePath))
            {
                writer.WriteLine(content);
            }

            return ComponentNamePrefix;

        }
    }
}
