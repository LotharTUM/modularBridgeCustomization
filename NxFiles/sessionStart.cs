// NX 1965
// Journal created by ga24nix on Thu Sep  1 13:55:11 2022 Mitteleuropäische Sommerzeit

//
using System;
using NXOpen;

public class NXJournal
{
  public static void Main(string[] args)
  {
    NXOpen.Session theSession = NXOpen.Session.GetSession();
    NXOpen.Part workPart = theSession.Parts.Work;
    NXOpen.Part displayPart = theSession.Parts.Display;
	NXOpen.UI theUI = NXOpen.UI.GetUI();
    theSession.CleanUpFacetedFacesAndEdges();
    
	NXOpen.Session.UndoMarkId markId1;
    markId1 = theSession.SetUndoMark(NXOpen.Session.MarkVisibility.Invisible, "Start");
    theSession.SetUndoMarkName(markId1, "ben. Einstellungen für Lothar");
	
	// Lizenz aktivieren
	string[] bundles = new string[4] { "ACD10", "ACD11", "NXAMACAD", "SCACAD100" };
    theSession.LicenseManager.SetBundlesForUse(bundles);
    
    // Journaling auf c# umstellen
	theSession.Preferences.UserInterface.JournalLanguage = NXOpen.Preferences.SessionUserInterface.JournalLanguageType.Cs;
    theSession.Preferences.UserInterface.JournalFileFormat = NXOpen.Preferences.SessionUserInterface.JournalFileFormatType.Unicode;
    theSession.Preferences.UserInterface.InsetMenuDialogComments = true;
	
    // Relevante Teile öffnen
    string localRootPath = @"C:\Users\ga24nix\source\repos\modularBridgeCustomization\NxFiles";
    string[] partsToOpen = new string[13];

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
    partsToOpen[12] = localRootPath + "\\Substructure\\Traverse\\Traverse.prt";
    
    int i = 0;
    foreach(string path in partsToOpen){
        try{
            NXOpen.PartLoadStatus partLoadStatus;
            NXOpen.BasePart basePart = theSession.Parts.OpenActiveDisplay(partsToOpen[i], NXOpen.DisplayPartOption.AllowAdditional, out partLoadStatus);
            partLoadStatus.Dispose();
            i++;
        }
        catch{
            i++;
        }
    }
    
    
    //loading options
    theSession.Parts.LoadOptions.LoadLatest = false;
    theSession.Parts.LoadOptions.ComponentLoadMethod = NXOpen.LoadOptions.LoadMethod.SearchDirectories;
    string[] searchDirectories1 = new string[6];
    
    searchDirectories1[0] = localRootPath+"\\Substructure\\";
    searchDirectories1[1] = localRootPath+"\\Substructure\\KneeNodes";
    searchDirectories1[2] = localRootPath+"\\Substructure\\PanelModules";
    searchDirectories1[3] = localRootPath+"\\Substructure\\PanelModules\\ArchPanels\\";
    searchDirectories1[4] = localRootPath+"\\Substructure\\PanelModules\\ColumnPanels\\";
    searchDirectories1[5] = localRootPath+"\\Foundations\\";

	
    bool[] searchSubDirs1 = new bool[6];
    searchSubDirs1[0] = true;
    searchSubDirs1[1] = true;
    searchSubDirs1[2] = true;
    searchSubDirs1[3] = true;
    searchSubDirs1[4] = true;
    searchSubDirs1[5] = true;

				
    
    theSession.Parts.LoadOptions.SetSearchDirectories(searchDirectories1, searchSubDirs1);
    theSession.Parts.LoadOptions.ComponentsToLoad = NXOpen.LoadOptions.LoadComponents.All;
    theSession.Parts.LoadOptions.PartLoadOption = NXOpen.LoadOptions.LoadOption.MinimallyLoadLightweightDisplay;
    theSession.Parts.LoadOptions.SetInterpartData(false, NXOpen.LoadOptions.Parent.Partial);
    theSession.Parts.LoadOptions.AllowSubstitution = false;  
    theSession.Parts.LoadOptions.GenerateMissingPartFamilyMembers = true;   
    theSession.Parts.LoadOptions.AbortOnFailure = false; 
    theSession.Parts.LoadOptions.OptionUpdateSubsetOnLoad = NXOpen.LoadOptions.UpdateSubsetOnLoad.None;

    
    //set substructure display part
    BasePart substructurePart = theSession.Parts.FindObject("Substructure");
    NXOpen.PartLoadStatus pls;
    theSession.Parts.SetDisplay(substructurePart, false, false, out pls);
    NXOpen.Display.Camera camera1 = ((NXOpen.Display.Camera)workPart.Cameras.FindObject("Isometric"));
    workPart.ModelingViews.WorkView.Fit();
    
  }
  public static int GetUnloadOption(string dummy) { return (int)NXOpen.Session.LibraryUnloadOption.Immediately; }
}
