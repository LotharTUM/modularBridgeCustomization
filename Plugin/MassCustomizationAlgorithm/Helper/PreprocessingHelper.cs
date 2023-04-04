using NXOpen;
using NXOpen.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchBridgeAlgorithm.Helper
{
    /// <summary>
    /// encapsulates the necessary preprocessing of the steering sketch in the substructure part and the associative import into the modular group parts
    /// </summary>
    public static class PreprocessingHelper
    {
        public static bool TrimArchColumnsOfModularGroup(Part substructurePart, Part modularGroupPart)
        {
            try
            {
                Session session = Session.GetSession();
                session.Parts.SetWork(substructurePart);
                PartLoadStatus pls;
                session.Parts.SetActiveDisplay(substructurePart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls);
                try
                {
                    NXOpen.Display.Camera camera4 = (NXOpen.Display.Camera)modularGroupPart.Cameras.FindObject("Isometric");
                    camera4.ApplyToView(modularGroupPart.ModelingViews.WorkView);
                    modularGroupPart.ModelingViews.WorkView.Fit();
                }
                catch { } //do not correct the view in case it doesn't work - does not affect the algorithm

                Session.UndoMarkId markId = session.SetUndoMark(NXOpen.Session.MarkVisibility.Visible, "Trim arch columns for " + modularGroupPart.Name);
                session.ListingWindow.WriteFullline("Starting to trim the arch columns in the steering sketch of the substructure part");

                //get input data from substructure part file
                DatumPlane trimmingPlaneLowerY = (substructurePart.Features.ToArray().Where(f => f.Name == "TrimmingPlaneForColumnLowerY").First()).GetEntities().OfType<DatumPlane>().First();
                DatumPlane trimmingPlaneHigherY = (substructurePart.Features.ToArray().Where(f => f.Name == "TrimmingPlaneForColumnHigherY").First()).GetEntities().OfType<DatumPlane>().First();
                ProjectCurve untrimmedColumns = null; //assign depending on modular group
                if (modularGroupPart.Name == "ModularGroupLowerX") untrimmedColumns = substructurePart.Features.ToArray().Where(f => f.Name == "ArchColumnsToBeTrimmedLowerX").First() as ProjectCurve;
                else if (modularGroupPart.Name == "ModularGroupHigherX") untrimmedColumns = substructurePart.Features.ToArray().Where(f => f.Name == "ArchColumnsToBeTrimmedHigherX").First() as ProjectCurve;
                untrimmedColumns.Suppress(); 
                untrimmedColumns.Unsuppress(); //just to make disappear a pointless warning of nx
                List<Line> lines = untrimmedColumns.GetEntities().OfType<Line>().ToList();
                List<Point3d> helpPoints = lines.Select(l => GeometryHelper.GetLineCenter(l)).OrderBy(l=>l.Y).ToList();

                //declare builders
                TrimCurve2 nullNXOpen_Features_TrimCurve2 = null;
                TrimCurve2Builder trimCurve2Builder1 = substructurePart.Features.CreateTrimCurve2FeatureBuilder(nullNXOpen_Features_TrimCurve2);
                trimCurve2Builder1.CurveToTrim.Clear();
                trimCurve2Builder1.MakeInputCurvesDashed = false;
                NXOpen.GeometricUtilities.TrimCurveBoundingObjectBuilder trimCurveBoundingObjectBuilder1;
                trimCurveBoundingObjectBuilder1 = trimCurve2Builder1.CreateTrimCurveBoundingObjectBuilder();
                Plane nullNXOpen_Plane = null;
                trimCurveBoundingObjectBuilder1.BoundingPlane = nullNXOpen_Plane;
                trimCurve2Builder1.BoundingObjectList.Append(trimCurveBoundingObjectBuilder1);
                trimCurve2Builder1.DirectionOption = TrimCurve2Builder.Direction.AlongDirection;
                trimCurve2Builder1.CurveExtensionOption = TrimCurve2Builder.CurveExtension.None;
                trimCurveBoundingObjectBuilder1.BoundingObjectMethodType = NXOpen.GeometricUtilities.TrimCurveBoundingObjectBuilder.Method.SelectPlane;
                NXOpen.GeometricUtilities.TrimCurveBoundingObjectBuilder trimCurveBoundingObjectBuilder2;
                trimCurveBoundingObjectBuilder2 = trimCurve2Builder1.CreateTrimCurveBoundingObjectBuilder();
                NXOpen.SelectDisplayableObjectList selectDisplayableObjectList2;
                selectDisplayableObjectList2 = trimCurveBoundingObjectBuilder2.BoundingObjectList;
                selectDisplayableObjectList2.Clear();
                trimCurveBoundingObjectBuilder2.BoundingPlane = nullNXOpen_Plane;
                trimCurve2Builder1.BoundingObjectList.Append(trimCurveBoundingObjectBuilder2);
                trimCurveBoundingObjectBuilder2.BoundingObjectMethodType = NXOpen.GeometricUtilities.TrimCurveBoundingObjectBuilder.Method.SelectPlane;

                //add all lines belonging to the projected curve feature to the trimming curve builder
                Point3d origin3 = new NXOpen.Point3d(0.0, 0.0, 0.0);
                Vector3d vector1 = new NXOpen.Vector3d(0.0, 0.0, 1.0);
                Direction direction1 = substructurePart.Directions.CreateDirection(origin3, vector1, SmartObject.UpdateOption.WithinModeling);
                trimCurve2Builder1.Vector = direction1;
                trimCurve2Builder1.MakeInputCurvesDashed = true;
                SelectionIntentRuleOptions selectionIntentRuleOptions1 = substructurePart.ScRuleFactory.CreateRuleOptions();
                selectionIntentRuleOptions1.SetSelectedFromInactive(false);
                NXOpen.Features.Feature[] features1 = new Feature[1] { untrimmedColumns };
                NXOpen.DisplayableObject nullNXOpen_DisplayableObject = null;
                NXOpen.CurveFeatureRule curveFeatureRule1;
                curveFeatureRule1 = substructurePart.ScRuleFactory.CreateRuleCurveFeature(features1, nullNXOpen_DisplayableObject, selectionIntentRuleOptions1);
                selectionIntentRuleOptions1.Dispose();
                SelectionIntentRule[] rules1 = new SelectionIntentRule[1];
                rules1[0] = curveFeatureRule1;
                NXObject nullNXOpen_NXObject = null;

                // if error message "Erzeugen eines teileübergreifenden Schnitts ist nicht erlaubt" appears check that work part is substructure
                trimCurve2Builder1.CurveToTrim.AllowSelfIntersection(true);
                trimCurve2Builder1.CurveToTrim.AllowDegenerateCurves(false);
                trimCurve2Builder1.CurveToTrim.SetAllowedEntityTypes(NXOpen.Section.AllowTypes.OnlyCurves);
                trimCurve2Builder1.CurveToTrim.AddToSection(rules1, nullNXOpen_NXObject, nullNXOpen_NXObject, nullNXOpen_NXObject, helpPoints.First(), Section.Mode.Create, false);

                //cast lower trimming plane to simple nxopen.plane
                Point3d origin2 = new NXOpen.Point3d(0.0, 0.0, 0.0);
                Vector3d normal2 = new NXOpen.Vector3d(0.0, 0.0, 1.0);
                Plane trimmingPlaneLowerYCasted = substructurePart.Planes.CreatePlane(origin2, normal2, NXOpen.SmartObject.UpdateOption.WithinModeling);
                trimCurveBoundingObjectBuilder1.BoundingPlane = trimmingPlaneLowerYCasted;
                trimmingPlaneLowerYCasted.SetMethod(NXOpen.PlaneTypes.MethodType.Distance);
                NXOpen.NXObject[] trimmingPlaneLowerYAsNxObj = new NXOpen.NXObject[1] { trimmingPlaneLowerY };
                trimmingPlaneLowerYCasted.SetFlip(false);
                trimmingPlaneLowerYCasted.SetReverseSide(false);
                trimmingPlaneLowerYCasted.SetAlternate(NXOpen.PlaneTypes.AlternateType.One);
                trimmingPlaneLowerYCasted.Evaluate();
                trimmingPlaneLowerYCasted.SetMethod(NXOpen.PlaneTypes.MethodType.Distance);
                trimmingPlaneLowerYCasted.SetGeometry(trimmingPlaneLowerYAsNxObj);
                trimmingPlaneLowerYCasted.SetFlip(false);
                trimmingPlaneLowerYCasted.SetReverseSide(false);
                trimmingPlaneLowerYCasted.SetAlternate(NXOpen.PlaneTypes.AlternateType.One);
                trimmingPlaneLowerYCasted.Evaluate();
                trimCurveBoundingObjectBuilder1.BoundingPlane = trimmingPlaneLowerYCasted;

                //cast higher trimming plane to simple nxopen.plane
                trimCurve2Builder1.UpdateTrimRegionsAndDivideLocations();
                trimCurve2Builder1.ResetTrimRegions();
                Point3d origin4 = new NXOpen.Point3d(0.0, 0.0, 0.0);
                Vector3d normal3 = new NXOpen.Vector3d(0.0, 0.0, 1.0);
                Plane trimmingPlaneHigherYCasted = substructurePart.Planes.CreatePlane(origin4, normal3, NXOpen.SmartObject.UpdateOption.WithinModeling);
                NXOpen.NXObject[] trimmingPlaneHigherYAsNxObj = new NXOpen.NXObject[1] { trimmingPlaneHigherY };
                trimmingPlaneHigherYCasted.SetMethod(NXOpen.PlaneTypes.MethodType.Distance);
                trimmingPlaneHigherYCasted.SetFlip(false);
                trimmingPlaneHigherYCasted.SetReverseSide(false);
                trimmingPlaneHigherYCasted.SetAlternate(NXOpen.PlaneTypes.AlternateType.One);
                trimmingPlaneHigherYCasted.Evaluate();
                trimmingPlaneHigherYCasted.SetMethod(NXOpen.PlaneTypes.MethodType.Distance);
                trimmingPlaneHigherYCasted.SetGeometry(trimmingPlaneHigherYAsNxObj);
                trimmingPlaneHigherYCasted.SetFlip(false);
                trimmingPlaneHigherYCasted.SetReverseSide(false);
                trimmingPlaneHigherYCasted.Evaluate();
                trimCurveBoundingObjectBuilder2.BoundingPlane = trimmingPlaneHigherYCasted;

                //final config of trim curve builder, commit and group feature
                trimCurve2Builder1.UpdateTrimRegionsAndDivideLocations();
                trimCurve2Builder1.ResetTrimRegions();
                foreach (Point3d helpPoint in helpPoints) trimCurve2Builder1.SelectTrimRegion(helpPoint);
                trimCurve2Builder1.CurveOptions.InputCurveOption = NXOpen.GeometricUtilities.CurveOptions.InputCurve.Blank;
                Feature[] trimmedCurves = new Feature[] { trimCurve2Builder1.Commit() as Feature };
                FeatureGroup featureGroup = null;
                if (modularGroupPart.Name == "ModularGroupLowerX")
                {
                    trimmedCurves[0].SetName("ArchColumnsTrimmedLowerX");
                    featureGroup = substructurePart.Features.ToArray().Where(f => f.Name == "LowerXGroup").First() as FeatureGroup;
                    featureGroup.AddMembersWithRelocation(trimmedCurves, true, true);
                }
                else if (modularGroupPart.Name == "ModularGroupHigherX")
                {
                    trimmedCurves[0].SetName("ArchColumnsTrimmedHigherX");
                    featureGroup = substructurePart.Features.ToArray().Where(f => f.Name == "HigherXGroup").First() as FeatureGroup;
                    featureGroup.AddMembersWithRelocation(trimmedCurves, true, true);
                }

                //wind up trimming operation
                trimCurve2Builder1.Destroy();
                trimmingPlaneLowerYCasted.DestroyPlane();
                trimmingPlaneHigherYCasted.DestroyPlane();
                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                session.CleanUpFacetedFacesAndEdges();
                return true;
            }

            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown during Preprocessing of steering sketch in substructure part: " + e.Message);
                return false;
            }
        }

        public static bool ImportSteeringSketchInModularGroupPart(Part substructure, Part modularGroupPart, ref List<Line> archSegments, ref List<Line> archColumns, ref List<Line> lateralColumns)
        {
            try
            {
                //import it via the wave linker into the right modularGroup part file
                Session session = Session.GetSession();
                session.Parts.SetWork(modularGroupPart);
                Session.UndoMarkId markId;
                markId = session.SetUndoMark(Session.MarkVisibility.Visible, "Wavelink the steering sketches of " + modularGroupPart.Name);
                session.ListingWindow.WriteFullline("Starting to import the steering sketch of the substructure part into the modular group parts");

                //features from modular group
                CurveFeature archSegmentsToBeWaveLinked = null, lateralColumnsToBeWaveLinked = null, archColumnsToBeWaveLinked = null;
                if (modularGroupPart.Name == "ModularGroupLowerX")
                {
                    archSegmentsToBeWaveLinked = substructure.Features.ToArray().Where(f => f.Name == "ArchLowerX").First() as CurveFeature;
                    archColumnsToBeWaveLinked = substructure.Features.ToArray().Where(f => f.Name == "ArchColumnsTrimmedLowerX").First() as CurveFeature; 
                    lateralColumnsToBeWaveLinked = substructure.Features.ToArray().Where(f => f.Name == "LateralColumnsLowerX").First() as CurveFeature; 
                }
                else if (modularGroupPart.Name == "ModularGroupHigherX")
                {
                    archSegmentsToBeWaveLinked = substructure.Features.ToArray().Where(f => f.Name == "ArchHigherX").First() as CurveFeature;
                    archColumnsToBeWaveLinked = substructure.Features.ToArray().Where(f => f.Name == "ArchColumnsTrimmedHigherX").First() as CurveFeature;
                    lateralColumnsToBeWaveLinked = substructure.Features.ToArray().Where(f => f.Name == "LateralColumnsHigherX").First() as CurveFeature;
                }

                int i = 1;
                foreach (CurveFeature curveFeatureToBeWaveLinked in new CurveFeature[] { archSegmentsToBeWaveLinked, archColumnsToBeWaveLinked, lateralColumnsToBeWaveLinked })
                {
                    //builders with settings
                    Feature nullNXOpen_Features_Feature = null;
                    WaveLinkBuilder waveLinkBuilder1 = modularGroupPart.BaseFeatures.CreateWaveLinkBuilder(nullNXOpen_Features_Feature);
                    WaveDatumBuilder waveDatumBuilder1 = waveLinkBuilder1.WaveDatumBuilder;
                    CompositeCurveBuilder compositeCurveBuilder1 = waveLinkBuilder1.CompositeCurveBuilder;
                    Section section1 = compositeCurveBuilder1.Section;
                    section1.SetAllowRefCrvs(false);
                    compositeCurveBuilder1.Section.AngleTolerance = 0.5;
                    compositeCurveBuilder1.Section.DistanceTolerance = 0.01;
                    compositeCurveBuilder1.Section.ChainingTolerance = 0.0094999999999999998;
                    compositeCurveBuilder1.Associative = true;
                    compositeCurveBuilder1.MakePositionIndependent = false;
                    compositeCurveBuilder1.FixAtCurrentTimestamp = false;
                    compositeCurveBuilder1.HideOriginal = false;
                    compositeCurveBuilder1.InheritDisplayProperties = false;
                    compositeCurveBuilder1.JoinOption = NXOpen.Features.CompositeCurveBuilder.JoinMethod.No;
                    compositeCurveBuilder1.Tolerance = 0.01;
                    Section section2 = compositeCurveBuilder1.Section;
                    NXOpen.GeometricUtilities.CurveFitData curveFitData2 = compositeCurveBuilder1.CurveFitData;
                    section2.SetAllowedEntityTypes(Section.AllowTypes.CurvesAndPoints);

                    //select all lines by selection intent rules
                    SelectionIntentRuleOptions selectionIntentRuleOptions1;
                    selectionIntentRuleOptions1 = modularGroupPart.ScRuleFactory.CreateRuleOptions();
                    selectionIntentRuleOptions1.SetSelectedFromInactive(false);
                    Feature[] features1 = new Feature[1] { curveFeatureToBeWaveLinked };
                    DisplayableObject nullNXOpen_DisplayableObject = null;
                    CurveFeatureRule curveFeatureRule1;
                    curveFeatureRule1 = modularGroupPart.ScRuleFactory.CreateRuleCurveFeature(features1, nullNXOpen_DisplayableObject, selectionIntentRuleOptions1);
                    selectionIntentRuleOptions1.Dispose();
                    section2.AllowSelfIntersection(false);
                    section2.AllowDegenerateCurves(false);
                    SelectionIntentRule[] rules1 = new NXOpen.SelectionIntentRule[1];
                    rules1[0] = curveFeatureRule1;
                    NXObject nullNXOpen_NXObject = null;
                    Point3d helpPoint1 = new Point3d(32500.000000000004, 914.61228318650046, 2138.2842689835006);
                    section2.AddToSection(rules1, nullNXOpen_NXObject, nullNXOpen_NXObject, nullNXOpen_NXObject, helpPoint1, Section.Mode.Create, false);

                    CompositeCurve linkedCompositeCurve = waveLinkBuilder1.Commit() as CompositeCurve;
                    if (i == 1) 
                    { 
                        archSegments = linkedCompositeCurve.GetEntities().OfType<Line>().ToList();
                        linkedCompositeCurve.SetName("ReferenceArchGeometryForAlgorithm");
                    }
                    else if (i == 2) { 
                        archColumns = linkedCompositeCurve.GetEntities().OfType<Line>().ToList();
                        linkedCompositeCurve.SetName("ReferenceArchColumnGeometryForAlgorithm");
                    }
                    else if (i == 3) { 
                        lateralColumns = linkedCompositeCurve.GetEntities().OfType<Line>().ToList();
                        linkedCompositeCurve.SetName("ReferenceLateralColumnGeometryForAlgorithm");
                    }
                    
                    waveLinkBuilder1.Destroy();
                    session.CleanUpFacetedFacesAndEdges();
                    i++;
                }

                int nErrs1 = session.UpdateManager.DoUpdate(markId);
                return true;
            }

            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown during wave link import of steering sketch from substructure to modular group parts: " + e.Message);
                archSegments = null;
                archColumns = null; 
                lateralColumns = null;
                return false;
            }

        }

    }
}
