using NXOpen;
using System.Collections.Generic;
using System.Linq;
using ArchBridgeDataModel;
using ArchBridgeAlgorithm.Helper;
using NXOpen.Assemblies;
using NXOpen.PartFamily;
using NXOpen.Features;

namespace ArchBridgeAlgorithm
{ 
    /// <summary>
    /// This class orchestrates the model generation algorithm
    /// </summary>
    public class Assembler
    {
        public static void SetupSession()
        {
            NXOpen.Session theSession = NXOpen.Session.GetSession();
            NXOpen.Part workPart = theSession.Parts.Work;
            NXOpen.Part displayPart = theSession.Parts.Display;
            NXOpen.UI theUI = NXOpen.UI.GetUI();
            theSession.CleanUpFacetedFacesAndEdges();

            // Lizenz aktivieren
            string[] bundles = new string[4] { "ACD10", "ACD11", "NXAMACAD", "SCACAD100" };
            theSession.LicenseManager.SetBundlesForUse(bundles);

            // Change journaling to c# umstellen
            theSession.Preferences.UserInterface.JournalLanguage = NXOpen.Preferences.SessionUserInterface.JournalLanguageType.Cs;
            theSession.Preferences.UserInterface.JournalFileFormat = NXOpen.Preferences.SessionUserInterface.JournalFileFormatType.Unicode;
            theSession.Preferences.UserInterface.InsetMenuDialogComments = true;

            // Relevante Teile öffnen
            string localRootPath = @"C:\Users\ga24nix\source\repos\modularBridgeCustomization\NxFiles";
            string[] partsToOpen = new string[12];

            partsToOpen[0] = localRootPath + "\\Superstructure\\Superstructure.prt";
            partsToOpen[1] = localRootPath + "\\Superstructure\\Deck.prt";
            partsToOpen[2] = localRootPath + "\\Substructure\\Substructure.prt";
            partsToOpen[3] = localRootPath + "\\Substructure\\ModularGroupLowerX.prt";
            partsToOpen[4] = localRootPath + "\\Substructure\\ModularGroupHigherX.prt";
            partsToOpen[5] = localRootPath + "\\Foundations\\Foundations.prt";
            partsToOpen[6] = localRootPath + "\\Foundations\\FoundationLowerY.prt";
            partsToOpen[7] = localRootPath + "\\Foundations\\FoundationHigherY.prt";
            partsToOpen[8] = localRootPath + "\\Foundations\\AbutmentLowerY.prt";
            partsToOpen[9] = localRootPath + "\\Foundations\\AbutmentHigherY.prt";

            partsToOpen[10] = localRootPath + "\\Substructure\\FourPointConnector.prt";
            partsToOpen[11] = localRootPath + "\\Foundations\\TemporaryJoint.prt";

            int i = 0;
            foreach (string path in partsToOpen)
            {
                try
                {
                    NXOpen.PartLoadStatus partLoadStatus;
                    if (i <= 4)
                    {
                        NXOpen.BasePart basePart = theSession.Parts.OpenActiveDisplay(partsToOpen[i], NXOpen.DisplayPartOption.AllowAdditional, out partLoadStatus);
                    }
                    else
                    {
                        NXOpen.BasePart basePart = theSession.Parts.Open(partsToOpen[i], out partLoadStatus);
                    }
                    partLoadStatus.Dispose();
                    i++;
                }
                catch
                {
                    i++;
                }
            }

            //loading options
            theSession.Parts.LoadOptions.LoadLatest = false;
            theSession.Parts.LoadOptions.ComponentLoadMethod = NXOpen.LoadOptions.LoadMethod.SearchDirectories;
            string[] searchDirectories1 = new string[8];

            searchDirectories1[0] = localRootPath + "\\Substructure\\";
            searchDirectories1[1] = localRootPath + "\\Substructure\\Bearing";
            searchDirectories1[2] = localRootPath + "\\Substructure\\KneeNodes";
            searchDirectories1[3] = localRootPath + "\\Substructure\\PanelModules";
            searchDirectories1[4] = localRootPath + "\\Substructure\\PanelModules\\ArchPanels\\";
            searchDirectories1[5] = localRootPath + "\\Substructure\\PanelModules\\ColumnPanels\\";
            searchDirectories1[6] = localRootPath + "\\Substructure\\Tendons\\";
            searchDirectories1[7] = localRootPath + "\\Foundations\\";


            bool[] searchSubDirs1 = new bool[8];
            searchSubDirs1[0] = true;
            searchSubDirs1[1] = true;
            searchSubDirs1[2] = true;
            searchSubDirs1[3] = true;
            searchSubDirs1[4] = true;
            searchSubDirs1[5] = true;
            searchSubDirs1[6] = true;
            searchSubDirs1[7] = true;

            theSession.Parts.LoadOptions.SetSearchDirectories(searchDirectories1, searchSubDirs1);
            theSession.Parts.LoadOptions.ComponentsToLoad = NXOpen.LoadOptions.LoadComponents.All;
            theSession.Parts.LoadOptions.PartLoadOption = NXOpen.LoadOptions.LoadOption.MinimallyLoadLightweightDisplay;
            theSession.Parts.LoadOptions.SetInterpartData(false, NXOpen.LoadOptions.Parent.Partial);
            theSession.Parts.LoadOptions.AllowSubstitution = false;
            theSession.Parts.LoadOptions.GenerateMissingPartFamilyMembers = true;
            theSession.Parts.LoadOptions.AbortOnFailure = false;
            theSession.Parts.LoadOptions.OptionUpdateSubsetOnLoad = NXOpen.LoadOptions.UpdateSubsetOnLoad.None;

            //set substructure display part
            BasePart substructurePart = theSession.Parts.FindObject("ArchBridge");
            NXOpen.PartLoadStatus pls;
            theSession.Parts.SetDisplay(substructurePart, false, false, out pls);
            NXOpen.Display.Camera camera1 = ((NXOpen.Display.Camera)workPart.Cameras.FindObject("Isometric"));
            workPart.ModelingViews.WorkView.Fit();
        }

        public static Substructure Modularize()
        {
            /// NX instance references and configurations for the local machine, insert your local root path of the nx - files
            //Please run the journal "sessionStart" in order to have the parts openened and accessible via the API
            #region configuration
            Configuration configuration = new Configuration(@"C:\Users\ga24nix\source\repos\modularBridgeCustomization\NxFiles");
            Session session = Session.GetSession();
            Part entireBridgePart = session.Parts.FindObject("ArchBridge") as Part;
            Part substructurePart = session.Parts.FindObject("Substructure") as Part;
            Part modularGroup1Part = session.Parts.FindObject("ModularGroupLowerX") as Part;
            Part modularGroup2Part = session.Parts.FindObject("ModularGroupHigherX") as Part;
            Part traversesPart = session.Parts.FindObject("Traverses") as Part;
            Substructure substructure = new Substructure(substructurePart);
            PartLoadStatus pls1, pls2;
            session.ListingWindow.Open();
            configuration.UpdateComponentNamePrefix();
            session.Parts.SetActiveDisplay(substructurePart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls1);
            session.Parts.SetWork(substructurePart);
            #endregion configuration

            /// Preprocessing steps in part files of substructure and modular groups
            session.ListingWindow.WriteFullline("SECTION #1 of model generation algorithm: Preprocessing of model structure and computation of initial system topology");
            #region preprocessing
            /// for each modular group, trim arch columns at plane level and wave - link the three sets of projected curves into the corresponding modular group part
            foreach (Part modularGroupPart in new List<Part>() { modularGroup1Part, modularGroup2Part })
            {
                List<Line> archSegments = new List<Line>(), archColumns = new List<Line>(), lateralColumns = new List<Line>();
                bool succeeded00 = PreprocessingHelper.TrimArchColumnsOfModularGroup(substructurePart, modularGroupPart);
                if (succeeded00) session.ListingWindow.WriteFullline("Sucessfully trimmed the arch columns in the steering sketch of the modular group part");
                bool succeeded01 = PreprocessingHelper.ImportSteeringSketchInModularGroupPart(substructurePart, modularGroupPart, ref archSegments, ref archColumns, ref lateralColumns);
                if (succeeded01) session.ListingWindow.WriteFullline("Sucessfully imported the steering sketch of the substructure part into the modular group parts");
                SubsystemModularGroup modularGroup = TopologyHelper.GenerateModularGroupOutlineFromSteeringSketch(substructurePart, modularGroupPart, archSegments, archColumns, lateralColumns);
                session.ListingWindow.WriteFullline("Modular Group from steering sketch sucessfully generated");
                substructure.AddModularGroup(modularGroup);
            }
            #endregion preprocessing

            ///  Generation of all arch elements and their placement
            session.ListingWindow.WriteFullline("");
            session.ListingWindow.WriteFullline("SECTION #2 of model generation algorithm: Creating, adding and orienting modules to arch");
            #region archGeneration
            //create two or three types of different kneeNode parts, assign parts to unique objects, place components in assembly and post - process the steering sketch
            bool succeeded10 = NxPartHelper.CreateUniqueKneeNodeParts(substructure, configuration.KneeNodeDir, configuration.ComponentNamePrefix);
            if (succeeded10) session.ListingWindow.WriteFullline("Successfully generated unique knee node module parts");
            bool succeeded11 = NxAssemblyHelper.AddKneeNodesToModularGroups(substructure);
            if (succeeded11) session.ListingWindow.WriteFullline("Knee node components added to both modular groups");
            bool succeeded12 = NxAssemblyHelper.TrimSteeringSketchWithKneeNodes(substructure);
            if (succeeded12) session.ListingWindow.WriteFullline("Faces of knee nodes imported and steering sketches of modular groups sucessfully trimmed");

            //bool succeeded20 = TopologyHelper.GenerateArchPanelObjects(substructure);
            //if (succeeded20) session.ListingWindow.WriteFullline("Topology and type of all arch elements computed.");
            //bool succeeded22 = NxPartHelper.CreateUniqueArchPanelParts(substructure, configuration.ArchPanelDir, configuration.ComponentNamePrefix);
            //if (succeeded22) session.ListingWindow.WriteFullline("Unique Arch Panel Module Parts generated");
            //bool succeeded23 = NxAssemblyHelper.AddArchPanelsToModularGroups(substructure);
            //if (succeeded23) session.ListingWindow.WriteFullline("Arch panel components added to both modular groups");
            #endregion archGeneration

            // Generation of all column elements and their placement
            session.ListingWindow.WriteFullline("");
            session.ListingWindow.WriteFullline("SECTION #3 of model generation algorithm: Creating, adding and orienting modules to column");
            #region columnGeneration
            bool succeeded30 = TopologyHelper.GenerateColumnObjects(substructure, configuration.ColumnPanelDir);
            if (succeeded30) session.ListingWindow.WriteFullline("Topology and type of all column elements sucessfully computed.");
            bool succeeded31 = NxPartHelper.CreateUniqueColumnPanelParts(substructure, configuration.ColumnPanelDir, configuration.ComponentNamePrefix);
            if (succeeded31) session.ListingWindow.WriteFullline("Successfully generated unique column panel module parts");
            bool succeeded32 = NxAssemblyHelper.AddColumnPanelsToModularGroups(substructure);
            if (succeeded32) session.ListingWindow.WriteFullline("Column panel components added to both modular groups");
            bool succeeded33 = NxAssemblyHelper.AddLateralColumnFoundations(substructure, configuration.FoundationDir);
            if (succeeded33) session.ListingWindow.WriteFullline("Box Foundations added to both modular groups");
            #endregion columnGeneration

            /// Generating and placing all tendons
            //session.ListingWindow.WriteFullline("");
            //session.ListingWindow.WriteFullline("SECTION #4 of model generation algorithm: Creating, adding and orienting tendons in both columns and archs");
            //#region tendonGeneration
            //bool succeeded50 = TopologyHelper.GenerateTendonObjectsInArchs(substructure);
            //if (succeeded50) session.ListingWindow.WriteFullline("Successfully computed topology and types of all tendons in archs.");
            //bool succeeded52 = NxPartHelper.CreateUniqueTendonPartsForArchs(substructure, configuration.TendonDir, configuration.ComponentNamePrefix);
            //if (succeeded52) session.ListingWindow.WriteFullline("Successfully generated unique tendon parts for archs");
            //bool succeeded53 = NxAssemblyHelper.AddTendonsToArchsInModularGroups(substructure);
            //if (succeeded53) session.ListingWindow.WriteFullline("Sucessfully added tendon components to archs");
            //bool succeeded54 = TopologyHelper.GenerateTendonObjectsInColumns(substructure);
            //if (succeeded54) session.ListingWindow.WriteFullline("Successfully computed topology and types of all tendons in columns.");
            //bool succeeded55 = NxPartHelper.CreateUniqueTendonPartsForColumns(substructure, configuration.TendonDir, configuration.ComponentNamePrefix);
            //if (succeeded55) session.ListingWindow.WriteFullline("Successfully generated unique tendon parts for columns");
            //bool succeeded56 = NxAssemblyHelper.AddTendonsToColumnsInModularGroups(substructure);
            //if (succeeded56) session.ListingWindow.WriteFullline("Sucessfully added tendon components to columns");
            //#endregion tendonGeneration

            ///// Traverses
            //session.ListingWindow.WriteFullline("");
            //session.ListingWindow.WriteFullline("SECTION #5 of model generation algorithm: Adding Traverses");
            //bool succeeded60 = TopologyHelper.GenerateTraverseObjects(substructure);
            //bool succeeded61 = NxAssemblyHelper.AddTraverses(substructure, traversesPart);
            //session.Parts.SetActiveDisplay(entireBridgePart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls2);
            //session.Parts.SetWork(entireBridgePart);

            /////// Post - processing the model structure and views by grouping the created parts another time and setting view and work part
            //session.ListingWindow.WriteFullline("");
            //session.ListingWindow.WriteFullline("SECTION #5 of Design Algorithm: Post-processing model structure");
            //bool succeeded71 = NxAssemblyHelper.GroupArchs(substructure, configuration.ContainerDir, configuration.ComponentNamePrefix);
            //if (succeeded71) session.ListingWindow.WriteFullline("Archs sucessfully grouped in nx model structure");
            //bool succeeded72 = NxAssemblyHelper.GroupColumns(substructure, configuration.ContainerDir, configuration.ComponentNamePrefix);
            //if (succeeded72) session.ListingWindow.WriteFullline("Columns sucessfully grouped in nx model structure");
            //PartLoadStatus pls3;
            //session.Parts.SetActiveDisplay(entireBridgePart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls3);
            return substructure;

        }

        public static void UndoModularization()
        {
            Session session = Session.GetSession();
            Part entireBridgePart = session.Parts.FindObject("ArchBridge") as Part;
            Part foundationsPart = session.Parts.FindObject("Foundations") as Part;
            Part substructurePart = session.Parts.FindObject("Substructure") as Part;
            Part modularGroup1Part = session.Parts.FindObject("ModularGroupLowerX") as Part;
            Part modularGroup2Part = session.Parts.FindObject("ModularGroupHigherX") as Part;
            Part traverses = session.Parts.FindObject("Traverses") as Part;
            PartLoadStatus pls1;
            session.Parts.SetActiveDisplay(entireBridgePart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls1);

            #region removeBoxFoundations
            session.Parts.SetWork(foundationsPart);
            try
            {
                Component[] boxFoundationComponentsToBeRemoved = foundationsPart.ComponentAssembly.RootComponent.GetChildren().Where(ch => ch.DisplayName == "BoxFoundation").ToArray();
                if (boxFoundationComponentsToBeRemoved.Count() > 0)
                {

                    NXOpen.Session.UndoMarkId id1 = session.NewestVisibleUndoMark;
                    session.UpdateManager.AddObjectsToDeleteList(boxFoundationComponentsToBeRemoved);
                    session.UpdateManager.DoUpdate(id1);
                }
            }
            catch { }

            session.ListingWindow.WriteFullline("Removed box foundations");
            #endregion removeBoxFoundations

            #region removeSubstructureFeatures
            session.Parts.SetWork(substructurePart);
            try
            {
                TaggedObject[] substructureFeaturesToBeRemoved = new TaggedObject[2];
                substructureFeaturesToBeRemoved[0] = substructurePart.Features.ToArray().Where(f => f.Name == "ArchColumnsTrimmedLowerX").First();
                substructureFeaturesToBeRemoved[1] = substructurePart.Features.ToArray().Where(f => f.Name == "ArchColumnsTrimmedHigherX").First();
                if (substructureFeaturesToBeRemoved[0] != null)
                {
                    NXOpen.Session.UndoMarkId id1 = session.NewestVisibleUndoMark;
                    session.UpdateManager.AddObjectsToDeleteList(substructureFeaturesToBeRemoved);
                    session.UpdateManager.DoUpdate(id1);
                }
            }
            catch { }

            session.ListingWindow.WriteFullline("Features added during modularization removed from substructure part");
            #endregion removeSubstructureFeatures

            #region emptyModularGroupParts
            foreach (Part modularGroupPart in new Part[] { modularGroup1Part, modularGroup2Part })
            {
                session.Parts.SetWork(modularGroupPart);
                // features
                Feature[] modularGroupPartFeaturesToBeRemoved = modularGroupPart.Features.ToArray(); //remove all features
                if (modularGroupPartFeaturesToBeRemoved.Length > 0)
                {
                    NXOpen.Session.UndoMarkId id1 = session.NewestVisibleUndoMark;
                    session.UpdateManager.AddObjectsToDeleteList(modularGroupPartFeaturesToBeRemoved);
                    session.UpdateManager.DoUpdate(id1);
                }
                //components
                try
                {
                    Component[] componentsToBeRemoved = modularGroupPart.ComponentAssembly.RootComponent.GetChildren();

                    if (componentsToBeRemoved.Length > 0)
                    {
                        NXOpen.Session.UndoMarkId id1 = session.NewestVisibleUndoMark;
                        session.UpdateManager.AddObjectsToDeleteList(componentsToBeRemoved);
                        session.UpdateManager.DoUpdate(id1);
                    }
                }
                catch(System.Exception e) { session.ListingWindow.WriteFullline(e.ToString()); }

                try
                {
                    session.Parts.SetWork(traverses);
                    Component[] traverseComponentsToBeRemoved = traverses.ComponentAssembly.RootComponent.GetChildren();
                    if (traverseComponentsToBeRemoved.Length > 0)
                    {
                        NXOpen.Session.UndoMarkId id1 = session.NewestVisibleUndoMark;
                        session.UpdateManager.AddObjectsToDeleteList(traverseComponentsToBeRemoved);
                        session.UpdateManager.DoUpdate(id1);
                    }
                }
                catch { }


                ///finished result
                session.ListingWindow.WriteFullline("Modular group" + modularGroupPart.Name + "empty or emptied from components and features added during modularization.");

            }
            #endregion emptyModularGroupParts

            PartLoadStatus pls2;
            session.Parts.SetActiveDisplay(entireBridgePart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls2);


        }

        public static void Main(string[] args)
        {
            Session session = Session.GetSession();
            try { Part substructure = session.Parts.FindObject("Substructure") as Part; }
            catch { Assembler.SetupSession(); }
            Part modularGroupPart = session.Parts.FindObject("ModularGroupLowerX") as Part;
            if (modularGroupPart.Features.ToArray().Length > 0) Assembler.UndoModularization();
            Assembler.Modularize();
        }

        public static int GetUnloadOption(string arg)
        {
            return System.Convert.ToInt32(Session.LibraryUnloadOption.Explicitly);
            //return System.Convert.ToInt32(Session.LibraryUnloadOption.Immediately);
            // return System.Convert.ToInt32(Session.LibraryUnloadOption.AtTermination);
        }
    }
}