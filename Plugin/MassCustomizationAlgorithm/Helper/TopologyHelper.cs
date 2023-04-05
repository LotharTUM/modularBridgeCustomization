using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchBridgeDataModel;
using NXOpen;
using NXOpen.Assemblies;
using NXOpen.Features;
using NXOpen.UF;

namespace ArchBridgeAlgorithm.Helper
{
    /// <summary>
    /// class responsible of evolving the semantics and topology of the design, by analyzing patterns and chosing appropriate number and types of modules and connections and few geometric properties required for the object instantations
    /// </summary>
    public class TopologyHelper
    {
        /// <summary>
        /// computes the initial, rough topology of arch segments, kneeNodes and columns 
        /// </summary>
        public static SubsystemModularGroup GenerateModularGroupOutlineFromSteeringSketch(Part substructurePart, Part modularGroupPart, List<Line> archSegmentLines, List<Line> archColumnLines, List<Line> lateralColumnLines)
        {
            Session session = Session.GetSession();
            PartLoadStatus pls1;
            session.Parts.SetActiveDisplay(substructurePart, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls1);
            session.Parts.SetWork(substructurePart);
            //sorted input data
            List<Line> ySortedArchSegments = archSegmentLines.OrderBy(c => c.StartPoint.Y).ToList();
            List<Line> ySortedArchColumns = archColumnLines.OrderBy(c => c.StartPoint.Y).ToList();
            List<Line> ySortedLateralColumns = lateralColumnLines.OrderBy(c => c.StartPoint.Y).ToList();

            //populate and assign then
            List<SubsystemArchSegment> archSegments = new List<SubsystemArchSegment>();
            List<SubsystemColumn> columns = new List<SubsystemColumn>();
            List<Support> supports = new List<Support>();

            // segments
            int s = 1;
            foreach (Line line in ySortedArchSegments)
            {
                //all segments until the level, middle one are fully before the symmetry plane
                bool beforeXzSymmetryPlane = false;
                if (s <= ((double) ySortedArchSegments.Count())/2) beforeXzSymmetryPlane = true;

                //distinguish two cases
                if (s==1|| s ==5)
                {
                    List <Point3d> yOrderedPoints = new List<Point3d>() { line.StartPoint, line.EndPoint };
                    yOrderedPoints = yOrderedPoints.OrderBy(p => p.Y).ToList();
                    Support newSupport = new Support(SupportTypeSpecification.type1); //right now not further used in the algo, maybe useful for structural analysis
                    if (s==1) newSupport.SetSupportFaceReferencePoint(yOrderedPoints.First());
                    if (s==5) newSupport.SetSupportFaceReferencePoint(yOrderedPoints.Last());
                    supports.Add(newSupport);

                    archSegments.Add(new SubsystemArchSegment(line, newSupport, beforeXzSymmetryPlane));
                }
                else
                {
                    archSegments.Add(new SubsystemArchSegment(line, beforeXzSymmetryPlane));
                }
                s++;
            }
            
            //columns supported by arch
            double m = 1;
            foreach (Line line in ySortedArchColumns)
            {
                if (line.Equals(ySortedArchColumns.First()))
                {
                    WavePoint referencePointFeatureArchColumn = substructurePart.Features.ToArray().Where(f => f.Name == "ReferencePointArchColumnFoundationLowerY").First() as WavePoint;
                    Component substructureComponent = substructurePart.ComponentAssembly.RootComponent;
                    Point referencePointArchColumn;
                    try { referencePointArchColumn = referencePointFeatureArchColumn.GetEntities().OfType<Point>().First(); }
                    catch { referencePointArchColumn = referencePointFeatureArchColumn.FindObject("POINT 1") as Point; }
                    CurveCollection lineCollection = session.Parts.Work.Curves;
                    Line yOffsettedLine = lineCollection.CreateLine(
                        new Point3d(line.StartPoint.X, referencePointArchColumn.Coordinates.Y, referencePointArchColumn.Coordinates.Z),
                        new Point3d(line.EndPoint.X, referencePointArchColumn.Coordinates.Y, line.EndPoint.Z));

                    SubsystemColumn newColumn = new SubsystemColumn(yOffsettedLine, supports.First(), true, true);
                    columns.Add(newColumn);
                    line.Blank();
                    yOffsettedLine.Blank();
                    
                }

                else if (line.Equals(ySortedArchColumns.Last()))
                {
                    WavePoint referencePointFeatureArchColumn = substructurePart.Features.ToArray().Where(f => f.Name == "ReferencePointArchColumnFoundationHigherY").First() as WavePoint;
                    Component substructureComponent = substructurePart.ComponentAssembly.RootComponent;
                    Point referencePointArchColumn;
                    try { referencePointArchColumn = referencePointFeatureArchColumn.GetEntities().OfType<Point>().First(); }
                    catch { referencePointArchColumn = referencePointFeatureArchColumn.FindObject("POINT 1") as Point; }
                    CurveCollection lineCollection = session.Parts.Work.Curves;
                    Line yOffsettedLine = lineCollection.CreateLine(
                        new Point3d(line.StartPoint.X, referencePointArchColumn.Coordinates.Y, referencePointArchColumn.Coordinates.Z),
                        new Point3d(line.EndPoint.X, referencePointArchColumn.Coordinates.Y, line.EndPoint.Z));

                     columns.Add(new SubsystemColumn(yOffsettedLine, supports.Last(), false, true));
                    line.Blank();
                    yOffsettedLine.Blank(); 
                }

                else
                {
                    if (m <= (ySortedArchColumns.Count / 2))
                    {

                        columns.Add(new SubsystemColumn(line, true, true));
                    }
                    else
                    {
                        columns.Add(new SubsystemColumn(line, false, true));
                    }
                }
                m++;
            }

            //knee nodes
            List<ModuleKneeNode> kneeNodes = new List<ModuleKneeNode>();
            for (int i = 0; i < ySortedArchSegments.Count() - 1; i++)
            {
                ModuleKneeNode newKneeNode = new ModuleKneeNode(
                    columns.ElementAt(i + 1),
                    archSegments.ElementAt(i),
                    archSegments.ElementAt(i + 1));
                
                kneeNodes.Add(newKneeNode);
                columns.ElementAt(i + 1).SetKneeNode(newKneeNode);
            }

            //lateral columns
            foreach (Line line in lateralColumnLines)
            {
                List<Point3d> zOrderedPoints = new List<Point3d>() { line.StartPoint, line.EndPoint };
                zOrderedPoints = zOrderedPoints.OrderBy(p => p.Z).ToList();
                Support newSupport = new Support(SupportTypeSpecification.type2);
                supports.Add(newSupport);
                columns.Add(new SubsystemColumn(line, newSupport, false, false));
            }

            //now missing references to knee nodes can be set
            SubsystemArch arch = new SubsystemArch(archSegments, kneeNodes);
            SubsystemModularGroup modularGroup = new SubsystemModularGroup(modularGroupPart, arch, columns, supports);

            for (int i = 0; i < archSegments.Count(); i++)
            {
                if (i == 0)
                {
                    archSegments.ElementAt(i).SetHigherYKneeNode(kneeNodes.First());
                }
               
                else if (i>0 && i< (archSegments.Count()-1))
                {
                    archSegments.ElementAt(i).SetBothKneeNodes(kneeNodes.ElementAt(i - 1), kneeNodes.ElementAt(i));
                } 
                
                else if (i == (archSegments.Count() - 1))
                {
                    archSegments.ElementAt(i).SetLowerYKneeNode(kneeNodes.Last());
                }
            }
            return modularGroup;
        }

        /// <summary>
        /// computes the topology and types of all arch panel modules 
        /// </summary>
        public static bool GenerateArchPanelObjects(Substructure substructure)
        {
            try
            {
                Session.GetSession().ListingWindow.WriteFullline("Starting to compute the topology and type of all arch panels");
                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    int s = 1;
                    double lengthOfArchPanelsType2And3 = modularGroup.Arch.ArchSegments.ElementAt(2).SketchGeo.GetLength() / 2;
                    foreach (SubsystemArchSegment archSeg in modularGroup.Arch.ArchSegments)
                    {
                        // decided to restrict our kit to have always arch segments divided into two arch panel modules and thus restricting the arch segment length between 4.00 and 8.00 meters of length (including the approximate measures of the knee node occupying a part of the arch segment)
                        double modularizedLength = archSeg.SketchGeo.GetLength();
                        if (modularizedLength < 2500) throw new ArgumentOutOfRangeException("arch Segment to short for kit to be applicable");
                        if (modularizedLength > 8000) throw new ArgumentOutOfRangeException("arch Segment too long, modify the geometry to an arch segment length shorter than 8.00 meters");

                        //type
                        ArchPanelTypeSpecification type = ArchPanelTypeSpecification.Type2; //default as most follow this type;

                        //subdivide arch segment line in two and store al
                        List<Point3d> points = new List<Point3d>();
                        Point3d startPoint = archSeg.SketchGeo.StartPoint;
                        Point3d endPoint = archSeg.SketchGeo.EndPoint;
                        points.Add(startPoint);
                        points.Add(endPoint);
                        points = points.OrderBy(p => p.Y).ToList();
                        double deltaZ = points.Last().Z - points.First().Z;
                        double deltaY = (points.Last().Y - points.First().Y);
                        double angleFromLowerYToHigherY = Math.Atan(deltaZ / deltaY);

                        //midpoint is either in the middle (for interior arch segments) or slightly offset for outside arch segments (to have equal lengths of arch panels type2/3
                        if (s == 1)
                        {
                            points.Insert(1, new Point3d(points.First().X,
                               points.First().Y + Math.Cos(angleFromLowerYToHigherY) * (modularizedLength - lengthOfArchPanelsType2And3),
                               points.First().Z + Math.Sin(angleFromLowerYToHigherY) * (modularizedLength - lengthOfArchPanelsType2And3)));
                        }

                        else if (s >1 && s<5)
                        {
                            points.Insert(1, new Point3d(
                                (points.First().X + points.Last().X) / 2,
                                (points.First().Y + points.Last().Y) / 2,
                                (points.First().Z + points.Last().Z) / 2));
                            
                        }

                        else if (s==5)
                        {
                            points.Insert(1, new Point3d(points.First().X,
                             points.First().Y + Math.Cos(angleFromLowerYToHigherY) * lengthOfArchPanelsType2And3,
                             points.First().Z + Math.Sin(angleFromLowerYToHigherY) * lengthOfArchPanelsType2And3));
                            type = ArchPanelTypeSpecification.Type1;
                        }


                        List<ModuleArchPanel> archPanels = new List<ModuleArchPanel>();
                        for (int i = 0; i < 2; i++)
                        {
                            // instantiate the class
                            if (s==1 && i == 0 || s==5&&i==1) { type = ArchPanelTypeSpecification.Type1; };
                            ModuleArchPanel moduleArchPanel = new ModuleArchPanel(archSeg, type, points.ElementAt(i), points.ElementAt(i + 1));
                            archPanels.Add(moduleArchPanel);
                        }
                        archSeg.SetArchPanels(archPanels);
                    }
                    s++;
                }
                return true;
            }
            catch (ArgumentException e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in TopologyHelper.GenerateArchPanelObjects: " + e.Message);
                return false;

            }
        }

        /// <summary>
        /// computes the topology and types of all column modules 
        /// </summary>
        public static bool GenerateColumnObjects(Substructure substructure, string columnDir)
        {
            try
            {
                //open session
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Starting to compute the topology of all column elements");
                Part columnPanelType1Part, columnPanelType2Part, columnPanelType3Part, columnPanelType4Part;
             
                PartLoadStatus pls1, pls2, pls3;

                //get part file for  column panel type 1
                try { columnPanelType1Part = session.Parts.FindObject("ColumnPanel Type1 KneeNodeOrBoxFoundation_Panel_OtherPanelType2or3") as Part; }
                catch { columnPanelType1Part = session.Parts.Open(string.Format("{0}\\ColumnPanel Type1 KneeNodeOrBoxFoundation_Panel_OtherPanelType2or3.prt", columnDir), out pls1); }

                //get part file for column panel type 2
                try { columnPanelType2Part = session.Parts.FindObject("ColumnPanel Type2 PanelType1or2_Panel_PanelType2or3") as Part; }
                catch { columnPanelType2Part = session.Parts.Open(string.Format("{0}\\ColumnPanel Type2 PanelType1or2_Panel_PanelType2or3.prt", columnDir), out pls2); }

                //get part file for column panel type 3
                try { columnPanelType3Part = session.Parts.FindObject("ColumnPanel Type3 OtherPanelType1or2_Panel_Deck") as Part; }
                catch { columnPanelType3Part = session.Parts.Open(string.Format("{0}\\ColumnPanel Type3 OtherPanelType1or2_Panel_Deck.prt", columnDir), out pls3); }

                //get part file for column panel type 4
                try { columnPanelType4Part = session.Parts.FindObject("ColumnPanel Type4 KneeNode_Panel_Deck") as Part; }
                catch { columnPanelType4Part = session.Parts.Open(string.Format("{0}\\ColumnPanel Type4 KneeNode_Panel_Deck.prt", columnDir), out pls3); }


                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    foreach (SubsystemColumn column in modularGroup.Columns)
                    {
                        //Data intialization
                        List<ModuleColumnPanel> columnPanels = new List<ModuleColumnPanel>();
                        int numberOfPanels = 0;
                        double panelLength = 0.0;
                        double maxPanelLength = 4000.0; //constraint to ensure robotic fabrication and simple logistics
                        double columnLength = column.SketchGeo.EndPoint.Z - column.SketchGeo.StartPoint.Z;
                        List<Point3d> unorderedPoints = new List<Point3d>() { column.SketchGeo.EndPoint, column.SketchGeo.StartPoint };
                        Point3d startPoint = unorderedPoints.OrderBy(p => p.Z).First();
                        Point3d endPoint = unorderedPoints.OrderBy(p => p.Z).Last();
                        ColumnPanelAnchorage anchorage = null;

                        //Determination of number of panels and their length
                        if (columnLength <= maxPanelLength)
                        {
                            numberOfPanels = 1;
                            ColumnPanelTypeSpecification type = ColumnPanelTypeSpecification.Type4;
                            ModuleColumnPanel moduleColumnPanel = new ModuleColumnPanel(column, type, startPoint, endPoint);
                            columnPanels.Add(moduleColumnPanel);
                        }
                        else if (columnLength > maxPanelLength && columnLength <= 3*maxPanelLength) numberOfPanels = (int)Math.Ceiling(columnLength / maxPanelLength);
                        else throw new Exception("column length too great");

                        panelLength = columnLength / numberOfPanels;
                        if (numberOfPanels > 1)
                        {
                            //geometrical preprocessing, relevant only if there is more than one panel involved
                            List<Point3d> alignmentPoints = GeometryHelper.SubdivideColumn(startPoint, endPoint, numberOfPanels, panelLength, column.IsArchColumn);

                            //the lateral columns are eventually lifted up a bit to spare the extra type 3 module in GeometryHelper.SubdivideColumn(), so we assign the reference point of the support only after having called this function
                            if (!column.IsArchColumn) column.Support.SetSupportFaceReferencePoint(alignmentPoints.First());

                            //iterate over all spans and create a ModuleColumnPanel object.
                            {
                                for (int j = 0; j < numberOfPanels; j++)
                                {
                                    ColumnPanelTypeSpecification type = ColumnPanelTypeSpecification.Type1;
                                    Point3d lowerZPoint = alignmentPoints.ElementAt(j);
                                    Point3d higherZPoint = alignmentPoints.ElementAt(j + 1);
                                    if (j == 0) type = ColumnPanelTypeSpecification.Type1;
                                    else if (j == 1 && numberOfPanels == 3) type = ColumnPanelTypeSpecification.Type2;
                                    else if ((j == 1 && numberOfPanels == 2) || (j == 2 && numberOfPanels == 3)) type = ColumnPanelTypeSpecification.Type3;
                                    ModuleColumnPanel moduleColumnPanel = new ModuleColumnPanel(column, type, lowerZPoint, higherZPoint);
                                    columnPanels.Add(moduleColumnPanel);
                                    anchorage = new ColumnPanelAnchorage(column);
                                }
                            }
                        }
                        column.ZOrderedPanels.AddRange(columnPanels);
                        column.SetColumnPanels(columnPanels);
                        column.SetAnchorage(anchorage);

                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in TopologyHelper.GenerateColumnObjects: " + e.Message);
                return false;
            }
        }
    
        /// <summary>
        /// computes the number, type and anchorage points of all tendons to be placed in the archs of the modular groups
        /// </summary>
        internal static bool GenerateTendonObjectsInArchs(Substructure substructure)
        {
            try
            {
                Session.GetSession().ListingWindow.WriteFullline("Starting to compute topology and types of all tendons in archs.");
                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                { 
                    //input data
                    List<SubsystemArchSegment> archSegments = modularGroup.Arch.ArchSegments;
                    
                    int i = 1;
                    foreach (SubsystemArchSegment archSegment in archSegments)
                    { 
                        //lateral
                        if (archSegment.Equals(archSegments.First()) || archSegment.Equals(archSegments.First()))
                        {
                            bool firstSegment = archSegment.Equals(archSegments.First());
                            //get components and parts 
                            Component archPanelType1Component = archSegment.YOrderedPanels.First().Component; 
                            if (!firstSegment) { archPanelType1Component = archSegment.YOrderedPanels.Last().Component; }
                            //only one knee node in list returned for lateral arch segments
                            Component anchoringKneeNodeComponent = archSegment.GetKneeNodes().First().Component; 
                            Part archPanelType1Part = archPanelType1Component.Prototype as Part;
                            Part anchoringKneeNodePart = anchoringKneeNodeComponent.Prototype as Part;


                            //find feature in part and find occurence in component, order points
                            //for panel
                            SketchFeature relevantPartFeaturePanel = archPanelType1Part.Features.ToArray().Where(f => f.Name == "TendonAnchoragePoints").First() as SketchFeature;
                            Sketch relevantComponentFeaturePanel = archPanelType1Component.FindOccurrence(relevantPartFeaturePanel.Sketch) as Sketch;
                            List<Point> componentPointsPanelTmp = relevantComponentFeaturePanel.GetAllGeometry().OfType<Point>().ToArray().ToList();
                            componentPointsPanelTmp = componentPointsPanelTmp.OrderBy(p => p.Coordinates.X).ToList();
                            List<Point> componentPointsPanel = new List<Point>();
                            componentPointsPanel.AddRange(componentPointsPanelTmp.Take(2).OrderBy(p => p.Coordinates.Z));
                            componentPointsPanel.AddRange(componentPointsPanelTmp.Skip(2).Take(2).OrderBy(p => p.Coordinates.Z));

                            //same for knee node
                            SketchFeature relevantPartFeatureKneeNode = anchoringKneeNodePart.Features.ToArray().Where(f => f.Name == "TendonAnchoragePointsForLowerZArchSegment" && f.Suppressed == false).First() as SketchFeature;
                            Sketch relevantComponentFeatureKneeNode = anchoringKneeNodeComponent.FindOccurrence(relevantPartFeatureKneeNode.Sketch) as Sketch;
                            List<Point> componentPointsKneeNodeTmp = relevantComponentFeatureKneeNode.GetAllGeometry().OfType<Point>().ToList();
                            componentPointsKneeNodeTmp = componentPointsKneeNodeTmp.OrderBy(p => p.Coordinates.X).ToList();
                            List<Point> componentPointsKneeNode = new List<Point>();
                            componentPointsKneeNode.AddRange(componentPointsKneeNodeTmp.Take(2).OrderBy(p => p.Coordinates.Z));
                            componentPointsKneeNode.AddRange(componentPointsKneeNodeTmp.Skip(2).Take(2).OrderBy(p => p.Coordinates.Z));

                            //list points
                            Point lowerXlowerZPanel = componentPointsPanel.ElementAt(0);
                            Point lowerXhigherZPanel = componentPointsPanel.ElementAt(1);
                            Point higherXlowerZPanel = componentPointsPanel.ElementAt(2);
                            Point higherXhigherZPanel = componentPointsPanel.ElementAt(3);

                            Point lowerXlowerZKneeNode = componentPointsKneeNode.ElementAt(0);
                            Point lowerXhigherZKneeNode = componentPointsKneeNode.ElementAt(1);
                            Point higherXlowerZKneeNode = componentPointsKneeNode.ElementAt(2);
                            Point higherXhigherZKneeNode = componentPointsKneeNode.ElementAt(3);

                            //use the objects and geometry to generate tendon objects
                            List<Tendon> archTendons = new List<Tendon>();
                            archTendons.Add(new Tendon(archSegment, archSegment.GetKneeNodes().First(), lowerXlowerZPanel, lowerXlowerZKneeNode, true));
                            archTendons.Add(new Tendon(archSegment, archSegment.GetKneeNodes().First(), lowerXhigherZPanel, lowerXhigherZKneeNode, false));
                            archTendons.Add(new Tendon(archSegment, archSegment.GetKneeNodes().First(), higherXlowerZPanel, higherXlowerZKneeNode, true));
                            archTendons.Add(new Tendon(archSegment, archSegment.GetKneeNodes().First(), higherXhigherZPanel, higherXhigherZKneeNode, false));
                            archSegment.SetTendons(archTendons);

                        }

                        //for interior arch segments
                        else
                        {
                            //get components and parts                        
                            Component anchoringKneeNodeComponentLowerY = archSegment.GetKneeNodes().First().Component;
                            Component anchoringKneeNodeComponentHigherY = archSegment.GetKneeNodes().Last().Component;

                            Part anchoringKneeNodePartLowerY = anchoringKneeNodeComponentLowerY.Prototype as Part;
                            Part anchoringKneeNodePartHigherY = anchoringKneeNodeComponentHigherY.Prototype as Part;

                            //get anchoring points sensitive of the segment position before or after the symmetry axis respectively at the level middle piece
                            SketchFeature relevantPartFeatureLowerYKneeNode;
                            SketchFeature relevantPartFeatureHigherYKneeNode;

                            if (archSegment.BeforeXZSymmetryPlane && archSegment.InclinationToPositiveY > 0)
                            {
                                relevantPartFeatureLowerYKneeNode = anchoringKneeNodePartLowerY.Features.ToArray().Where(f => f.Name == "TendonAnchoragePointsForHigherZArchSegment" && f.Suppressed == false).First() as SketchFeature;
                                relevantPartFeatureHigherYKneeNode = anchoringKneeNodePartHigherY.Features.ToArray().Where(f => f.Name == "TendonAnchoragePointsForLowerZArchSegment" && f.Suppressed == false).First() as SketchFeature;
                            }
                            //straight middle segment
                            else if (Math.Abs(archSegment.InclinationToPositiveY) < 0.01)
                            {
                                relevantPartFeatureLowerYKneeNode = anchoringKneeNodePartLowerY.Features.ToArray().Where(f => f.Name == "TendonAnchoragePointsForHigherZArchSegment" && f.Suppressed == false).First() as SketchFeature;
                                relevantPartFeatureHigherYKneeNode = anchoringKneeNodePartHigherY.Features.ToArray().Where(f => f.Name == "TendonAnchoragePointsForHigherZArchSegment" && f.Suppressed == false).First() as SketchFeature;
                            }
                            else if (!archSegment.BeforeXZSymmetryPlane && archSegment.InclinationToPositiveY < 0)
                            {
                                relevantPartFeatureLowerYKneeNode = anchoringKneeNodePartLowerY.Features.ToArray().Where(f => f.Name == "TendonAnchoragePointsForLowerZArchSegment" && f.Suppressed == false).First() as SketchFeature;
                                relevantPartFeatureHigherYKneeNode = anchoringKneeNodePartHigherY.Features.ToArray().Where(f => f.Name == "TendonAnchoragePointsForHigherZArchSegment" && f.Suppressed == false).First() as SketchFeature;
                            }
                            else throw new ArgumentException();

                            Sketch relevantComponentFeatureLowerYKneeNode = anchoringKneeNodeComponentLowerY.FindOccurrence(relevantPartFeatureLowerYKneeNode.Sketch) as Sketch;
                            Sketch relevantComponentFeatureHigherYKneeNode = anchoringKneeNodeComponentHigherY.FindOccurrence(relevantPartFeatureHigherYKneeNode.Sketch) as Sketch;

                            //postprocess points for lowerY kneeNode
                            List<Point> componentPointsKneeNodeLowerYTmp = relevantComponentFeatureLowerYKneeNode.GetAllGeometry().OfType<Point>().ToList();
                            componentPointsKneeNodeLowerYTmp = componentPointsKneeNodeLowerYTmp.OrderBy(p => p.Coordinates.X).ToList();
                            List<Point> componentPointsKneeNodeLowerY = new List<Point>();
                            componentPointsKneeNodeLowerY.AddRange(componentPointsKneeNodeLowerYTmp.Take(2).OrderBy(p => p.Coordinates.Z));
                            componentPointsKneeNodeLowerY.AddRange(componentPointsKneeNodeLowerYTmp.Skip(2).Take(2).OrderBy(p => p.Coordinates.Z));

                            //postprocess points for higherY kneeNode
                            List<Point> componentPointsKneeNodeHigherYTmp = relevantComponentFeatureHigherYKneeNode.GetAllGeometry().OfType<Point>().ToList();
                            componentPointsKneeNodeHigherYTmp = componentPointsKneeNodeHigherYTmp.OrderBy(p => p.Coordinates.X).ToList();
                            List<Point> componentPointsKneeNodeHigherY = new List<Point>();
                            componentPointsKneeNodeHigherY.AddRange(componentPointsKneeNodeHigherYTmp.Take(2).OrderBy(p => p.Coordinates.Z));
                            componentPointsKneeNodeHigherY.AddRange(componentPointsKneeNodeHigherYTmp.Skip(2).Take(2).OrderBy(p => p.Coordinates.Z));

                            //use the objects and geometry to generate tendon objects
                            Point lowerXlowerZlowerKneeNode = componentPointsKneeNodeLowerY.ElementAt(0);
                            Point lowerXhigherZlowerKneeNode = componentPointsKneeNodeLowerY.ElementAt(1);
                            Point higherXlowerZlowerKneeNode = componentPointsKneeNodeLowerY.ElementAt(2);
                            Point higherXhigherZlowerKneeNode = componentPointsKneeNodeLowerY.ElementAt(3);

                            Point lowerXlowerZHigherKneeNode = componentPointsKneeNodeHigherY.ElementAt(0);
                            Point lowerXhigherZHigherKneeNode = componentPointsKneeNodeHigherY.ElementAt(1);
                            Point higherXlowerZHigherKneeNode = componentPointsKneeNodeHigherY.ElementAt(2);
                            Point higherXhigherZHigherKneeNode = componentPointsKneeNodeHigherY.ElementAt(3);

                            List<Tendon> archTendons = new List<Tendon>();
                            archTendons.Add(new Tendon(archSegment, archSegment.KneeNodeLowerY, archSegment.KneeNodeHigherY, lowerXlowerZlowerKneeNode, lowerXlowerZHigherKneeNode, true));
                            archTendons.Add(new Tendon(archSegment, archSegment.KneeNodeLowerY, archSegment.KneeNodeHigherY, lowerXhigherZlowerKneeNode, lowerXhigherZHigherKneeNode, false));
                            archTendons.Add(new Tendon(archSegment, archSegment.KneeNodeLowerY, archSegment.KneeNodeHigherY, higherXlowerZlowerKneeNode, higherXlowerZHigherKneeNode, true));
                            archTendons.Add(new Tendon(archSegment, archSegment.KneeNodeLowerY, archSegment.KneeNodeHigherY, higherXhigherZlowerKneeNode, higherXhigherZHigherKneeNode, false));
                            archSegment.SetTendons(archTendons);
                        }
                        i++;
                    }
                }
                return true;
            }

            catch (Exception e)
            {
                Session session = Session.GetSession();
                try
                {
                    int line = new StackTrace(e, true).GetFrame(0).GetFileLineNumber();
                    session.ListingWindow.WriteFullline(string.Format("Exception thrown in TopologyHelper.GenerateTendonObjectsInArch in line {0}: {1} ", line, e.Message));
                }
                catch
                {
                    session.ListingWindow.WriteFullline(string.Format("Exception thrown in TopologyHelper.GenerateTendonObjectsInArch: {0}", e.Message));
                }

              
                return false;
            }
        }

        /// <summary>
        /// computes the number, type and anchorage points of all tendons to be placed in the columns of the modular groups
        /// </summary>
        internal static bool GenerateTendonObjectsInColumns(Substructure substructure)
        {
            try
            {
                Session.GetSession().ListingWindow.WriteFullline("Starting to compute topology and types of all tendons in columns.");
                foreach (SubsystemModularGroup modularGroup in substructure.ModularGroups)
                {
                    foreach (SubsystemColumn column in modularGroup.Columns)
                    {
                        if (column.ZOrderedPanels.Count > 1)
                        {
                            //get components and parts
                            Part columnPanelType1Part = column.ZOrderedPanels.First().Part;
                            Part columnPanelType3Part = column.ZOrderedPanels.Last().Part;
                            Component columnPanelType1Component = column.ZOrderedPanels.First().Component;
                            Component columnPanelType3Component = column.ZOrderedPanels.Last().Component;


                            //find feature in part and find occurence in component, order points
                            //for anchorage
                            SketchFeature relevantPartFeatureAnchorage = columnPanelType1Part.Features.ToArray().Where(f => f.Name == "TendonConnectionPoints").First() as SketchFeature; //ERROR!
                            Sketch relevantComponentFeatureAnchorage = columnPanelType1Component.FindOccurrence(relevantPartFeatureAnchorage.Sketch) as Sketch;
                            List<Point> componentPointsAnchorage = relevantComponentFeatureAnchorage.GetAllGeometry().OfType<Point>().ToList();
                            componentPointsAnchorage = componentPointsAnchorage.OrderBy(p => p.Coordinates.Y).ThenBy(p => p.Coordinates.X).ToList();

                            //for upper panel
                            SketchFeature relevantFeaturePanel = columnPanelType3Part.Features.ToArray().Where(f => f.Name == "TendonAnchoragePoints").First() as SketchFeature;
                            Sketch relevantComponentFeaturePanel = columnPanelType3Component.FindOccurrence(relevantFeaturePanel.Sketch) as Sketch;
                            List<Point> componentPointsPanel = relevantComponentFeaturePanel.GetAllGeometry().OfType<Point>().ToList();
                            componentPointsPanel = componentPointsPanel.OrderBy(p => p.Coordinates.Y).ThenBy(p => p.Coordinates.X).ToList();

                            //order points
                            Point lowerXlowerYAnchorage = componentPointsAnchorage.ElementAt(0);
                            Point lowerXhigherYAnchorage = componentPointsAnchorage.ElementAt(1);
                            Point higherXlowerYAnchorage = componentPointsAnchorage.ElementAt(2);
                            Point higherXhigherYAnchorage = componentPointsAnchorage.ElementAt(3);

                            Point lowerXlowerYPanel = componentPointsPanel.ElementAt(0);
                            Point lowerXhigherYPanel = componentPointsPanel.ElementAt(1);
                            Point higherXlowerYPanel = componentPointsPanel.ElementAt(2);
                            Point higherXhigherYPanel = componentPointsPanel.ElementAt(3);

                            //use the objects and geometry to generate tendon objects
                            List<Tendon> columnTendons = new List<Tendon>();
                            columnTendons.Add(new Tendon(column, lowerXlowerYAnchorage, lowerXlowerYPanel));
                            columnTendons.Add(new Tendon(column, lowerXhigherYAnchorage, lowerXhigherYPanel));
                            columnTendons.Add(new Tendon(column, higherXlowerYAnchorage, higherXlowerYPanel));
                            columnTendons.Add(new Tendon(column, higherXhigherYAnchorage, higherXhigherYPanel));
                            column.SetTendons(columnTendons);
                        }
                    }
                }
                return true;
            }

            catch (Exception e)
            {
                Session session = Session.GetSession();
                try
                {
                    int line = new StackTrace(e, true).GetFrame(0).GetFileLineNumber();
                    session.ListingWindow.WriteFullline(string.Format("Exception thrown in TopologyHelper.GenerateTendonObjectsInColumns in line {0}: {1} ", line, e.Message));
                }
                catch
                {
                    session.ListingWindow.WriteFullline("Exception thrown in TopologyHelper.GenerateTendonObjectsInColumns in: " + e.Message);
                }

                return false;
            }
        }

        /// <summary>
        /// computes the topology for the traverses
        /// </summary>
        internal static bool GenerateTraverseObjects(Substructure substructure)
        {
            try
            {
                Session session = Session.GetSession();
                PartLoadStatus pls;
                session.Parts.SetActiveDisplay(substructure.Part, DisplayPartOption.ReplaceExisting, PartDisplayPartWorkPartOption.SameAsDisplay, out pls);
                session.Parts.SetWork(substructure.Part);

                SubsystemModularGroup lowerGroup = substructure.ModularGroups.Where(m => m.Part.Name == "ModularGroupLowerX").First();
                List<ModuleTraverse> traverses = new List<ModuleTraverse>();

                foreach (ModuleKneeNode kneeNode in lowerGroup.Arch.KneeNodes)
                {
                    SketchFeature traverseSupportPointFeature = kneeNode.Part.Features.ToArray().Where(f => f.Name == "TraverseReferencePoint" && !f.Suppressed).First() as SketchFeature;
                    Sketch traverseSupportPointSketch = kneeNode.Component.FindOccurrence(traverseSupportPointFeature.Sketch) as Sketch;
                    Point traverseSupportPointUncasted = traverseSupportPointSketch.GetAllGeometry().OfType<Point>().ToList().First();
                    Point3d traverseSupportPoint = new Point3d(traverseSupportPointUncasted.Coordinates.X, traverseSupportPointUncasted.Coordinates.Y, traverseSupportPointUncasted.Coordinates.Z);
                    traverses.Add(new ModuleTraverse(kneeNode, traverseSupportPoint));
                }
                substructure.AddTraverses(traverses);
                return true;

                //traverses.Add(new ModuleTraverse(lowerGroup.Arch.KneeNodes.ElementAt(0));
            }
            catch (Exception e)
            {
                Session session = Session.GetSession();
                session.ListingWindow.WriteFullline("Exception thrown in TopologyHelper.GenerateTraversObjects in: " + e.Message);
                return false;
            }

        }

    }
}
