using System;
using System.Collections.Generic;
using System.Linq;
using NXOpen;
using NXOpen.UF;
using NXOpen.Assemblies;
using NXOpen.Positioning;
using NXOpen.Features;
using NXOpen.GeometricUtilities;
using NXOpen.Assemblies.ProductInterface;
using ArchBridgeDataModel;

namespace ArchBridgeAlgorithm.Helper
{
    /// <summary>
    /// encapsulates all methods that interact with the assembly (at different levels of hierarchy) and the modules at a component-level
    /// </summary>
    public static class NxAssemblyHelper
    {
        /// <summary>
        /// adds the generated knee node parts as components to the modular groups exploiting the symmetry and adapting to 5 or 7 existing arch segments  
        /// </summary>
        public static bool AddKneeNodesToModularGroups(Substructure substructure)
        {
            try
            {
                Session session = Session.GetSession();
                Session.UndoMarkId markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Adding and Orienting KneeNodes");
                session.ListingWindow.WriteFullline("Starting to add and orient knee node modules in modular groups");

                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    //set the modular assembly the active working part and add the corresponding components to the assembly
                    Part modularGroupPart = modularGroup.Part;
                    session.Parts.SetWork(modularGroupPart);
                    Component assemblyRoot = modularGroupPart.ComponentAssembly.RootComponent;
                    List<ModuleKneeNode> kneeNodes = modularGroup.Arch.KneeNodes;


                    foreach (ModuleKneeNode kneeNode in kneeNodes)
                    {
                        AddComponentBuilder addComponentBuilder1 = modularGroupPart.AssemblyManager.CreateAddComponentBuilder();
                        ComponentPositioner componentPositioner1;
                        componentPositioner1 = modularGroupPart.ComponentAssembly.Positioner;
                        componentPositioner1.ClearNetwork();
                        componentPositioner1.BeginAssemblyConstraints();

                        bool allowInterpartPositioning1;
                        allowInterpartPositioning1 = session.Preferences.Assemblies.InterpartPositioning;
                        Network network1 = componentPositioner1.EstablishNetwork();
                        ComponentNetwork componentNetwork1 = ((ComponentNetwork)network1);
                        componentNetwork1.MoveObjectsState = true;
                        Component nullNXOpen_Assemblies_Component = null;
                        componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component;

                        componentNetwork1.MoveObjectsState = true;
                        InterfaceObject nullNXOpen_Assemblies_ProductInterface_InterfaceObject = null;
                        addComponentBuilder1.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject);

                        //positioning
                        Point3d targetPoint = kneeNode.MidPoint;
                        Matrix3x3 orientation = GeometryHelper.GetUnitMatrix();
                        addComponentBuilder1.SetInitialLocationAndOrientation(targetPoint, orientation);
                        addComponentBuilder1.SetScatterOption(true);
                        addComponentBuilder1.SetCount(1);
                        addComponentBuilder1.ReferenceSet = "ALGO";
                        addComponentBuilder1.Layer = -1;
                        BasePart[] partstouse1 = new BasePart[1] { kneeNode.Part };
                        addComponentBuilder1.SetPartsToAdd(partstouse1);
                        InterfaceObject[] productinterfaceobjects1;
                        addComponentBuilder1.GetAllProductInterfaceObjects(out productinterfaceobjects1);
                        componentNetwork1.Solve();
                        componentPositioner1.ClearNetwork();
                        componentPositioner1.EndAssemblyConstraints();
                        NXOpen.PDM.LogicalObject[] logicalobjects1;
                        addComponentBuilder1.GetLogicalObjectsHavingUnassignedRequiredAttributes(out logicalobjects1);
                        addComponentBuilder1.ComponentName = "KneeNode";

                        if (kneeNode.KneeNodeBeforeXZSymmetry == false)
                        {
                            addComponentBuilder1.RotateAlongZDirection();
                            addComponentBuilder1.RotateAlongZDirection();
                        }

                        Component addedAnchorageComponent = addComponentBuilder1.Commit() as Component;
                        kneeNode.SetComponent(addedAnchorageComponent);
                        addComponentBuilder1.Destroy();
                        session.CleanUpFacetedFacesAndEdges();
                    }
                }

                PartLoadStatus pls2;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls2);
                session.Parts.SetWork(substructure.Part);
                pls2.Dispose();
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                session.ListingWindow.WriteFullline("Knee node modules added to both modular groups");
                return true;
            }
            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in AssemblyHelper.AddKneeNodesToAssembly: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Trims the steering sketch at the boundary faces of the kneeNode in order to get precise measures for the arch panels and the column, the measures of the knee node are not known in advance
        /// </summary>
        public static bool TrimSteeringSketchWithKneeNodes(Substructure substructure)
        {
            try
            {
                Session session = Session.GetSession();
                UFSession ufsession = UFSession.GetUFSession();
                NXOpen.Session.UndoMarkId markId = session.SetUndoMark(NXOpen.Session.MarkVisibility.Invisible, "trimming steering sketch at Knee Nodes");
               
                //in order to create a grouped feature
                List<Feature> features = new List<Feature>();
                ExtractFace[] linkedFaces = new ExtractFace[3];

                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    Part modularGroupPart = modularGroup.Part;
                    PartLoadStatus pls5;
                    session.Parts.SetActiveDisplay(modularGroupPart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls5);
                    try
                    {
                        NXOpen.Display.Camera camera4 = (NXOpen.Display.Camera)modularGroupPart.Cameras.FindObject("Isometric");
                        camera4.ApplyToView(modularGroupPart.ModelingViews.WorkView);
                        modularGroupPart.ModelingViews.WorkView.Fit();
                    }
                    catch { } //do not correct the view in case it doesn't work - does not affect the algorithm

                    //wave linker operation
                    List<ModuleKneeNode> kneeNodes = modularGroup.Arch.KneeNodes;
                    foreach (ModuleKneeNode kneeNode in kneeNodes)
                    {
                        //builders
                        Feature nullNXOpen_Features_Feature1 = null;
                        WaveLinkBuilder waveLinkBuilder1 = modularGroupPart.BaseFeatures.CreateWaveLinkBuilder(nullNXOpen_Features_Feature1);
                        waveLinkBuilder1.Type = WaveLinkBuilder.Types.FaceLink;

                        Feature nullNXOpen_Features_Feature2 = null;
                        WaveLinkBuilder waveLinkBuilder2 = modularGroupPart.BaseFeatures.CreateWaveLinkBuilder(nullNXOpen_Features_Feature2);
                        waveLinkBuilder2.Type = WaveLinkBuilder.Types.FaceLink;

                        Feature nullNXOpen_Features_Feature3 = null;
                        WaveLinkBuilder waveLinkBuilder3 = modularGroupPart.BaseFeatures.CreateWaveLinkBuilder(nullNXOpen_Features_Feature3);
                        waveLinkBuilder3.Type = WaveLinkBuilder.Types.FaceLink;

                        ExtractFaceBuilder extractFaceBuilder1 = waveLinkBuilder1.ExtractFaceBuilder;
                        extractFaceBuilder1.FaceOption = ExtractFaceBuilder.FaceOptionType.SingleFace;
                        extractFaceBuilder1.AngleTolerance = 45.0;
                        extractFaceBuilder1.ParentPart = ExtractFaceBuilder.ParentPartType.OtherPart;
                        extractFaceBuilder1.Associative = true;
                        extractFaceBuilder1.MakePositionIndependent = false;
                        extractFaceBuilder1.FixAtCurrentTimestamp = false;
                        extractFaceBuilder1.HideOriginal = false;
                        extractFaceBuilder1.DeleteHoles = false;
                        extractFaceBuilder1.InheritDisplayProperties = false;

                        ExtractFaceBuilder extractFaceBuilder2 = waveLinkBuilder2.ExtractFaceBuilder;
                        extractFaceBuilder2.FaceOption = ExtractFaceBuilder.FaceOptionType.SingleFace;
                        extractFaceBuilder2.AngleTolerance = 45.0;
                        extractFaceBuilder2.ParentPart = ExtractFaceBuilder.ParentPartType.OtherPart;
                        extractFaceBuilder2.Associative = true;
                        extractFaceBuilder2.MakePositionIndependent = false;
                        extractFaceBuilder2.FixAtCurrentTimestamp = false;
                        extractFaceBuilder2.HideOriginal = false;
                        extractFaceBuilder2.DeleteHoles = false;
                        extractFaceBuilder2.InheritDisplayProperties = false;

                        ExtractFaceBuilder extractFaceBuilder3 = waveLinkBuilder3.ExtractFaceBuilder;
                        extractFaceBuilder3.FaceOption = ExtractFaceBuilder.FaceOptionType.SingleFace;
                        extractFaceBuilder3.AngleTolerance = 45.0;
                        extractFaceBuilder3.ParentPart = ExtractFaceBuilder.ParentPartType.OtherPart;
                        extractFaceBuilder3.Associative = true;
                        extractFaceBuilder3.MakePositionIndependent = false;
                        extractFaceBuilder3.FixAtCurrentTimestamp = false;
                        extractFaceBuilder3.HideOriginal = false;
                        extractFaceBuilder3.DeleteHoles = false;
                        extractFaceBuilder3.InheritDisplayProperties = false;

                        // data sources
                        Component componentToBeLinked = kneeNode.Component;
                        var test1 = kneeNode.Part.Bodies.ToArray()[0];
                        var test2= kneeNode.Part.Bodies.ToArray()[1];
                        var test3 = kneeNode.Part.Bodies.ToArray()[2];

                        Body kneeNodeBody = componentToBeLinked.FindOccurrence(test3) as Body;


                        //get faces and link them
                        ExtractFace trimmingFaceFeatureForLowerZSegment = null, trimmingFaceFeatureForColumn = null, trimmingFaceFeatureForHigherZSegment = null;

                        if (kneeNode.KneeNodeBeforeXZSymmetry)
                        {
                            trimmingFaceFeatureForLowerZSegment = kneeNode.Part.Features.ToArray().Where(f => f.Name == "LowerZSegmentTrimmingFace").First() as ExtractFace;
                            trimmingFaceFeatureForColumn = kneeNode.Part.Features.ToArray().Where(f => f.Name == "ColumnTrimmingFace").First() as ExtractFace;
                            trimmingFaceFeatureForHigherZSegment = kneeNode.Part.Features.ToArray().Where(f => f.Name == "HigherZSegmentTrimmingFace").First() as ExtractFace;
                        }
                        else if (!kneeNode.KneeNodeBeforeXZSymmetry)
                        {
                            trimmingFaceFeatureForLowerZSegment = kneeNode.Part.Features.ToArray().Where(f => f.Name == "HigherZSegmentTrimmingFace").First() as ExtractFace;
                            trimmingFaceFeatureForColumn = kneeNode.Part.Features.ToArray().Where(f => f.Name == "ColumnTrimmingFace").First() as ExtractFace;
                            trimmingFaceFeatureForHigherZSegment = kneeNode.Part.Features.ToArray().Where(f => f.Name == "LowerZSegmentTrimmingFace").First() as ExtractFace;
                        }


                        Face trimmingFaceForLowerZSegment = kneeNode.Component.FindOccurrence(trimmingFaceFeatureForLowerZSegment.GetFaces().First()) as Face;
                        Face trimmingFaceForColumn = kneeNode.Component.FindOccurrence(trimmingFaceFeatureForColumn.GetFaces().First()) as Face;
                        Face trimmingFaceForHigherZSegment = kneeNode.Component.FindOccurrence(trimmingFaceFeatureForHigherZSegment.GetFaces().First()) as Face;

                        SelectDisplayableObjectList selectDisplayableObjectList1 = extractFaceBuilder1.ObjectToExtract;
                        SelectDisplayableObjectList selectDisplayableObjectList2 = extractFaceBuilder2.ObjectToExtract;
                        SelectDisplayableObjectList selectDisplayableObjectList3 = extractFaceBuilder3.ObjectToExtract;

                        bool added1 = selectDisplayableObjectList1.Add(trimmingFaceForLowerZSegment);
                        bool added2 = selectDisplayableObjectList2.Add(trimmingFaceForColumn);
                        bool added3 = selectDisplayableObjectList3.Add(trimmingFaceForHigherZSegment);

                        linkedFaces[0] = waveLinkBuilder1.Commit() as ExtractFace;
                        linkedFaces[1] = waveLinkBuilder2.Commit() as ExtractFace;
                        linkedFaces[2] = waveLinkBuilder3.Commit() as ExtractFace;

                        linkedFaces[0].HideBody();
                        linkedFaces[1].HideBody();
                        linkedFaces[2].HideBody();

                        features.AddRange(linkedFaces.ToList());


                        //tstart of trimming
                        Line[] lines = new Line[3];
                        Point3d[] helpPoints = new Point3d[3];

                        lines[0] = kneeNode.ArchSegmentLowerY.SketchGeo;
                        lines[1] = kneeNode.Column.SketchGeo;
                        lines[2] = kneeNode.ArchSegmentHigherY.SketchGeo;

                        helpPoints[0] = GeometryHelper.GetLineCenter(lines[0]);
                        helpPoints[1] = GeometryHelper.GetLineCenter(lines[1]);
                        helpPoints[2] = GeometryHelper.GetLineCenter(lines[2]);


                        for (int j = 0; j < 3; j++)
                        {
                            Line lineToTrim = lines[j];
                            IBaseCurve[] curves1 = new IBaseCurve[1];
                            curves1[0] = lineToTrim;
                            Point3d helpPoint = helpPoints[j];
                            ExtractFace trimFace = linkedFaces[j];

                            //black box process taken from a journal - still error prone
                            TrimCurve2 nullNXOpen_Features_TrimCurve2 = null;
                            TrimCurve2Builder trimCurve2Builder1;
                            trimCurve2Builder1 = modularGroupPart.Features.CreateTrimCurve2FeatureBuilder(nullNXOpen_Features_TrimCurve2);
                            trimCurve2Builder1.MakeInputCurvesDashed = true;
                            TrimCurveBoundingObjectBuilder trimCurveBoundingObjectBuilder1;
                            trimCurveBoundingObjectBuilder1 = trimCurve2Builder1.CreateTrimCurveBoundingObjectBuilder();
                            trimCurveBoundingObjectBuilder1.BoundingObjectMethodType = NXOpen.GeometricUtilities.TrimCurveBoundingObjectBuilder.Method.SelectPlane;
                            Plane nullNXOpen_Plane = null;
                            trimCurveBoundingObjectBuilder1.BoundingPlane = nullNXOpen_Plane;
                            trimCurve2Builder1.BoundingObjectList.Append(trimCurveBoundingObjectBuilder1);
                            trimCurve2Builder1.DirectionOption = NXOpen.Features.TrimCurve2Builder.Direction.AlongDirection;
                            trimCurve2Builder1.CurveOptions.InputCurveOption = NXOpen.GeometricUtilities.CurveOptions.InputCurve.Blank;
                            //trimCurve2Builder1.MakeInputCurvesDashed = false;
                            //trimCurve2Builder1.CurveExtensionOption = NXOpen.Features.TrimCurve2Builder.

                            Point3d origin3 = new NXOpen.Point3d(0.0, 0.0, 0.0);
                            Vector3d vector1 = new NXOpen.Vector3d(0.0, 0.0, 1.0);
                            Direction direction1 = modularGroupPart.Directions.CreateDirection(origin3, vector1, NXOpen.SmartObject.UpdateOption.WithinModeling);
                            trimCurve2Builder1.Vector = direction1;
                            trimCurve2Builder1.CurveToTrim.SetAllowedEntityTypes(NXOpen.Section.AllowTypes.OnlyCurves);
                            SelectionIntentRuleOptions selectionIntentRuleOptions1 = modularGroupPart.ScRuleFactory.CreateRuleOptions();
                            selectionIntentRuleOptions1.SetSelectedFromInactive(false);
                            CurveDumbRule curveDumbRule1 = modularGroupPart.ScRuleFactory.CreateRuleBaseCurveDumb(curves1, selectionIntentRuleOptions1);
                            selectionIntentRuleOptions1.Dispose();
                            trimCurve2Builder1.CurveToTrim.AllowSelfIntersection(true);
                            trimCurve2Builder1.CurveToTrim.AllowDegenerateCurves(false);
                            SelectionIntentRule[] rules1 = new NXOpen.SelectionIntentRule[1];
                            rules1[0] = curveDumbRule1;
                            NXObject nullNXOpen_NXObject = null;
                            trimCurve2Builder1.CurveToTrim.AddToSection(rules1, lineToTrim, nullNXOpen_NXObject, nullNXOpen_NXObject, helpPoint, NXOpen.Section.Mode.Create, false);
                            trimCurveBoundingObjectBuilder1.BoundingObjectMethodType = TrimCurveBoundingObjectBuilder.Method.SelectObject;

                            ScCollector scCollector1 = modularGroupPart.ScCollectors.CreateCollector();
                            scCollector1.SetAllowRefCurves(false);
                            scCollector1.SetAllowedWireframeType(NXOpen.ScCollectorAllowTypes.CurvesAndPoints);
                            SelectionIntentRuleOptions selectionIntentRuleOptions2;
                            selectionIntentRuleOptions2 = modularGroupPart.ScRuleFactory.CreateRuleOptions();
                            selectionIntentRuleOptions2.SetSelectedFromInactive(false);

                            Body body1 = ((Body)modularGroupPart.Bodies.FindObject(trimFace.JournalIdentifier));
                            FaceBodyRule faceBodyRule1;
                            faceBodyRule1 = modularGroupPart.ScRuleFactory.CreateRuleFaceBody(body1, selectionIntentRuleOptions2);
                            selectionIntentRuleOptions2.Dispose();
                            SelectionIntentRule[] rules2 = new SelectionIntentRule[1];
                            rules2[0] = faceBodyRule1;
                            scCollector1.ReplaceRules(rules2, false);

                            SelectDisplayableObjectList selectDisplayableObjectList4;
                            selectDisplayableObjectList4 = trimCurveBoundingObjectBuilder1.BoundingObjectList;
                            selectDisplayableObjectList4.Clear();
                            selectDisplayableObjectList4.Add(scCollector1);
                            trimCurve2Builder1.UpdateTrimRegionsAndDivideLocations();
                            trimCurve2Builder1.ResetTrimRegions();
                            trimCurve2Builder1.SelectTrimRegion(helpPoint);
                            Feature trimFeature = (Feature)trimCurve2Builder1.Commit();

                            //add the trimmed lines for further operations 
                            Line trimmedLine = trimFeature.GetEntities().OfType<Line>().First();

                            if (j == 0)
                            {
                                kneeNode.ArchSegmentLowerY.SetSketchGeo(trimmedLine);
                            }
                            else if (j == 1)
                            {
                                kneeNode.Column.SetSketchGeo(trimmedLine);
                            }
                            else if (j == 2)
                            {
                                kneeNode.ArchSegmentHigherY.SetSketchGeo(trimmedLine);
                            }

                            //to create grouped feature
                            features.Add(trimFeature);

                            //clean up
                            //session.UpdateManager.AddObjectsToDeleteList(new NXObject[] { lineToTrim });
                            trimCurve2Builder1.Destroy();
                        }

                    }
                    Tag featureGroupTag;
                    ufsession.Modl.CreateSetOfFeature("linkingFacesAndTrimmingLInes", features.Select(f => f.Tag).ToArray(), features.Count, 1, out featureGroupTag);
                    int nErrs1 = session.UpdateManager.DoUpdate(markId);
                }
                return true;

            }
            catch (ArgumentException e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in AssemblyHelper.TrimSteeringSketchWithKneeNodes: " + e.Message);
                return false;
            }
        }


        /// <summary>
        /// Adds the arch panels to the modular groups exploiting the point symmetry in every segment. 
        /// </summary>
        public static bool AddArchPanelsToModularGroups(Substructure substructure)
        {
            try
            {
                Session session = Session.GetSession();
                Session.UndoMarkId markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Adding Arch Panels");
                session.ListingWindow.WriteFullline("Starting to add arch panels to modular groups");
                int numberOfArchSegments = substructure.ModularGroups.First().Arch.ArchSegments.Count;


                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    Part modularGroupPart = modularGroup.Part;
                    PartLoadStatus pls1;
                    session.Parts.SetActiveDisplay(modularGroupPart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls1);
                    session.Parts.SetWork(modularGroupPart);
                    Component assemblyRoot = modularGroupPart.ComponentAssembly.RootComponent;

                    int i = 1;
                    foreach (ModuleArchPanel panel in modularGroup.GetArchPanels())
                    {
                        AddComponentBuilder addComponentBuilder1 = modularGroupPart.AssemblyManager.CreateAddComponentBuilder();
                        ComponentPositioner componentPositioner1;
                        componentPositioner1 = modularGroupPart.ComponentAssembly.Positioner;
                        componentPositioner1.ClearNetwork();
                        componentPositioner1.BeginAssemblyConstraints();

                        bool allowInterpartPositioning1;
                        allowInterpartPositioning1 = session.Preferences.Assemblies.InterpartPositioning;
                        Network network1 = componentPositioner1.EstablishNetwork();
                        ComponentNetwork componentNetwork1 = ((ComponentNetwork)network1);
                        componentNetwork1.MoveObjectsState = true;
                        Component nullNXOpen_Assemblies_Component1 = null;
                        componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component1;
                        componentNetwork1.MoveObjectsState = true;
                        InterfaceObject nullNXOpen_Assemblies_ProductInterface_InterfaceObject1 = null;
                        addComponentBuilder1.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject1);
                        BasePart[] partstouse1 = new BasePart[1];
                        Part partToBeAdded1 = panel.Part;
                        partstouse1[0] = partToBeAdded1;
                        addComponentBuilder1.SetPartsToAdd(partstouse1);
                        InterfaceObject[] productinterfaceobjects1;
                        addComponentBuilder1.GetAllProductInterfaceObjects(out productinterfaceobjects1);

                        //positioning
                        Point3d targetPoint = panel.LowerYMidPoint;
                        double rotationAngle = panel.ContainingSegment.InclinationToPositiveY * (-1);
                        // according to the number of segments, some modules need to be placed in at the higher y point ... 
                        if (numberOfArchSegments == 5 && (i == 4 || i == 6 || i == 8 || i == 10) || (numberOfArchSegments == 7 && (i == 4 || i == 6 || i == 8 || i == 10 || i == 12 || i==14)))
                        {
                            targetPoint = panel.HigherYMidPoint;
                        }

                        Matrix3x3 rotationXZ = GeometryHelper.GetRotationMatrixAroundXZ(rotationAngle);

                        addComponentBuilder1.SetInitialLocationAndOrientation(targetPoint, rotationXZ);
                        addComponentBuilder1.SetScatterOption(true);
                        addComponentBuilder1.SetCount(1);
                        addComponentBuilder1.ReferenceSet = "ALGO";
                        addComponentBuilder1.Layer = -1;

                        //...and then mirrored 180° around the z-axis-vector
                        if (numberOfArchSegments == 5 && (i == 4 || i == 6 || i == 8 || i == 10) || (numberOfArchSegments == 7 && (i == 4 || i == 6 || i == 8 || i == 10 || i == 12 || i==14)))
                        {
                            addComponentBuilder1.RotateAlongZDirection();
                            addComponentBuilder1.RotateAlongZDirection();
                        }

                        //execute the addition of the components
                        componentNetwork1.Solve();
                        componentPositioner1.ClearNetwork();
                        componentPositioner1.EndAssemblyConstraints();
                        NXOpen.PDM.LogicalObject[] logicalobjects1;
                        addComponentBuilder1.GetLogicalObjectsHavingUnassignedRequiredAttributes(out logicalobjects1);
                        addComponentBuilder1.ComponentName = "Arch Panel";

                        //assign created components to subsystem assembly graph
                        Component addedPanelComponent = addComponentBuilder1.Commit() as Component;
                        ErrorList errorList1 = modularGroupPart.ComponentAssembly.ReplaceReferenceSetInOwners("ALGO", new Component[] { addedPanelComponent });
                        Component[] nestedComponents = addedPanelComponent.GetChildren();
                        ErrorList errorList = modularGroupPart.ComponentAssembly.ReplaceReferenceSetInOwners("MODEL", nestedComponents);

                        panel.SetComponent(addedPanelComponent);
                        addComponentBuilder1.Destroy();
                        session.CleanUpFacetedFacesAndEdges();
                        i++;
                    }
                }

                PartLoadStatus pls2;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls2);
                session.Parts.SetWork(substructure.Part);
                pls2.Dispose();
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;
            }
            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in AssemblyHelper.AddArchPanelsToAssembly: " + e.ToString());
                return false;
            }
        }



        /// <summary>
        /// Adds the column panel and anchorage components to each modular group. 
        /// </summary>
        public static bool AddColumnPartsToModularGroups(Substructure substructure)
        {
            try
            {
                Session session = Session.GetSession();
                Session.UndoMarkId markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Adding Column Panels");
                session.ListingWindow.WriteFullline("Starting to add column panels to modular groups");

                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    Part modularGroupPart = modularGroup.Part;
                    PartLoadStatus pls1;
                    session.Parts.SetActiveDisplay(modularGroupPart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls1);
                    session.Parts.SetWork(modularGroupPart);
                    Component assemblyRoot = modularGroupPart.ComponentAssembly.RootComponent;

                    foreach (SubsystemColumn column in modularGroup.Columns)
                    {
                        #region panels
                        foreach (ModuleColumnPanel panel in column.ZOrderedPanels)
                        {
                            AddComponentBuilder addComponentBuilder1 = modularGroupPart.AssemblyManager.CreateAddComponentBuilder();
                            ComponentPositioner componentPositioner1;
                            componentPositioner1 = modularGroupPart.ComponentAssembly.Positioner;
                            componentPositioner1.ClearNetwork();
                            componentPositioner1.BeginAssemblyConstraints();

                            bool allowInterpartPositioning1;
                            allowInterpartPositioning1 = session.Preferences.Assemblies.InterpartPositioning;
                            Network network1 = componentPositioner1.EstablishNetwork();
                            ComponentNetwork componentNetwork1 = ((ComponentNetwork)network1);
                            componentNetwork1.MoveObjectsState = true;
                            Component nullNXOpen_Assemblies_Component1 = null;
                            componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component1;
                            componentNetwork1.MoveObjectsState = true;
                            InterfaceObject nullNXOpen_Assemblies_ProductInterface_InterfaceObject1 = null;
                            addComponentBuilder1.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject1);
                            BasePart[] partstouse1 = new BasePart[1];
                            Part partToBeAdded1 = panel.Part;
                            partstouse1[0] = partToBeAdded1;
                            addComponentBuilder1.SetPartsToAdd(partstouse1);
                            InterfaceObject[] productinterfaceobjects1;
                            addComponentBuilder1.GetAllProductInterfaceObjects(out productinterfaceobjects1);

                            //geometric input for placement
                            Point3d targetPoint1 = panel.LowerZMidPoint;
                            Matrix3x3 unit1 = GeometryHelper.GetUnitMatrix();
                            addComponentBuilder1.SetInitialLocationAndOrientation(targetPoint1, unit1);
                            addComponentBuilder1.SetScatterOption(true);
                            addComponentBuilder1.SetCount(1);
                            addComponentBuilder1.ReferenceSet = "ALGO";
                            addComponentBuilder1.Layer = -1;

                            //execute the addition of the components
                            componentNetwork1.Solve();
                            componentPositioner1.ClearNetwork();
                            componentPositioner1.EndAssemblyConstraints();
                            NXOpen.PDM.LogicalObject[] logicalobjects1;
                            addComponentBuilder1.GetLogicalObjectsHavingUnassignedRequiredAttributes(out logicalobjects1);
                            addComponentBuilder1.ComponentName = "Column Panel";

                            //assign created components to subsystem assembly graph
                            Component addedPanelComponent = addComponentBuilder1.Commit() as Component;
                            ErrorList errorList1 = modularGroupPart.ComponentAssembly.ReplaceReferenceSetInOwners("ALGO", new Component[] { addedPanelComponent });
                            Component[] nestedComponents = addedPanelComponent.GetChildren();
                            ErrorList errorList = modularGroupPart.ComponentAssembly.ReplaceReferenceSetInOwners("MODEL", nestedComponents);

                            panel.SetComponent(addedPanelComponent);
                            addComponentBuilder1.Destroy();
                            session.CleanUpFacetedFacesAndEdges();
                        }
                        #endregion panels

                        #region anchorage
                        if (column.Anchorage!= null)
                        {
                            AddComponentBuilder addComponentBuilder2 = modularGroupPart.AssemblyManager.CreateAddComponentBuilder();
                            ComponentPositioner componentPositioner2;
                            componentPositioner2 = modularGroupPart.ComponentAssembly.Positioner;
                            componentPositioner2.ClearNetwork();
                            componentPositioner2.BeginAssemblyConstraints();

                            bool allowInterpartPositioning2;
                            allowInterpartPositioning2 = session.Preferences.Assemblies.InterpartPositioning;
                            Network network2 = componentPositioner2.EstablishNetwork();
                            ComponentNetwork componentNetwork2 = ((ComponentNetwork)network2);
                            componentNetwork2.MoveObjectsState = true;
                            Component nullNXOpen_Assemblies_Component1 = null;
                            componentNetwork2.DisplayComponent = nullNXOpen_Assemblies_Component1;
                            componentNetwork2.MoveObjectsState = true;
                            InterfaceObject nullNXOpen_Assemblies_ProductInterface_InterfaceObject2 = null;
                            addComponentBuilder2.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject2);
                            BasePart[] partstouse2 = new BasePart[1];
                            Part partToBeAdded2 = column.Anchorage.Part;
                            partstouse2[0] = partToBeAdded2;
                            addComponentBuilder2.SetPartsToAdd(partstouse2);
                            InterfaceObject[] productinterfaceobjects2;
                            addComponentBuilder2.GetAllProductInterfaceObjects(out productinterfaceobjects2);

                            //geometric input for placement
                            Point3d targetPoint2 = column.ZOrderedPanels.First().LowerZMidPoint;
                            Matrix3x3 unit2 = GeometryHelper.GetUnitMatrix();
                            addComponentBuilder2.SetInitialLocationAndOrientation(targetPoint2, unit2);
                            addComponentBuilder2.SetScatterOption(true);
                            addComponentBuilder2.SetCount(1);
                            addComponentBuilder2.ReferenceSet = "ALGO";
                            addComponentBuilder2.Layer = -1;

                            //execute the addition of the components
                            componentNetwork2.Solve();
                            componentPositioner2.ClearNetwork();
                            componentPositioner2.EndAssemblyConstraints();
                            NXOpen.PDM.LogicalObject[] logicalobjects2;
                            addComponentBuilder2.GetLogicalObjectsHavingUnassignedRequiredAttributes(out logicalobjects2);
                            addComponentBuilder2.ComponentName = "Column Panel";

                            //assign created components to subsystem assembly graph
                            Component addedAnchorageComponent = addComponentBuilder2.Commit() as Component;
                            //ErrorList errorList2 = modularGroupPart.ComponentAssembly.ReplaceReferenceSetInOwners("ALGO", new Component[] { addedAnchorageComponent });
                            Component[] nestedComponents2 = addedAnchorageComponent.GetChildren();
                            ErrorList errorList2 = modularGroupPart.ComponentAssembly.ReplaceReferenceSetInOwners("MODEL", nestedComponents2);

                            column.Anchorage.SetComponent(addedAnchorageComponent);
                            addComponentBuilder2.Destroy();
                            session.CleanUpFacetedFacesAndEdges();
                        }
                        #endregion anchorage

                    }
                }

                PartLoadStatus pls2;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls2);
                session.Parts.SetWork(substructure.Part);
                pls2.Dispose();
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;
            }
            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in AssemblyHelper.AddColumnPanelsToAssembly: " + e.ToString());
                return false;
            }
        }


        /// <summary>
        /// Adds box foundations to the column groups which are not supported by a knee node
        /// </summary>
        internal static bool AddLateralColumnFoundations(Substructure substructure, string foundationDir)
        {
            try
            {
                /// General Config 
                Session session = NXOpen.Session.GetSession();
                Session.UndoMarkId markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Adding Box Foundations");
                PartLoadStatus pls;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls);
                session.Parts.SetWork(substructure.Part);
                session.ListingWindow.WriteFullline("Starting to add box Foundations to both modular groups");
                Part modularGroupPart = session.Parts.Work;
                Part foundationsPart, boxFoundationPart;

                //loading the needed two parts and the right support objects
                PartLoadStatus pls1, pls2;
                try { foundationsPart = session.Parts.FindObject("Foundations") as Part; }
                catch { foundationsPart = session.Parts.Open(string.Format("{0}\\..\\Foundations.prt", foundationDir), out pls1); }

                try { boxFoundationPart = session.Parts.Open(string.Format("{0}\\BoxFoundation.prt", foundationDir), out pls2); }
                catch { boxFoundationPart = session.Parts.FindObject("BoxFoundation") as Part; }

                Component FoundationComponent = foundationsPart.ComponentAssembly.RootComponent;

                List<Support> boxFoundations = substructure.ModularGroups.First().Supports.Where(s => s.Type == SupportTypeSpecification.type2).ToList();
                boxFoundations.AddRange(substructure.ModularGroups.Last().Supports.Where(s => s.Type == SupportTypeSpecification.type2).ToList());

                foreach (Support boxFoundation in boxFoundations)
                {
                    AddComponentBuilder addComponentBuilder1 = foundationsPart.AssemblyManager.CreateAddComponentBuilder();
                    ComponentPositioner componentPositioner1;
                    componentPositioner1 = foundationsPart.ComponentAssembly.Positioner;
                    componentPositioner1.ClearNetwork();
                    componentPositioner1.BeginAssemblyConstraints();

                    bool allowInterpartPositioning1;
                    allowInterpartPositioning1 = session.Preferences.Assemblies.InterpartPositioning;
                    Network network1 = componentPositioner1.EstablishNetwork();
                    ComponentNetwork componentNetwork1 = ((ComponentNetwork)network1);
                    componentNetwork1.MoveObjectsState = true;
                    Component nullNXOpen_Assemblies_Component = null;
                    componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component;

                    componentNetwork1.MoveObjectsState = true;
                    InterfaceObject nullNXOpen_Assemblies_ProductInterface_InterfaceObject = null;
                    addComponentBuilder1.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject);
                    Matrix3x3 orientation = GeometryHelper.GetUnitMatrix();
                    addComponentBuilder1.SetInitialLocationAndOrientation(boxFoundation.SupportFaceReferencePoint, orientation);
                    addComponentBuilder1.SetCount(1);
                    addComponentBuilder1.Layer = -1;
                    addComponentBuilder1.ReferenceSet = "Use Model";

                    BasePart[] partstouse1 = new BasePart[1];
                    partstouse1[0] = boxFoundationPart;
                    addComponentBuilder1.SetPartsToAdd(partstouse1);
                    InterfaceObject[] productinterfaceobjects1;
                    addComponentBuilder1.GetAllProductInterfaceObjects(out productinterfaceobjects1);

                    componentNetwork1.Solve();
                    componentPositioner1.ClearNetwork();
                    componentPositioner1.EndAssemblyConstraints();

                    NXOpen.PDM.LogicalObject[] logicalobjects1;
                    addComponentBuilder1.GetLogicalObjectsHavingUnassignedRequiredAttributes(out logicalobjects1);
                    addComponentBuilder1.ComponentName = "BoxFoundation";

                    Component addedAnchorageComponent = addComponentBuilder1.Commit() as Component;
                    boxFoundation.SetComponent(addedAnchorageComponent);

                    addComponentBuilder1.Destroy();
                    session.CleanUpFacetedFacesAndEdges();
                }

                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;
            }
            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in AssemblyHelper.AddLateralColumnFoundation: " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Adds tendons to the archs in both modular groups 
        /// </summary>
        public static bool AddTendonsToArchsInModularGroups(Substructure substructure)
        {
            try
            {
                Session session = Session.GetSession();
                NXOpen.Session.UndoMarkId markId = session.SetUndoMark(NXOpen.Session.MarkVisibility.Visible, "Adding tendons to archs");
                session.ListingWindow.WriteFullline("Starting to add tendon components to archs in both modular groups");

                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    Part modularGroupPart = modularGroup.Part;
                    PartLoadStatus pls1;
                    session.Parts.SetActiveDisplay(modularGroupPart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls1);
                    session.Parts.SetWork(modularGroupPart);
                    Component assemblyRoot = modularGroupPart.ComponentAssembly.RootComponent;

                    List<Tendon> tendons = modularGroup.GetTendons().Where(t => t.Type == TendonTypeSpecification.Type1).ToList();
                    if (tendons.Count > 0)
                    {
                        foreach (Tendon tendon in tendons)
                        {
                            AddComponentBuilder addComponentBuilder1 = modularGroupPart.AssemblyManager.CreateAddComponentBuilder();
                            ComponentPositioner componentPositioner1;
                            componentPositioner1 = modularGroupPart.ComponentAssembly.Positioner;
                            componentPositioner1.ClearNetwork();
                            componentPositioner1.BeginAssemblyConstraints();

                            bool allowInterpartPositioning1;
                            allowInterpartPositioning1 = session.Preferences.Assemblies.InterpartPositioning;
                            Network network1 = componentPositioner1.EstablishNetwork();
                            ComponentNetwork componentNetwork1 = ((ComponentNetwork)network1);
                            componentNetwork1.MoveObjectsState = true;
                            Component nullNXOpen_Assemblies_Component = null;
                            componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component;

                            componentNetwork1.MoveObjectsState = true;
                            InterfaceObject nullNXOpen_Assemblies_ProductInterface_InterfaceObject = null;
                            addComponentBuilder1.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject);

                            Point3d targetPoint = tendon.LowerMidPoint;
                            double rotationAngle = tendon.ArchSegmentJoined.InclinationToPositiveY * (-1);
                            if (!tendon.ArchSegmentJoined.BeforeXZSymmetryPlane)
                            {
                                addComponentBuilder1.RotateAlongZDirection();
                                addComponentBuilder1.RotateAlongZDirection();
                            }
                            Matrix3x3 rotationXZ = GeometryHelper.GetRotationMatrixAroundXZ(rotationAngle);

                            addComponentBuilder1.SetInitialLocationAndOrientation(targetPoint, rotationXZ);
                            addComponentBuilder1.SetScatterOption(true);
                            addComponentBuilder1.SetCount(1);
                            addComponentBuilder1.ReferenceSet = "MODEL";
                            addComponentBuilder1.Layer = -1;

                            BasePart[] partstouse1 = new BasePart[1];
                            Part part1 = tendon.Part;
                            partstouse1[0] = part1;
                            addComponentBuilder1.SetPartsToAdd(partstouse1);
                            InterfaceObject[] productinterfaceobjects1;
                            addComponentBuilder1.GetAllProductInterfaceObjects(out productinterfaceobjects1);
                            componentNetwork1.Solve();
                            componentPositioner1.ClearNetwork();
                            componentPositioner1.EndAssemblyConstraints();
                            NXOpen.PDM.LogicalObject[] logicalobjects1;
                            addComponentBuilder1.GetLogicalObjectsHavingUnassignedRequiredAttributes(out logicalobjects1);
                            addComponentBuilder1.ComponentName = "Tendon";
                            Component addedAnchorageComponent = addComponentBuilder1.Commit() as Component;
                            tendon.SetComponent(addedAnchorageComponent);
                            addComponentBuilder1.Destroy();
                            session.CleanUpFacetedFacesAndEdges();
                        }
                    }
                }

                PartLoadStatus pls2;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls2);
                session.Parts.SetWork(substructure.Part);
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;
            }
            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in AssemblyHelper.AddTendonsToArchsInModularGroups: " + e.ToString());
                return false;
            }
        }


        /// <summary>
        /// Adds tendons to the columns in both modular groups
        /// </summary>
        public static bool AddTendonsToColumnsInModularGroups(Substructure substructure)
        {
            try
            {
                Session session = Session.GetSession();
                NXOpen.Session.UndoMarkId markId = session.SetUndoMark(NXOpen.Session.MarkVisibility.Visible, "Adding tendons to columns");
                session.ListingWindow.WriteFullline("Starting to add tendon components to columns in both modular groups");
                PartLoadStatus plsSub;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out plsSub);
                session.Parts.SetWork(substructure.Part);

                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    Part modularGroupPart = modularGroup.Part;
                    PartLoadStatus plsMod;
                    session.Parts.SetActiveDisplay(modularGroupPart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out plsMod);
                    session.Parts.SetWork(modularGroupPart);
                    Component assemblyRoot = modularGroupPart.ComponentAssembly.RootComponent;

                    foreach (SubsystemColumn column in modularGroup.Columns)
                    {
                        if (column.Tendons.Count > 0)
                        {
                            List<Tendon> tendons = column.Tendons;
                            foreach (Tendon tendon in tendons)
                            {
                                AddComponentBuilder addComponentBuilder1 = modularGroupPart.AssemblyManager.CreateAddComponentBuilder();
                                ComponentPositioner componentPositioner1;
                                componentPositioner1 = modularGroupPart.ComponentAssembly.Positioner;
                                componentPositioner1.ClearNetwork();
                                componentPositioner1.BeginAssemblyConstraints();

                                bool allowInterpartPositioning1;
                                allowInterpartPositioning1 = session.Preferences.Assemblies.InterpartPositioning;
                                Network network1 = componentPositioner1.EstablishNetwork();
                                ComponentNetwork componentNetwork1 = ((ComponentNetwork)network1);
                                componentNetwork1.MoveObjectsState = true;
                                Component nullNXOpen_Assemblies_Component = null;
                                componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component;

                                componentNetwork1.MoveObjectsState = true;
                                InterfaceObject nullNXOpen_Assemblies_ProductInterface_InterfaceObject = null;
                                addComponentBuilder1.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject);

                                Point3d targetPoint = tendon.LowerMidPoint;
                                double rotationAngle = 0.0;
                                rotationAngle = -90.0;
                                Matrix3x3 rotationXZ = GeometryHelper.GetRotationMatrixAroundXZ(rotationAngle);

                                addComponentBuilder1.SetInitialLocationAndOrientation(targetPoint, rotationXZ);
                                addComponentBuilder1.SetScatterOption(true);
                                addComponentBuilder1.SetCount(1);
                                addComponentBuilder1.ReferenceSet = "MODEL";
                                addComponentBuilder1.Layer = -1;

                                BasePart[] partstouse1 = new BasePart[1];
                                Part part1 = tendon.Part;
                                partstouse1[0] = part1;
                                addComponentBuilder1.SetPartsToAdd(partstouse1);
                                InterfaceObject[] productinterfaceobjects1;
                                addComponentBuilder1.GetAllProductInterfaceObjects(out productinterfaceobjects1);
                                componentNetwork1.Solve();
                                componentPositioner1.ClearNetwork();
                                componentPositioner1.EndAssemblyConstraints();
                                NXOpen.PDM.LogicalObject[] logicalobjects1;
                                addComponentBuilder1.GetLogicalObjectsHavingUnassignedRequiredAttributes(out logicalobjects1);
                                addComponentBuilder1.ComponentName = "Tendon";
                                Component addedAnchorageComponent = addComponentBuilder1.Commit() as Component;
                                tendon.SetComponent(addedAnchorageComponent);
                                addComponentBuilder1.Destroy();
                                session.CleanUpFacetedFacesAndEdges();
                            }
                        }
                    }
                }

                PartLoadStatus pls2;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls2);
                session.Parts.SetWork(substructure.Part);
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;
            }
            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in AssemblyHelper.AddTendonsToColumnsInModularGroups: " + e.ToString());
                return false;
            }
        }


        /// <summary>
        /// Adds traverses
        /// </summary>
        public static bool AddTraverses(Substructure substructure, Part traversesGroup)
        {
            Session session = Session.GetSession();
            Session.UndoMarkId markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Adding and orienting traverses");
            PartLoadStatus pls1, pls2;
            try
            {
                //set the modular assembly the active working part and add the corresponding components to the assembly
                session.Parts.SetActiveDisplay(traversesGroup, DisplayPartOption.AllowAdditional, PartDisplayPartWorkPartOption.SameAsDisplay, out pls1);
                session.Parts.SetWork(traversesGroup);

                Component assemblyRoot = traversesGroup.ComponentAssembly.RootComponent;
                Part traversePart;
                try { traversePart = session.Parts.FindObject("Traverse") as Part; }
                catch
                {
                    traversePart = session.Parts.Open("Traverse", out pls2);
                };


                foreach (ModuleTraverse traverse in substructure.Traverses)
                {
                    AddComponentBuilder addComponentBuilder1 = traversesGroup.AssemblyManager.CreateAddComponentBuilder();
                    ComponentPositioner componentPositioner1;
                    componentPositioner1 = traversesGroup.ComponentAssembly.Positioner;
                    componentPositioner1.ClearNetwork();
                    componentPositioner1.BeginAssemblyConstraints();

                    bool allowInterpartPositioning1;
                    allowInterpartPositioning1 = session.Preferences.Assemblies.InterpartPositioning;
                    Network network1 = componentPositioner1.EstablishNetwork();
                    ComponentNetwork componentNetwork1 = ((ComponentNetwork)network1);
                    componentNetwork1.MoveObjectsState = true;
                    Component nullNXOpen_Assemblies_Component = null;
                    componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component;

                    componentNetwork1.MoveObjectsState = true;
                    InterfaceObject nullNXOpen_Assemblies_ProductInterface_InterfaceObject = null;
                    addComponentBuilder1.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject);

                    //positioning
                    Point3d targetPoint = traverse.LowerXReferencePoint;
                    Matrix3x3 orientation = GeometryHelper.GetUnitMatrix();
                    Point3d referencePoint = new Point3d(targetPoint.X, targetPoint.Y - 2913.5, targetPoint.Z + 6182.3); //hardcoded for now due to some incomprehensible offset... 
                    addComponentBuilder1.SetInitialLocationAndOrientation(referencePoint, orientation);
                    addComponentBuilder1.SetScatterOption(true);
                    addComponentBuilder1.SetCount(1);
                    addComponentBuilder1.ReferenceSet = "ALGO";
                    addComponentBuilder1.Layer = -1;
                    BasePart[] partstouse1 = new BasePart[1] { traversePart };
                    addComponentBuilder1.SetPartsToAdd(partstouse1);
                    InterfaceObject[] productinterfaceobjects1;
                    addComponentBuilder1.GetAllProductInterfaceObjects(out productinterfaceobjects1);
                    componentNetwork1.Solve();
                    componentPositioner1.ClearNetwork();
                    componentPositioner1.EndAssemblyConstraints();
                    NXOpen.PDM.LogicalObject[] logicalobjects1;
                    addComponentBuilder1.GetLogicalObjectsHavingUnassignedRequiredAttributes(out logicalobjects1);
                    addComponentBuilder1.ComponentName = "Traverse";

                    Component addedTraverseComponent = addComponentBuilder1.Commit() as Component;
                    traverse.SetComponent(addedTraverseComponent);
                    addComponentBuilder1.Destroy();
                    session.CleanUpFacetedFacesAndEdges();
                }


                PartLoadStatus pls3;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls3);
                session.Parts.SetWork(substructure.Part);
                pls1.Dispose();
                pls3.Dispose();
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;

            }
            catch (Exception e)
            {
                session.ListingWindow.WriteFullline("Exception thrown in AssemblyHelper.AddTraverses: " + e.Message);
                return false;
            }
        }


        /// <summary>
        /// Collects knee node, panels and tendon components and groups them into arch subgroups in order to create a more comprehensible model 
        /// </summary>
        internal static bool GroupArchs(Substructure substructure, string containerDir, char fileNamePrefix)
        {
            try
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Starting to group archs in nx model structure");
                Session.UndoMarkId markId = session.SetUndoMark(NXOpen.Session.MarkVisibility.Visible, "Grouping Archs");

                int j = 1;
                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    if (modularGroup.Arch.GetArchPanels().Select(p => p.Component).ToList().Count > 0)
                    {

                        Part modularGroupPart = modularGroup.Part;
                        session.Parts.SetWork(modularGroupPart);
                        PartLoadStatus pls1, pls2;
                        //Arch
                        //usual builder objects
                        AddComponentBuilder addComponentBuilder1 = modularGroupPart.AssemblyManager.CreateAddComponentBuilder();
                        ComponentPositioner componentPositioner1 = modularGroupPart.ComponentAssembly.Positioner;
                        componentPositioner1.ClearNetwork();
                        bool allowInterpartPositioning1 = session.Preferences.Assemblies.InterpartPositioning;
                        Network network1 = componentPositioner1.EstablishNetwork();
                        ComponentNetwork componentNetwork1 = ((ComponentNetwork)network1);
                        componentNetwork1.MoveObjectsState = true;
                        Component nullNXOpen_Assemblies_Component = null;
                        componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component;
                        componentNetwork1.MoveObjectsState = true;
                        InterfaceObject nullNXOpen_Assemblies_ProductInterface_InterfaceObject = null;
                        addComponentBuilder1.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject);
                        addComponentBuilder1.SetInitialLocationType(AddComponentBuilder.LocationType.WorkPartAbsolute);
                        addComponentBuilder1.SetCount(1);
                        addComponentBuilder1.SetScatterOption(true);

                        //open part
                        BasePart[] partstouse1 = new BasePart[1];
                        Part archContainerPart;
                        try { archContainerPart = session.Parts.FindObject("Arch") as Part; }
                        catch { archContainerPart = session.Parts.Open(string.Format("{0}\\Arch.prt", containerDir), out pls1); }
                        partstouse1[0] = archContainerPart;

                        //set builder attributes
                        addComponentBuilder1.SetPartsToAdd(partstouse1);
                        componentNetwork1.Solve();
                        componentPositioner1.ClearNetwork();
                        int nErrs1 = session.UpdateManager.AddToDeleteList(componentNetwork1);
                        componentPositioner1.EndAssemblyConstraints();
                        NXOpen.PDM.LogicalObject[] logicalobjects2;
                        addComponentBuilder1.GetLogicalObjectsHavingUnassignedRequiredAttributes(out logicalobjects2);
                        Component archContainerComponentGeneric = addComponentBuilder1.Commit() as Component;
                        string componentName = string.Format("Arch {0}{1}", fileNamePrefix, j.ToString());
                        archContainerComponentGeneric.SetName(componentName);
                        addComponentBuilder1.GetOperationFailures();
                        addComponentBuilder1.Destroy();

                        //make container part unique
                        MakeUniquePartBuilder makeUniquePartBuilder1 = modularGroupPart.AssemblyManager.CreateMakeUniquePartBuilder();
                        bool added1 = makeUniquePartBuilder1.SelectedComponents.Add(archContainerComponentGeneric);
                        string path = string.Format("{0}\\{1}.prt", containerDir, componentName);
                        archContainerPart.SetMakeUniqueName(path);
                        var obj = makeUniquePartBuilder1.Commit();
                        makeUniquePartBuilder1.SelectedComponents.Remove(archContainerComponentGeneric);
                        Component archContainerComponentUnique = modularGroupPart.ComponentAssembly.RootComponent.GetChildren().Where(c => c.DisplayName == componentName).First();
                        Part archContainerPartUnique = session.Parts.ToArray().Where(p => p.Name == componentName).First() as Part;

                        //add components 
                        List<Component> origComponents = new List<Component>();
                        origComponents.AddRange(modularGroup.Arch.GetArchPanels().Select(p => p.Component).ToList());
                        origComponents.AddRange(modularGroup.Arch.KneeNodes.Select(k => k.Component).ToList());
                        origComponents.AddRange(modularGroup.GetTendons().Where(t => t.Type == TendonTypeSpecification.Type1).Select(t => t.Component));
                        Component[] newComponents = new Component[origComponents.Count];
                        ErrorList errorList1;
                        archContainerPartUnique.ComponentAssembly.RestructureComponents(origComponents.ToArray(), archContainerComponentUnique, true, out newComponents, out errorList1);
                        errorList1.Dispose();
                    }
                    j++;
                }
                int nErrs2 = session.UpdateManager.DoUpdate(markId);

                //index for naming 
                PartLoadStatus pls;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls);
                session.Parts.SetWork(substructure.Part);
                return true;
            }

            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in AssemblyHelper.GroupArchAndColumns: " + e.Message);
              
                return false;
            }

        }

        /// <summary>
        /// Collects panels, bearing and tendon components and groups them into column subgroups in order to create a more comprehensible model 
        /// </summary>
        internal static bool GroupColumns(Substructure substructure, string containerDir, char fileNamePrefix)
        {
            try
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Starting to group columns in nx model structure");
                Session.UndoMarkId markId = session.SetUndoMark(NXOpen.Session.MarkVisibility.Visible, "Grouping Columns");

                int j = 1;
                int i = 1; 
                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    Part modularGroupPart = modularGroup.Part;
                    session.Parts.SetWork(modularGroupPart);
                    PartLoadStatus pls1, pls2;
                    //Arch
                    //usual builder objects
                    AddComponentBuilder addComponentBuilder1 = modularGroupPart.AssemblyManager.CreateAddComponentBuilder();
                    ComponentPositioner componentPositioner1 = modularGroupPart.ComponentAssembly.Positioner;
                    componentPositioner1.ClearNetwork();
                    bool allowInterpartPositioning1 = session.Preferences.Assemblies.InterpartPositioning;
                    Network network1 = componentPositioner1.EstablishNetwork();
                    ComponentNetwork componentNetwork1 = ((ComponentNetwork)network1);
                    componentNetwork1.MoveObjectsState = true;
                    Component nullNXOpen_Assemblies_Component = null;
                    componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component;
                    componentNetwork1.MoveObjectsState = true;
                    InterfaceObject nullNXOpen_Assemblies_ProductInterface_InterfaceObject = null;
                    addComponentBuilder1.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject);
                    addComponentBuilder1.SetInitialLocationType(AddComponentBuilder.LocationType.WorkPartAbsolute);
                    addComponentBuilder1.SetCount(1);
                    addComponentBuilder1.SetScatterOption(true);

                    //columns
                    MakeUniquePartBuilder makeUniquePartBuilder2 = modularGroupPart.AssemblyManager.CreateMakeUniquePartBuilder();
                    foreach (SubsystemColumn column in modularGroup.Columns)
                    {
                        if (column.ZOrderedPanels.Count > 0)
                        {
                            PartLoadStatus pls3;
                            addComponentBuilder1 = modularGroupPart.AssemblyManager.CreateAddComponentBuilder();
                            componentPositioner1 = modularGroupPart.ComponentAssembly.Positioner;
                            componentPositioner1.ClearNetwork();
                            allowInterpartPositioning1 = session.Preferences.Assemblies.InterpartPositioning;

                            componentNetwork1 = componentPositioner1.EstablishNetwork() as ComponentNetwork;
                            componentNetwork1.MoveObjectsState = true;
                            nullNXOpen_Assemblies_Component = null;
                            componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component;
                            componentNetwork1.MoveObjectsState = true;
                            nullNXOpen_Assemblies_ProductInterface_InterfaceObject = null;
                            addComponentBuilder1.SetComponentAnchor(nullNXOpen_Assemblies_ProductInterface_InterfaceObject);
                            addComponentBuilder1.SetInitialLocationType(NXOpen.Assemblies.AddComponentBuilder.LocationType.WorkPartAbsolute);
                            addComponentBuilder1.SetCount(1);
                            addComponentBuilder1.SetScatterOption(true);
                            BasePart[] partstouse1 = new BasePart[1];
                            Part columnContainerPart;
                            try { columnContainerPart = session.Parts.FindObject("Column") as Part; }
                            catch { columnContainerPart = session.Parts.Open(string.Format("{0}\\Column.prt", containerDir), out pls3); }

                            partstouse1[0] = columnContainerPart;
                            addComponentBuilder1.SetPartsToAdd(partstouse1);
                            InterfaceObject[] productinterfaceobjects2;
                            addComponentBuilder1.GetAllProductInterfaceObjects(out productinterfaceobjects2);
                            componentNetwork1.Solve();
                            componentPositioner1.ClearNetwork();
                            int nErrs1 = session.UpdateManager.AddToDeleteList(componentNetwork1);
                            componentPositioner1.EndAssemblyConstraints();

                            NXOpen.PDM.LogicalObject[] logicalobjects1;
                            addComponentBuilder1.GetLogicalObjectsHavingUnassignedRequiredAttributes(out logicalobjects1);
                            Component columnContainerComponentGeneric = addComponentBuilder1.Commit() as Component;
                            string componentName = string.Format("Column {0}{1}", fileNamePrefix, i.ToString());
                            columnContainerComponentGeneric.SetName(componentName);
                            addComponentBuilder1.GetOperationFailures();
                            addComponentBuilder1.Destroy();

                            bool added1 = makeUniquePartBuilder2.SelectedComponents.Add(columnContainerComponentGeneric);
                            string path = string.Format("{0}\\{1}.prt", containerDir, componentName);
                            columnContainerPart.SetMakeUniqueName(path);
                            NXObject obj = makeUniquePartBuilder2.Commit();
                            makeUniquePartBuilder2.SelectedComponents.Remove(columnContainerComponentGeneric);
                            Component columnContainerComponentUnique = modularGroupPart.ComponentAssembly.RootComponent.GetChildren().Where(c => c.DisplayName == componentName).First();
                            Part columnContainerPartUnique = session.Parts.ToArray().Where(p => p.Name == componentName).First() as Part;

                            List<Component> origComponents2 = new List<Component>();
                            if (column.ZOrderedPanels.Count>0) origComponents2.AddRange(column.ZOrderedPanels.Select(m => m.Component).ToList());
                            if (column.Tendons.Count > 0) origComponents2.AddRange(column.Tendons.Select(t => t.Component).ToList());

                            Component[] newComponents2 = new Component[origComponents2.Count];
                            ErrorList errorList2;
                            columnContainerPartUnique.ComponentAssembly.RestructureComponents(origComponents2.ToArray(), columnContainerComponentUnique, true, out newComponents2, out errorList2);
                            //For some reason, a component named "1" remains in the column assembly after grouping sometimes - didnt find the reason in the prior code and thus remove it like that.
                            Component remnant = columnContainerPartUnique.ComponentAssembly.RootComponent.GetChildren().Where(n => n.DisplayName == "1").First();
                            if (remnant != null) { columnContainerPartUnique.ComponentAssembly.RemoveComponent(remnant); }
                            errorList2.Dispose();
                            i++;
                        }
                    }

                    int nErrs2 = session.UpdateManager.DoUpdate(markId);

                    //index for naming 
                    j++;
                }
                PartLoadStatus pls;
                session.Parts.SetWork(substructure.Part);
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls);
                return true;
            }

            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in AssemblyHelper.GroupColumns: " + e.Message);
                return false;
            }
        }
    }
}
