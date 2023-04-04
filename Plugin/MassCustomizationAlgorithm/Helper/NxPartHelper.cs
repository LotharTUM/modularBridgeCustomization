using System;
using System.Collections.Generic;
using System.Collections;
using NXOpen;
using NXOpen.PartFamily;
using NXOpen.Assemblies;
using NXOpen.UF;
using ArchBridgeDataModel;
using System.Linq;
using System.Diagnostics;
using NXOpen.Features;
using static NXOpen.Session;

namespace ArchBridgeAlgorithm.Helper
{
    /// <summary>
    /// encapsulates all methods that interfere with the nx part family generator functionalities
    /// </summary>
    public static class NxPartHelper
    {
        /// <summary>
        /// computes and assigns the necessary different knee node module parts, two or three depending on the number of arch segments
        /// </summary>
        public static bool CreateUniqueKneeNodeParts(Substructure substructure, string kneeNodeDir, char prefix)
        {
            try
            {
                // config
                Session session = Session.GetSession();
                NXOpen.Session.UndoMarkId markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Creating knee node parts");
                Session.GetSession().ListingWindow.WriteFullline("Starting to generate unique knee node parts");
                Part modularGroupPart = substructure.ModularGroups.First().Part;
                Part kneeNodeNX;
                PartLoadStatus pls1, pls2;
                try { kneeNodeNX = session.Parts.Open(string.Format("{0}\\KneeNode.prt", kneeNodeDir), out pls1); }
                catch { kneeNodeNX = session.Parts.FindObject("KneeNode") as Part; }
                Matrix3x3 matrix = new Matrix3x3();
                matrix.Xx = 1;
                matrix.Yy = 1;
                matrix.Zz = 1;
                Component kneeNodeNxAsComponent = modularGroupPart.ComponentAssembly.AddComponent(kneeNodeNX, "", "KneeNodeParent", new Point3d(0.0, 0.0, 0.0), matrix, -1, out pls2);
                pls2.Dispose();
                session.Parts.SetWork(kneeNodeNX);
                ErrorList errorList1 = modularGroupPart.ComponentAssembly.ReplaceReferenceSetInOwners("Empty", new Component[] { kneeNodeNxAsComponent });

                //configuration of PartFamily tool
                TemplateManager templateManager = kneeNodeNX.NewPartFamilyTemplateManager();
                templateManager.SaveDirectory = kneeNodeDir;
                if (templateManager.GetPartFamilyTemplate() != null) templateManager.DeletePartFamily();
                string[] attributesToAdd = new string[] { "AngleLowerArchSegment", "AngleHigherArchSegment", "LowerArchSegmentOuterDuct(33)", "LowerArchSegmentInnerDuct(24)", "TRAVERSESUPPORTLOWER", "TRAVERSESUPPORTHIGHER", "VoidingsForTraverseSupportLowerX(52)", "VoidingsForTraverseSupportHigherX(59)" };

                FamilyAttribute.AttrType expressionType = FamilyAttribute.AttrType.Expression;
                FamilyAttribute.AttrType featureType = FamilyAttribute.AttrType.Feature;
                FamilyAttribute.AttrType instanceType = FamilyAttribute.AttrType.Instance;

                FamilyAttribute.AttrType[] attributeTypes = new FamilyAttribute.AttrType[] { expressionType, expressionType, featureType, featureType, instanceType, instanceType, featureType, featureType };
                templateManager.AddToChosenAttributes(attributesToAdd, attributeTypes, 2);
                Template template = templateManager.CreatePartFamily();
                List<FamilyAttribute> familyAttributes = new List<FamilyAttribute>();

                familyAttributes.Add(templateManager.GetPartFamilyAttribute(expressionType, attributesToAdd[0]));
                familyAttributes.Add(templateManager.GetPartFamilyAttribute(expressionType, attributesToAdd[1]));
                familyAttributes.Add(templateManager.GetPartFamilyAttribute(featureType, attributesToAdd[2]));
                familyAttributes.Add(templateManager.GetPartFamilyAttribute(featureType, attributesToAdd[3]));
                familyAttributes.Add(templateManager.GetPartFamilyAttribute(instanceType, attributesToAdd[4]));
                familyAttributes.Add(templateManager.GetPartFamilyAttribute(instanceType, attributesToAdd[5]));
                familyAttributes.Add(templateManager.GetPartFamilyAttribute(featureType, attributesToAdd[6]));
                familyAttributes.Add(templateManager.GetPartFamilyAttribute(featureType, attributesToAdd[7]));



                //// Depending on the bridge having five or seven arch segments, generate two or three unique knee nodes and assign generated parts to unique objects
                List<ModuleKneeNode> kneeNodes = substructure.GetKneeNodes();
                //int kneeNodesPerModularGroup = kneeNodes.Count / 2;
                int kneeNodesPerModularGroup = 4;

                InstanceDefinition[] familyInstances = new InstanceDefinition[4];
                ModuleKneeNode firstUniqueKneeNode = substructure.ModularGroups.First().Arch.KneeNodes.ElementAt(0);
                ModuleKneeNode secondUniqueKneeNode = substructure.ModularGroups.First().Arch.KneeNodes.ElementAt(1);
                ModuleKneeNode thirdUniqueKneeNode = substructure.ModularGroups.First().Arch.KneeNodes.ElementAt(2);
                ModuleKneeNode fourthUniqueKneeNode = substructure.ModularGroups.First().Arch.KneeNodes.ElementAt(3);

                familyInstances[0] = templateManager.AddInstanceDefinition(string.Format("{0}{1}{2}", "KneeNode ", prefix.ToString(), "1"), familyInstances[0], "1");
                familyInstances[0].SetValueOfAttribute(familyAttributes[0], firstUniqueKneeNode.AngleLowerZArchSegment.ToString().Replace(',', '.'));
                familyInstances[0].SetValueOfAttribute(familyAttributes[1], firstUniqueKneeNode.AngleHigherZArchSegment.ToString().Replace(',', '.'));
                familyInstances[0].SetValueOfAttribute(familyAttributes[2], "YES");
                familyInstances[0].SetValueOfAttribute(familyAttributes[3], "NO");
                familyInstances[0].SetValueOfAttribute(familyAttributes[4], "");
                familyInstances[0].SetValueOfAttribute(familyAttributes[5], "TRAVERSESUPPORTHIGHER");
                familyInstances[0].SetValueOfAttribute(familyAttributes[6], "NO");
                familyInstances[0].SetValueOfAttribute(familyAttributes[7], "YES");


                familyInstances[1] = templateManager.AddInstanceDefinition(string.Format("{0}{1}{2}", "KneeNode ", prefix.ToString(), "2"), familyInstances[1], "2");
                familyInstances[1].SetValueOfAttribute(familyAttributes[0], secondUniqueKneeNode.AngleLowerZArchSegment.ToString().Replace(',', '.'));
                familyInstances[1].SetValueOfAttribute(familyAttributes[1], secondUniqueKneeNode.AngleHigherZArchSegment.ToString().Replace(',', '.'));
                familyInstances[1].SetValueOfAttribute(familyAttributes[2], "NO");
                familyInstances[1].SetValueOfAttribute(familyAttributes[3], "YES");
                familyInstances[1].SetValueOfAttribute(familyAttributes[4], "");
                familyInstances[1].SetValueOfAttribute(familyAttributes[5], "TRAVERSESUPPORTHIGHER");
                familyInstances[1].SetValueOfAttribute(familyAttributes[6], "NO");
                familyInstances[1].SetValueOfAttribute(familyAttributes[7], "YES");

                //three and four are symmetric, but will have a traverse console support on the opposite side of 1 and 2 (and will be point-symmetric mirrored for the second modular group)
                familyInstances[2] = templateManager.AddInstanceDefinition(string.Format("{0}{1}{2}", "KneeNode ", prefix.ToString(), "3"), familyInstances[2], "3");
                familyInstances[2].SetValueOfAttribute(familyAttributes[0], secondUniqueKneeNode.AngleLowerZArchSegment.ToString().Replace(',', '.'));
                familyInstances[2].SetValueOfAttribute(familyAttributes[1], secondUniqueKneeNode.AngleHigherZArchSegment.ToString().Replace(',', '.'));
                familyInstances[2].SetValueOfAttribute(familyAttributes[2], "NO");
                familyInstances[2].SetValueOfAttribute(familyAttributes[3], "YES");
                familyInstances[2].SetValueOfAttribute(familyAttributes[4], "TRAVERSESUPPORTLOWER");
                familyInstances[2].SetValueOfAttribute(familyAttributes[5], "");
                familyInstances[2].SetValueOfAttribute(familyAttributes[6], "YES");
                familyInstances[2].SetValueOfAttribute(familyAttributes[7], "NO");


                familyInstances[3] = templateManager.AddInstanceDefinition(string.Format("{0}{1}{2}", "KneeNode ", prefix.ToString(), "4"), familyInstances[3], "4");
                familyInstances[3].SetValueOfAttribute(familyAttributes[0], firstUniqueKneeNode.AngleLowerZArchSegment.ToString().Replace(',', '.'));
                familyInstances[3].SetValueOfAttribute(familyAttributes[1], firstUniqueKneeNode.AngleHigherZArchSegment.ToString().Replace(',', '.'));
                familyInstances[3].SetValueOfAttribute(familyAttributes[2], "YES");
                familyInstances[3].SetValueOfAttribute(familyAttributes[3], "NO");
                familyInstances[3].SetValueOfAttribute(familyAttributes[4], "TRAVERSESUPPORTLOWER");
                familyInstances[3].SetValueOfAttribute(familyAttributes[5], "");
                familyInstances[3].SetValueOfAttribute(familyAttributes[6], "YES");
                familyInstances[3].SetValueOfAttribute(familyAttributes[7], "NO");


                //save and generate parts
                templateManager.SaveFamilyAndCreateMembers(familyInstances);
                string firstPath = string.Format("{0}\\{1}{2}{3}.prt", kneeNodeDir, "KneeNode ", prefix.ToString(), "1");
                string secondPath = string.Format("{0}\\{1}{2}{3}.prt", kneeNodeDir, "KneeNode ", prefix.ToString(), "2");
                string thirdPath = string.Format("{0}\\{1}{2}{3}.prt", kneeNodeDir, "KneeNode ", prefix.ToString(), "3");
                string fourthPath = string.Format("{0}\\{1}{2}{3}.prt", kneeNodeDir, "KneeNode ", prefix.ToString(), "4");

                PartLoadStatus pls3, pls4, pls5, pls6;
                Part firstUniqueKneeNodePart = session.Parts.Open(firstPath, out pls3);
                Part secondUniqueKneeNodePart = session.Parts.Open(secondPath, out pls4);
                Part thirdUniqueKneeNodePart = session.Parts.Open(thirdPath, out pls5);
                Part fourthUniqueKneeNodePart = session.Parts.Open(fourthPath, out pls6);

                pls3.Dispose();
                pls4.Dispose();
                pls5.Dispose();
                pls6.Dispose();

                //assignment
                foreach (ModuleKneeNode kneeNode in substructure.ModularGroups.First().Arch.KneeNodes)
                {
                    //assign family members in sequential order
                    firstUniqueKneeNode.SetPart(firstUniqueKneeNodePart);
                    secondUniqueKneeNode.SetPart(secondUniqueKneeNodePart);
                    thirdUniqueKneeNode.SetPart(thirdUniqueKneeNodePart);
                    fourthUniqueKneeNode.SetPart(fourthUniqueKneeNodePart);
                }

                foreach (ModuleKneeNode kneeNode in substructure.ModularGroups.Last().Arch.KneeNodes)
                {
                    //get objects from second modular group
                    firstUniqueKneeNode = substructure.ModularGroups.Last().Arch.KneeNodes.ElementAt(0);
                    secondUniqueKneeNode = substructure.ModularGroups.Last().Arch.KneeNodes.ElementAt(1);
                    thirdUniqueKneeNode = substructure.ModularGroups.Last().Arch.KneeNodes.ElementAt(2);
                    fourthUniqueKneeNode = substructure.ModularGroups.Last().Arch.KneeNodes.ElementAt(3);
                    //reverse assignment for second modular group (because traverse supports follow point symmetry about center point between the two archs)
                    firstUniqueKneeNode.SetPart(fourthUniqueKneeNodePart);
                    secondUniqueKneeNode.SetPart(thirdUniqueKneeNodePart);
                    thirdUniqueKneeNode.SetPart(secondUniqueKneeNodePart);
                    fourthUniqueKneeNode.SetPart(firstUniqueKneeNodePart);

                }

                //final config
                session.Parts.SetWork(modularGroupPart);
                modularGroupPart.ComponentAssembly.RemoveComponent(kneeNodeNxAsComponent);
                int nErrs1 = session.UpdateManager.DoUpdate(markId);

                return true;
            }
            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown during creation or assignment of unique knee node parts " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// computes and assigns the necessary different arch panel module part files. 
        /// For type 1, just the length must be specified, for type 2, three family instances are generated
        /// </summary>
        public static bool CreateUniqueArchPanelParts(Substructure substructure,  string archModuleDir, char prefix)
        {
            try
            {
                //general config, loading objs and files
                Session session = Session.GetSession();
                PartLoadStatus partLoadStatus;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out partLoadStatus);
                Session.UndoMarkId markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Creation of unique arch panel parts");
                Session.GetSession().ListingWindow.WriteFullline("Starting to generate unique arch panel parts");

                SubsystemModularGroup modularGroupLowerX = substructure.ModularGroups.First();
                SubsystemModularGroup modularGroupHigherX = substructure.ModularGroups.Last();
                Part modularGroupLowerXPart = modularGroupLowerX.Part;
                Part modularGroupHigherXPart = modularGroupHigherX.Part;

                Part panelModuleType1Part, panelModuleType2Part, panelModuleType3Part;
                PartLoadStatus pls1, pls2, pls3;
                try { panelModuleType1Part = session.Parts.Open(string.Format("{0}\\ArchPanel Type1 Support_Panel_OtherPanel.prt", archModuleDir), out pls1); pls1.Dispose(); }
                catch { panelModuleType1Part = session.Parts.FindObject("ArchPanel Type1 Support_Panel_OtherPanel.prt") as Part; }
                try { panelModuleType2Part = session.Parts.Open(string.Format("{0}\\ArchPanel Type2 KneeNode_Panel_OtherPanel.prt", archModuleDir), out pls2); pls2.Dispose(); }
                catch { panelModuleType2Part = session.Parts.FindObject("ArchPanel Type2 KneeNode_Panel_OtherPanel") as Part; }
                try { panelModuleType3Part = session.Parts.Open(string.Format("{0}\\ArchPanel Type3 KneeNode_Panel_OtherPanel.prt", archModuleDir), out pls3); pls3.Dispose(); }
                catch { panelModuleType3Part = session.Parts.FindObject("ArchPanel Type3 KneeNode_Panel_OtherPanel") as Part; }

                /// Set part length for the first arch panel module type
                #region type1
                //Compute the necessary length of the first type, create unique part and assign part to all relevant objects
                ModuleArchPanel archPanelModuleType1LowerYLowerX = modularGroupLowerX.Arch.ArchSegments.First().YOrderedPanels.First();
                ModuleArchPanel archPanelModuleType1HigherYLowerX = modularGroupLowerX.Arch.ArchSegments.Last().YOrderedPanels.Last();
                ModuleArchPanel archPanelModuleType1LowerYHigherY = modularGroupHigherX.Arch.ArchSegments.First().YOrderedPanels.First();
                ModuleArchPanel archPanelModuleType1HigherYHigherX = modularGroupHigherX.Arch.ArchSegments.Last().YOrderedPanels.Last();
                Expression lengthExpressionType1 = panelModuleType1Part.Expressions.FindObject("Length");
                Unit unit1 = panelModuleType1Part.UnitCollection.FindObject("MilliMeter") as Unit;
                panelModuleType1Part.Expressions.EditWithUnits(lengthExpressionType1, unit1, ((double)(archPanelModuleType1LowerYLowerX.Length)).ToString().Replace(",", "."));
                archPanelModuleType1LowerYLowerX.SetPart(panelModuleType1Part);
                archPanelModuleType1HigherYLowerX.SetPart(panelModuleType1Part);
                archPanelModuleType1LowerYHigherY.SetPart(panelModuleType1Part);
                archPanelModuleType1HigherYHigherX.SetPart(panelModuleType1Part);
                #endregion type1

                ///set same length for type 2 and 3 (they just differ in having the tendon/duct more inside or outside the center axis of the rib
                #region type2and3
                ModuleArchPanel moduleWithCharacteristicLength = modularGroupLowerX.Arch.ArchSegments.First().YOrderedPanels.Last();
                Expression lengthExpressionType2 = panelModuleType2Part.Expressions.FindObject("length");
                panelModuleType2Part.Expressions.EditWithUnits(lengthExpressionType2, unit1, ((double)(moduleWithCharacteristicLength.Length)).ToString().Replace(",", "."));
                Expression lengthExpressionType3 = panelModuleType3Part.Expressions.FindObject("length");
                panelModuleType3Part.Expressions.EditWithUnits(lengthExpressionType3, unit1, ((double)(moduleWithCharacteristicLength.Length)).ToString().Replace(",", "."));
                #endregion type2and3

                #region assignment
                /// assign right parts to module objects of substructure assembly graph
                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    int i = 1;
                    foreach (ModuleArchPanel archPanel in modularGroup.GetArchPanels())
                    {
                        if (i == 2 || i==5 || i== 6 || i == 9) archPanel.SetPart(panelModuleType2Part); //outer duct
                        else if (i == 3 || i == 4 || i == 7 || i == 8) archPanel.SetPart(panelModuleType3Part); //inner duct
                    
                        //if (numberOfArchSegments == 7)
                        //{
                        //    if (i == 2 || i == 13) archPanel.SetPart(firstUniqueArchPanelModuleType2Part);
                        //    else if (i == 3 || i == 4 || i == 11 || i == 12) archPanel.SetPart(secondUniqueArchPanelModuleType2Part);
                        //    else if (i == 5 || i == 6 || i == 9 || i == 10) archPanel.SetPart(thirdUniqueArchPanelModuleType2Part);
                        //    else if (i == 7 || i == 8) archPanel.SetPart(secondUniqueArchPanelModuleType2Part);

                        //}
                        i++;
                    }
                }
                #endregion assignment

                ///final configs
                session.Parts.SetWork(substructure.Part);
                PartLoadStatus pls9;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls9);
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;

            }

            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in PartHelper.CreateArchPanelFamily: " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// computes and assigns the necessary different column part files. 
        /// For each pair of columns (in the two modular groups), all elements are adapted in order to be able to cope with variable inclination.
        /// Certainly, some optimization might be done in order to reduce the number of adapted instances of the modules, but this is intricate and would require some further thinking
        /// </summary>
        public static bool CreateUniqueColumnPanelParts(Substructure substructure, string columnDir, char prefix)
        {
            //tbd 5.4: analyze why 5 type 1 get produced and why part 4 does not get inserted data

            try
            {
                #region config
                Session.GetSession().ListingWindow.WriteFullline("Starting to generate unique column panel parts");
                /// general config, loading objs and files
                Session session = Session.GetSession();
                PartLoadStatus partLoadStatus;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out partLoadStatus);
                session.Parts.SetWork(substructure.Part);
                Session.UndoMarkId markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Creation of unique column panel parts");

                //Get the following parts as they are needed to add them to the modular groups(type1, type2, bearing) respectively to first create a family (type3)
                PartLoadStatus pls1, pls2, pls3, pls4, pls5, pls6;
                Part columnPanelType1Part, columnPanelType2Part, columnPanelType3Part, columnPanelType4Part;

                //Column panel type 1
                try { columnPanelType1Part = session.Parts.FindObject("ColumnPanel Type1 KneeNodeOrBoxFoundation_Panel_OtherPanelType2or3") as Part; }
                catch { columnPanelType1Part = session.Parts.Open(string.Format("{0}\\ColumnPanel Type1 KneeNodeOrBoxFoundation_Panel_OtherPanelType2or3.prt", columnDir), out pls1); }

                //Column Panel Type 2
                try { columnPanelType2Part = session.Parts.FindObject("ColumnPanel Type2 PanelType1or2_Panel_PanelType2or3") as Part; }
                catch { columnPanelType2Part = session.Parts.Open(string.Format("{0}\\ColumnPanel Type2 ColumnPanel Type2 OtherPanelType1or2_Panel_Deck.prt", columnDir), out pls2); }

                //Column panel type 3
                try { columnPanelType3Part = session.Parts.FindObject("ColumnPanel Type3 OtherPanelType1or2_Panel_Deck") as Part; }
                catch { columnPanelType3Part = session.Parts.Open(string.Format("{0}\\ColumnPanel Type3 OtherPanelType1or2_Panel_Deck.prt", columnDir), out pls3); }

                //Column panel type 4
                try { columnPanelType4Part = session.Parts.FindObject("ColumnPanel Type4 KneeNode_Panel_Deck") as Part; }
                catch { columnPanelType4Part = session.Parts.Open(string.Format("{0}\\ColumnPanel Type4 KneeNode_Panel_Deck.prt", columnDir), out pls4); }
                #endregion config

                /// Generate family for type 1 
                #region familyType1
                // Get Columns of first and second modular group where type 3 appears 
                List<SubsystemColumn> columnsOfFirstModularGroupType1 = substructure.ModularGroups.First().Columns.Where(c => c.ZOrderedPanels.Count > 1).ToList();
                List<SubsystemColumn> columnsOfSecondModularGroupType1 = substructure.ModularGroups.Last().Columns.Where(c => c.ZOrderedPanels.Count > 1).ToList();

                // To create a part family, we need to temporarily add the part to the current work part
                Matrix3x3 matrix = GeometryHelper.GetUnitMatrix();
                PartLoadStatus plsType1;
                Component panelModuleType1Component = session.Parts.Work.ComponentAssembly.AddComponent(columnPanelType1Part, "", "Column Panel Type1 Parent", new Point3d(0.0, 0.0, 0.0), matrix, -1, out plsType1);
                session.Parts.SetWork(columnPanelType1Part);
                ErrorList errorList1 = session.Parts.Work.ComponentAssembly.ReplaceReferenceSetInOwners("Empty", new Component[] { panelModuleType1Component });

                TemplateManager templateManager1 = columnPanelType1Part.NewPartFamilyTemplateManager();
                templateManager1.SaveDirectory = columnDir;
                if (templateManager1.GetPartFamilyTemplate() != null) templateManager1.DeletePartFamily();
                string[] attributesToAdd1 = new string[] { "length" };
                FamilyAttribute.AttrType[] attributeTypes1 = new FamilyAttribute.AttrType[] { FamilyAttribute.AttrType.Expression };
                templateManager1.AddToChosenAttributes(attributesToAdd1, attributeTypes1, 2);
                Template template1 = templateManager1.CreatePartFamily();
                List<FamilyAttribute> familyAttributes1 = new List<FamilyAttribute>();
                familyAttributes1.Add(templateManager1.GetPartFamilyAttribute(FamilyAttribute.AttrType.Expression, attributesToAdd1[0]));
                InstanceDefinition[] familyInstances1 = new InstanceDefinition[columnsOfFirstModularGroupType1.Count]; //as many instances as geometrically identical pairs of columns

                int i = 0;
                foreach (SubsystemColumn column in columnsOfFirstModularGroupType1)
                {
                    ModuleColumnPanel uniquePanelType1 = column.ZOrderedPanels.Last();
                    familyInstances1[i] = templateManager1.AddInstanceDefinition(string.Format("{0}{1}{2}.prt", "ColumnPanel Type1 KneeNodeOrBoxFoundation_Panel_OtherPanelType2or3 ", prefix.ToString(), (i + 1).ToString()), familyInstances1[i], (i + 1).ToString());
                    familyInstances1[i].SetValueOfAttribute(familyAttributes1[0], uniquePanelType1.Length.ToString().Replace(',', '.'));
                    i++;
                }
                templateManager1.SaveFamilyAndCreateMembers(familyInstances1);

                //config as before
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out plsType1);
                substructure.Part.ComponentAssembly.RemoveComponent(panelModuleType1Component);
                #endregion familyType1

                /// Generate family for type 2 (to be confirmed)
                #region familyType2
                List<SubsystemColumn> columnsOfFirstModularGroupType2 = substructure.ModularGroups.First().Columns.Where(c => c.ZOrderedPanels.Count == 3).ToList();
                List<SubsystemColumn> columnsOfSecondModularGroupType2 = substructure.ModularGroups.Last().Columns.Where(c => c.ZOrderedPanels.Count == 3).ToList();
                int numberOfUniquePanelsType2 = columnsOfFirstModularGroupType2.Count;

                // To create a part family, we need to temporarily add the part to the current work part
                PartLoadStatus plsType2;
                Component panelModuleType2Component = session.Parts.Work.ComponentAssembly.AddComponent(columnPanelType2Part, "", "Column Panel Type2 Parent", new Point3d(0.0, 0.0, 0.0), matrix, -1, out plsType2);
                session.Parts.SetWork(columnPanelType2Part);
                ErrorList errorList2 = session.Parts.Work.ComponentAssembly.ReplaceReferenceSetInOwners("Empty", new Component[] { panelModuleType2Component });

                TemplateManager templateManager2 = columnPanelType2Part.NewPartFamilyTemplateManager();
                templateManager2.SaveDirectory = columnDir;
                if (templateManager2.GetPartFamilyTemplate() != null) templateManager2.DeletePartFamily();
                string[] attributesToAdd2 = new string[] { "length" };
                FamilyAttribute.AttrType[] attributeTypes2 = new FamilyAttribute.AttrType[] { FamilyAttribute.AttrType.Expression };
                templateManager2.AddToChosenAttributes(attributesToAdd2, attributeTypes2, 2);
                Template template2 = templateManager2.CreatePartFamily();
                List<FamilyAttribute> familyAttributes2 = new List<FamilyAttribute>();
                familyAttributes2.Add(templateManager2.GetPartFamilyAttribute(FamilyAttribute.AttrType.Expression, attributesToAdd2[0]));
                InstanceDefinition[] familyInstances2 = new InstanceDefinition[numberOfUniquePanelsType2]; 

                i = 0;
                foreach (SubsystemColumn column in columnsOfFirstModularGroupType2)
                {
                    foreach (ModuleColumnPanel panelType2 in column.ZOrderedPanels.Where(p => p.Type == ColumnPanelTypeSpecification.Type2).ToList())
                    {
                        familyInstances2[i] = templateManager2.AddInstanceDefinition(string.Format("{0}{1}{2}.prt", "ColumnPanel Type2 OtherPanelType1or2_Panel_Deck ", prefix.ToString(), (i + 1).ToString()), familyInstances2[i], (i + 1).ToString());
                        familyInstances2[i].SetValueOfAttribute(familyAttributes2[0], panelType2.Length.ToString().Replace(',', '.'));
                        i++;
                    }
                }
                templateManager2.SaveFamilyAndCreateMembers(familyInstances2);

                //config as before
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out plsType2);
                substructure.Part.ComponentAssembly.RemoveComponent(panelModuleType2Component);
                #endregion familyType2

                /// Generate family for type 3 
                #region familyType3
                // Get Columns of first and second modular group where type 3 appears 
                List<SubsystemColumn> columnsOfFirstModularGroupType3 = substructure.ModularGroups.First().Columns.Where(c => c.ZOrderedPanels.Count > 1).ToList();
                List<SubsystemColumn> columnsOfSecondModularGroupType3 = substructure.ModularGroups.Last().Columns.Where(c => c.ZOrderedPanels.Count > 1).ToList();
                //int numberOfUniquePanelsType3 = columnsOfFirstModularGroupType3.Where(c => c.ZOrderedPanels.Count == 3).ToList().Count;

                // To create a part family, we need to temporarily add the part to the current work part
                PartLoadStatus plsType3;
                Component panelModuleType3Component = session.Parts.Work.ComponentAssembly.AddComponent(columnPanelType3Part, "", "Column Panel Type3 Parent", new Point3d(0.0, 0.0, 0.0), matrix, -1, out plsType3);
                session.Parts.SetWork(columnPanelType3Part);
                ErrorList errorList3 = session.Parts.Work.ComponentAssembly.ReplaceReferenceSetInOwners("Empty", new Component[] { panelModuleType3Component });

                TemplateManager templateManager3 = columnPanelType3Part.NewPartFamilyTemplateManager();
                templateManager3.SaveDirectory = columnDir;
                if (templateManager3.GetPartFamilyTemplate() != null) templateManager3.DeletePartFamily();
                string[] attributesToAdd3 = new string[] { "length" };
                FamilyAttribute.AttrType[] attributeTypes3 = new FamilyAttribute.AttrType[] { FamilyAttribute.AttrType.Expression };
                templateManager3.AddToChosenAttributes(attributesToAdd3, attributeTypes3, 2);
                Template template3 = templateManager3.CreatePartFamily();
                List<FamilyAttribute> familyAttributes3 = new List<FamilyAttribute>();
                familyAttributes3.Add(templateManager3.GetPartFamilyAttribute(FamilyAttribute.AttrType.Expression, attributesToAdd3[0]));
                InstanceDefinition[] familyInstances3 = new InstanceDefinition[columnsOfFirstModularGroupType3.Count]; //as many instances as geometrically identical pairs of columns

                i = 0;
                foreach (SubsystemColumn column in columnsOfFirstModularGroupType3)
                {
                    ModuleColumnPanel uniquePanelType3 = column.ZOrderedPanels.Last();
                    familyInstances3[i] = templateManager3.AddInstanceDefinition(string.Format("{0}{1}{2}.prt", "ColumnPanel Type3 OtherPanelType1or2_Panel_Deck ", prefix.ToString(), (i + 1).ToString()), familyInstances3[i], (i + 1).ToString());
                    familyInstances3[i].SetValueOfAttribute(familyAttributes3[0], uniquePanelType3.Length.ToString().Replace(',', '.'));
                    i++;
                }
                templateManager3.SaveFamilyAndCreateMembers(familyInstances3);

                //config as before
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out plsType3);
                substructure.Part.ComponentAssembly.RemoveComponent(panelModuleType3Component);
                #endregion familyType3


                /// Generate family for type 4 
                #region familyType4
                // Get Columns of first and second modular group where type 4 appears 
                List<SubsystemColumn> columnsOfFirstModularGroupType4 = substructure.ModularGroups.First().Columns.Where(c => c.ZOrderedPanels.Count == 1).ToList();
                List<SubsystemColumn> columnsOfSecondModularGroupType4 = substructure.ModularGroups.Last().Columns.Where(c => c.ZOrderedPanels.Count == 1).ToList();

                if (columnsOfFirstModularGroupType4.Count > 0)
                {
                    PartLoadStatus plsType4;
                    Component panelModuleType4Component = session.Parts.Work.ComponentAssembly.AddComponent(columnPanelType4Part, "", "Column Panel Type4 Parent", new Point3d(0.0, 0.0, 0.0), matrix, -1, out plsType4);
                    session.Parts.SetWork(columnPanelType4Part);
                    ErrorList errorList4 = session.Parts.Work.ComponentAssembly.ReplaceReferenceSetInOwners("Empty", new Component[] { panelModuleType4Component });

                    TemplateManager templateManager4 = columnPanelType4Part.NewPartFamilyTemplateManager();
                    templateManager4.SaveDirectory = columnDir;
                    if (templateManager4.GetPartFamilyTemplate() != null) templateManager4.DeletePartFamily();
                    string[] attributesToAdd4 = new string[] { "length" };
                    FamilyAttribute.AttrType[] attributeTypes4 = new FamilyAttribute.AttrType[] { FamilyAttribute.AttrType.Expression };
                    templateManager4.AddToChosenAttributes(attributesToAdd4, attributeTypes4, 2);
                    Template template4 = templateManager4.CreatePartFamily();
                    List<FamilyAttribute> familyAttributes4 = new List<FamilyAttribute>();
                    familyAttributes4.Add(templateManager4.GetPartFamilyAttribute(FamilyAttribute.AttrType.Expression, attributesToAdd4[0]));
                    InstanceDefinition[] familyInstances4 = new InstanceDefinition[columnsOfSecondModularGroupType4.Count]; //as many instances as geometrically identical pairs of columns

                    i = 0;
                    foreach (SubsystemColumn column in columnsOfFirstModularGroupType4)
                    {
                        ModuleColumnPanel uniquePanelType4 = column.ZOrderedPanels.Last();
                        familyInstances4[i] = templateManager4.AddInstanceDefinition(string.Format("{0}{1}{2}.prt", "ColumnPanel Type4 KneeNode_Panel_Deck ", prefix.ToString(), (i + 1).ToString()), familyInstances4[i], (i + 1).ToString());
                        familyInstances4[i].SetValueOfAttribute(familyAttributes4[0], uniquePanelType4.Length.ToString().Replace(',', '.'));
                        i++;
                    }
                    templateManager4.SaveFamilyAndCreateMembers(familyInstances4);
                    substructure.Part.ComponentAssembly.RemoveComponent(panelModuleType4Component);
                }
                #endregion familyType4


                // assign part files to objects, for each pair of columns.Might be a bit confusing to follow as three levels of model hierarchy are traversed(groups > columns > panels)
                #region assignment
                int idxTotal = 0; //total counter
                int idxType1 = 0; //counter type 3
                int idxType2 = 0; //counter type 4
                int idxType3 = 0; //counter type 3
                int idxType4 = 0; //counter type 4

                foreach (SubsystemColumn singleColumnOfFirstModularGroup in substructure.ModularGroups.First().Columns)
                {
                    //get right colums from groups
                    SubsystemColumn singleColumnOfSecondModularGroup = substructure.ModularGroups.Last().Columns.ElementAt(idxTotal);


                    //open either family member of type 3 or type 4
                    string pathToColumnPanelModuleType1FamilyMember = "";
                    string pathToColumnPanelModuleType2FamilyMember = "";
                    string pathToColumnPanelModuleType3FamilyMember = "";
                    string pathToColumnPanelModuleType4FamilyMember = "";

                    PartLoadStatus plsMemberType1, plsMemberType2, plsMemberType3, plsMemberType4;
                    Part columnPanelType1FamilyMemberPart = session.Parts.Work;
                    Part columnPanelType2FamilyMemberPart = session.Parts.Work;
                    Part columnPanelType3FamilyMemberPart = session.Parts.Work;
                    Part columnPanelType4FamilyMemberPart = session.Parts.Work; //preliminary assignment of arbitrary part just to opress c# compilation error


                    if (singleColumnOfFirstModularGroup.ZOrderedPanels.Where(p => p.Type == ColumnPanelTypeSpecification.Type1).Any())
                    {
                        pathToColumnPanelModuleType1FamilyMember = string.Format("{0}\\{1}{2}{3}.prt", columnDir, "ColumnPanel Type1 KneeNodeOrBoxFoundation_Panel_OtherPanelType2or3 ", prefix.ToString(), (idxType1 + 1).ToString());
                        columnPanelType1FamilyMemberPart = session.Parts.Open(pathToColumnPanelModuleType1FamilyMember, out plsMemberType1);
                        idxType1++;
                    }

                    if (singleColumnOfFirstModularGroup.ZOrderedPanels.Where(p => p.Type == ColumnPanelTypeSpecification.Type2).Any())
                    {
                        pathToColumnPanelModuleType2FamilyMember = string.Format("{0}\\{1}{2}{3}.prt", columnDir, "ColumnPanel Type2 OtherPanelType1or2_Panel_Deck ", prefix.ToString(), (idxType2 + 1).ToString());
                        columnPanelType2FamilyMemberPart = session.Parts.Open(pathToColumnPanelModuleType2FamilyMember, out plsMemberType2);
                        idxType2++;
                    }

                    if (singleColumnOfFirstModularGroup.ZOrderedPanels.Where(p => p.Type == ColumnPanelTypeSpecification.Type3).Any())
                    {
                        pathToColumnPanelModuleType3FamilyMember = string.Format("{0}\\{1}{2}{3}.prt", columnDir, "ColumnPanel Type3 OtherPanelType1or2_Panel_Deck ", prefix.ToString(), (idxType3 + 1).ToString());
                        columnPanelType3FamilyMemberPart = session.Parts.Open(pathToColumnPanelModuleType3FamilyMember, out plsMemberType3);
                        idxType3++;
                    }

                    if (singleColumnOfFirstModularGroup.ZOrderedPanels.Where(p => p.Type == ColumnPanelTypeSpecification.Type4).Any())
                    {
                        pathToColumnPanelModuleType4FamilyMember = string.Format("{0}\\{1}{2}{3}.prt", columnDir, "ColumnPanel Type4 KneeNode_Panel_Deck ", prefix.ToString(), (idxType4 + 1).ToString());
                        columnPanelType4FamilyMemberPart = session.Parts.Open(pathToColumnPanelModuleType4FamilyMember, out plsMemberType4);
                        idxType4++;
                    }

                

                    // set parts in columns
                    for (int k = 0; k < singleColumnOfFirstModularGroup.ZOrderedPanels.Count; k++)
                    {
                        ModuleColumnPanel moduleOfFirstModularGroup = singleColumnOfFirstModularGroup.ZOrderedPanels.ElementAt(k);
                        ModuleColumnPanel moduleOfSecondModularGroup = singleColumnOfSecondModularGroup.ZOrderedPanels.ElementAt(k);

                        if (moduleOfFirstModularGroup.Type == ColumnPanelTypeSpecification.Type1)
                        {
                            moduleOfFirstModularGroup.SetPart(columnPanelType1FamilyMemberPart);
                            moduleOfSecondModularGroup.SetPart(columnPanelType1FamilyMemberPart);
                        }

                        else if (moduleOfFirstModularGroup.Type == ColumnPanelTypeSpecification.Type2)
                        {
                            moduleOfFirstModularGroup.SetPart(columnPanelType2FamilyMemberPart);
                            moduleOfSecondModularGroup.SetPart(columnPanelType2FamilyMemberPart);
                        }

                        else if (moduleOfFirstModularGroup.Type == ColumnPanelTypeSpecification.Type3)
                        {
                            moduleOfFirstModularGroup.SetPart(columnPanelType3FamilyMemberPart);
                            moduleOfSecondModularGroup.SetPart(columnPanelType3FamilyMemberPart);
                        }

                        else if (moduleOfFirstModularGroup.Type == ColumnPanelTypeSpecification.Type4)
                        {
                            moduleOfFirstModularGroup.SetPart(columnPanelType4FamilyMemberPart);
                            moduleOfSecondModularGroup.SetPart(columnPanelType4FamilyMemberPart);
                        }
                    }
                    idxTotal++;
                }

                #endregion assignment


                /// final configs
                session.Parts.SetWork(substructure.Part);
                PartLoadStatus plsSub;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out plsSub);
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;
            }

            catch (Exception e)
            {
                Session session = Session.GetSession();
                int line = new StackTrace(e, true).GetFrame(0).GetFileLineNumber();
                session.ListingWindow.WriteFullline("Exception thrown in PartHelper.CreateColumnPanelFamily in line: " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// creates and assigns tendon parts for the archs  
        /// Individual parts need to be created for each arch segment, every part can be used for eight components
        /// </summary>
        internal static bool CreateUniqueTendonPartsForArchs(Substructure substructure, string tendonDir, char prefix)
        {
            try
            {
                /// general config, loading objs and files
                Session session = Session.GetSession();
                PartLoadStatus pls;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls);
                session.Parts.SetWork(substructure.Part);
                Session.UndoMarkId markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Family generation of tendons for archs");
                session.ListingWindow.WriteFullline("Starting to generate unique tendon part files for archs");
                SubsystemModularGroup firstModularGroup = substructure.ModularGroups.First();
                SubsystemModularGroup secondModularGroup = substructure.ModularGroups.Last();
                int numberOfArchSegments = firstModularGroup.Arch.ArchSegments.Count;
                //Get the part files
                PartLoadStatus pls1, pls2, pls3, pls4, pls5, pls6, pls7;
                Part tendonType1Part;
                //tendon type 1 for lateral arch segments
                try { tendonType1Part = session.Parts.FindObject("Tendon Type1 ArchSegment") as Part; }
                catch { tendonType1Part = session.Parts.Open(string.Format("{0}\\Tendon Type1 ArchSegment.prt", tendonDir), out pls1); }


                /// computations for part generation of type 1. A family needs to be generated with either three respectively four members with every member to be placed 8 times then 
                #region type1Generation
                Matrix3x3 matrix = GeometryHelper.GetUnitMatrix();
                Component tendonType1Component = session.Parts.Work.ComponentAssembly.AddComponent(tendonType1Part, "", "Tendon type1 parent", new Point3d(0.0, 0.0, 0.0), matrix, -1, out pls3);
                session.Parts.SetWork(tendonType1Part);
                ErrorList errorList1 = session.Parts.Work.ComponentAssembly.ReplaceReferenceSetInOwners("Empty", new Component[] { tendonType1Component });

                //family template setup
                TemplateManager templateManager = tendonType1Part.NewPartFamilyTemplateManager();
                templateManager.SaveDirectory = tendonDir;
                if (templateManager.GetPartFamilyTemplate() != null) templateManager.DeletePartFamily();
                string[] attributesToAdd = new string[] { "TendonBaseLength" };
                //string[] attributesToAdd = new string[] { "TendonLength", "TendonExcessForPrestressing" };
                FamilyAttribute.AttrType[] attributeTypes = new FamilyAttribute.AttrType[] {  FamilyAttribute.AttrType.Expression };
                templateManager.AddToChosenAttributes(attributesToAdd, attributeTypes, 2);
                Template template = templateManager.CreatePartFamily();
                List<FamilyAttribute> familyAttributes = new List<FamilyAttribute>();
                familyAttributes.Add(templateManager.GetPartFamilyAttribute(attributeTypes[0], attributesToAdd[0]));

                // population of data for every part family instance
                int numberOfUniqueTendonsType1 = 3; //the first and last, the second and the fourth segment share the same tendon geo
                if (numberOfArchSegments == 7) numberOfUniqueTendonsType1 = 4; //in this case, the third and the fifth share the same tendon geo
                InstanceDefinition[] familyInstances = new InstanceDefinition[numberOfUniqueTendonsType1];
                for (int j = 0; j<numberOfUniqueTendonsType1; j++)
                {
                    Tendon characteristicTendon = firstModularGroup.Arch.ArchSegments.ElementAt(j).Tendons.First();
                    familyInstances[j] = templateManager.AddInstanceDefinition(string.Format("{0}{1}{2}", "Tendon Type1 ArchSegment ", prefix.ToString(), (j+1).ToString() ), familyInstances[j], (j+1).ToString());
                    familyInstances[j].SetValueOfAttribute(familyAttributes[0], characteristicTendon.Length.ToString().Replace(',', '.'));
                }

                ////save and generate parts
                templateManager.SaveFamilyAndCreateMembers(familyInstances);
                #endregion type1Generation

               
                /// assignment of parts 
                #region partAssignment
                //Assignment by looping through the five or seven arch segments
                //Exploiting the symmetry between the 1st and last, the 2nd and 4th and eventually the 3rd and 6th segment
                string pathToFirstTendonType1FamilyInstance="", pathToSecondTendonType1FamilyInstance="", pathToThirdTendonType1FamilyInstance="", pathToFourthTendonType1FamilyInstance="";
                pathToFirstTendonType1FamilyInstance = string.Format("{0}\\{1}{2}{3}.prt", tendonDir, "Tendon Type1 ArchSegment ", prefix.ToString(), "1");
                pathToSecondTendonType1FamilyInstance = string.Format("{0}\\{1}{2}{3}.prt", tendonDir, "Tendon Type1 ArchSegment ", prefix.ToString(), "2");
                pathToThirdTendonType1FamilyInstance = string.Format("{0}\\{1}{2}{3}.prt", tendonDir, "Tendon Type1 ArchSegment ", prefix.ToString(), "3");
                if (numberOfArchSegments == 7)
                {
                    pathToFourthTendonType1FamilyInstance = string.Format("{0}\\{1}{2}{3}.prt", tendonDir, "Tendon Type1 ArchSegment ", prefix.ToString(), "4");
                }
              
                Part firstTendonType1FamilyInstance, secondTendonType1FamilyInstance, thirdTendonType1FamilyInstance, fourthTendonType1FamilyInstance;
                firstTendonType1FamilyInstance = session.Parts.Open(pathToFirstTendonType1FamilyInstance, out pls5);
                secondTendonType1FamilyInstance = session.Parts.Open(pathToSecondTendonType1FamilyInstance, out pls6);
                thirdTendonType1FamilyInstance = session.Parts.Open(pathToThirdTendonType1FamilyInstance, out pls7);
                fourthTendonType1FamilyInstance = thirdTendonType1FamilyInstance; //placeholder;
                if (numberOfArchSegments == 7) fourthTendonType1FamilyInstance = session.Parts.Open(pathToFourthTendonType1FamilyInstance, out pls7);

                int l = 1;
                foreach (SubsystemArchSegment archSegment in firstModularGroup.Arch.ArchSegments)
                {

                    if (numberOfArchSegments == 5)
                    {
                        List<Tendon> tendonsType1OfArchSegmentPair = archSegment.Tendons.Union(secondModularGroup.Arch.ArchSegments.ElementAt(l - 1).Tendons).ToList();
                        if (l == 1 || l == 5) tendonsType1OfArchSegmentPair.ForEach(t => t.SetPart(firstTendonType1FamilyInstance));
                        else if (l == 2 || l == 4) tendonsType1OfArchSegmentPair.ForEach(t => t.SetPart(secondTendonType1FamilyInstance));
                        else if (l == 3)  tendonsType1OfArchSegmentPair.ForEach(t => t.SetPart(thirdTendonType1FamilyInstance));  
                    }
                    else if (numberOfArchSegments == 7)
                    {
                        List<Tendon> tendonsType1OfArchSegmentPair = archSegment.Tendons.Union(secondModularGroup.Arch.ArchSegments.ElementAt(l - 1).Tendons).ToList();
                        if (l == 1 || l == 7) tendonsType1OfArchSegmentPair.ForEach(t => t.SetPart(firstTendonType1FamilyInstance));
                        else if (l == 2 || l == 6) tendonsType1OfArchSegmentPair.ForEach(t => t.SetPart(secondTendonType1FamilyInstance));
                        else if (l == 3 || l == 5) tendonsType1OfArchSegmentPair.ForEach(t => t.SetPart(thirdTendonType1FamilyInstance));
                        else if (l == 4) tendonsType1OfArchSegmentPair.ForEach(t => t.SetPart(fourthTendonType1FamilyInstance));
                    }
                    l++;
                }

                #endregion partAssignment

                /// final config
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls4);
                session.Parts.SetWork(substructure.Part);
                substructure.Part.ComponentAssembly.RemoveComponent(tendonType1Component);
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;
            }

            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in PartHelper.CreateUniqueTendonPartsForArchs: " + e.ToString());
                

                return false;
            }
        }

        /// <summary>
        /// creates and assigns tendon parts for the columns
        /// Individual parts need to be created for pair of column, every part can be used then eight times
        /// </summary>
        internal static bool CreateUniqueTendonPartsForColumns(Substructure substructure, string tendonDir, char prefix)
        {
            try
            {
                ///general config, loading objs and files
                Session session = Session.GetSession();
                PartLoadStatus pls;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls);
                session.Parts.SetWork(substructure.Part);
                Session.UndoMarkId markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Family generation of tendons for columns");
                session.ListingWindow.WriteFullline("Starting to generate unique tendon part files for columns");
                SubsystemModularGroup firstModularGroup = substructure.ModularGroups.First();
                SubsystemModularGroup secondModularGroup = substructure.ModularGroups.Last();
                int numberOfArchSegments = firstModularGroup.Arch.ArchSegments.Count;
                //Get the part files
                PartLoadStatus pls1, pls2, pls3, pls4;
                Part tendonType2Part;
                //tendon type 3 for columns
                try { tendonType2Part = session.Parts.FindObject("Tendon Type2 Column") as Part; }
                catch { tendonType2Part = session.Parts.Open(string.Format("{0}\\Tendon Type2 Column.prt", tendonDir), out pls1); }

                /// computations for part generation of type 2. A family needs to be generated for each column with every member to be placed four times each
                #region type2Generation
                Matrix3x3 matrix = GeometryHelper.GetUnitMatrix();
                Component tendonType2Component = session.Parts.Work.ComponentAssembly.AddComponent(tendonType2Part, "", "Tendon Type2 parent", new Point3d(0.0, 0.0, 0.0), matrix, -1, out pls1);
                session.Parts.SetWork(tendonType2Part);
                ErrorList errorList = session.Parts.Work.ComponentAssembly.ReplaceReferenceSetInOwners("Empty", new Component[] { tendonType2Component });

                //family template setup
                TemplateManager templateManager = tendonType2Part.NewPartFamilyTemplateManager();
                templateManager.SaveDirectory = tendonDir;
                if (templateManager.GetPartFamilyTemplate() != null) templateManager.DeletePartFamily();
                string[] attributesToAdd = new string[] { "TendonBaseLength" };
                FamilyAttribute.AttrType[] attributeTypes = new FamilyAttribute.AttrType[] { FamilyAttribute.AttrType.Expression };
                templateManager.AddToChosenAttributes(attributesToAdd, attributeTypes, 2);
                Template template = templateManager.CreatePartFamily();
                List<FamilyAttribute> familyAttributes = new List<FamilyAttribute>();
                familyAttributes.Add(templateManager.GetPartFamilyAttribute(attributeTypes[0], attributesToAdd[0]));
                int numberOfUniqueTendonsType2 = firstModularGroup.Columns.Where(c => c.ZOrderedPanels.Count > 1).Count();
                InstanceDefinition[] familyInstances = new InstanceDefinition[numberOfUniqueTendonsType2];

                // population of data for every part family instance
                int k = 1;
                foreach (SubsystemColumn column in firstModularGroup.Columns)
                {
                    if (column.ZOrderedPanels.Count > 1)
                    {
                        Tendon characteristicTendon = column.Tendons.First();
                        string name = string.Format("{0}{1}{2}", "Tendon Type2 Column ", prefix.ToString(), k.ToString());
                        familyInstances[k - 1] = templateManager.AddInstanceDefinition(name, familyInstances[k - 1], k.ToString());
                        familyInstances[k - 1].SetValueOfAttribute(familyAttributes[0], characteristicTendon.Length.ToString().Replace(',', '.'));
                        k++;
                    }
                   
                }

                //save and generate parts
                templateManager.SaveFamilyAndCreateMembers(familyInstances);
                #endregion type2Generation

                /// assignment of right types 
                #region partAssignment
                //third part assignment by looping through every pair of columns and simply assigning one instance of the family to each

                int idxTendon = 1;
                for (int m = 0; m<firstModularGroup.Columns.Count; m++)
                {
                    SubsystemColumn columnFirstModularGroup = firstModularGroup.Columns.ElementAt(m);
                    SubsystemColumn columnSecondModularGroup = secondModularGroup.Columns.ElementAt(m);

                    if (columnFirstModularGroup.ZOrderedPanels.Count > 1)
                    {
                        List<Tendon> tendonsType2OfModularGroupLowerX = columnFirstModularGroup.Tendons;
                        List<Tendon> tendonsType2OfModularGroupHigherX = columnSecondModularGroup.Tendons;
                        string pathToTendonType2FamilyInstance = string.Format("{0}\\{1}{2}{3}.prt", tendonDir, "Tendon Type2 Column ", prefix.ToString(), idxTendon.ToString());
                        PartLoadStatus plsTendon;
                        Part tendonType2FamilyInstance = session.Parts.Open(pathToTendonType2FamilyInstance, out plsTendon);
                        tendonsType2OfModularGroupLowerX.ForEach(t => t.SetPart(tendonType2FamilyInstance));
                        tendonsType2OfModularGroupHigherX.ForEach(t => t.SetPart(tendonType2FamilyInstance));
                        idxTendon++;
                    }
                }

                #endregion partAssignment

                //final config
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls2);
                session.Parts.SetWork(substructure.Part);
                substructure.Part.ComponentAssembly.RemoveComponent(tendonType2Component);
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;
            }

            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in PartHelper.CreateUniqueTendonPartsForColumns: " + e.ToString());

                return false;
            }
        }
    }
}
