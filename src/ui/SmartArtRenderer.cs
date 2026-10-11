using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Graphics = ZenQOffice.UI.D2DGraphics;
using GdiGraphics = System.Drawing.Graphics;

namespace ZenQOffice.UI
{
    public static class SmartArtRenderer
    {
        public static bool SuppressNodeBodies = false;

        public static string[] GetSmartArtNodeList(string smartArtType, string textContent)
        {
            if (!string.IsNullOrEmpty(textContent))
            {
                string[] parts = textContent.Split(new char[] { '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts != null && parts.Length > 0)
                {
                    List<string> clean = new List<string>();
                    for (int i = 0; i < parts.Length; i++)
                    {
                        string t = parts[i].Trim();
                        if (!string.IsNullOrEmpty(t) && !t.StartsWith("🗺️") && !t.StartsWith("❖") && !t.StartsWith("=="))
                        {
                            clean.Add(t);
                        }
                    }
                    if (clean.Count > 0) return clean.ToArray();
                }
            }

            string type = smartArtType ?? "Hierarchy_Labeled";
            if (type.StartsWith("Hierarchy_") || type.Contains("Org") || type.Contains("Tree"))
            {
                return new string[] { "[Text]", "[Text]", "[Text]", "[Text]" };
            }
            else if (type.StartsWith("Process_") || type.Contains("Chevron") || type.Contains("Linear") || type.Contains("Flow"))
            {
                return new string[] { "[Text]", "[Text]", "[Text]" };
            }
            else if (type.StartsWith("Cycle_") || type.Contains("Radial") || type.Contains("Venn") || type.Contains("Ring"))
            {
                return new string[] { "[Text]", "[Text]", "[Text]", "[Text]", "[Text]" };
            }
            else if (type.StartsWith("Matrix_"))
            {
                return new string[] { "[Text]", "[Text]", "[Text]", "[Text]" };
            }
            else if (type.StartsWith("Pyramid_"))
            {
                return new string[] { "[Text]", "[Text]", "[Text]" };
            }
            else if (type.StartsWith("Picture_"))
            {
                return new string[] { "[Text]", "[Text]", "[Text]" };
            }
            else if (type.StartsWith("Rel_") || type.Contains("Balance") || type.Contains("Funnel") || type.Contains("Gear"))
            {
                return new string[] { "[Text]", "[Text]", "[Text]" };
            }
            else
            {
                return new string[] { "[Text]", "[Text]", "[Text]" };
            }
        }

        public static Color[] GetPaletteColors(int presetIndex, int count)
        {
            Color[][] presets = new Color[][]
            {
                // Preset 0: Office Blue Uniform (Default Theme Accent 1)
                new Color[] { Color.FromArgb(68, 114, 196), Color.FromArgb(68, 114, 196), Color.FromArgb(68, 114, 196), Color.FromArgb(68, 114, 196), Color.FromArgb(68, 114, 196) },
                // Preset 1: Office Blue Uniform (Theme Accent 1)
                new Color[] { Color.FromArgb(68, 114, 196), Color.FromArgb(68, 114, 196), Color.FromArgb(68, 114, 196), Color.FromArgb(68, 114, 196), Color.FromArgb(68, 114, 196) },
                // Preset 2: Colorful Accent (Đa Sắc Màu)
                new Color[] { Color.FromArgb(30, 136, 229), Color.FromArgb(67, 160, 71), Color.FromArgb(251, 140, 0), Color.FromArgb(142, 36, 170), Color.FromArgb(229, 57, 53) },
                // Preset 3: Emerald Green (Xanh Lục)
                new Color[] { Color.FromArgb(27, 94, 32), Color.FromArgb(46, 125, 50), Color.FromArgb(67, 160, 71), Color.FromArgb(102, 187, 106), Color.FromArgb(129, 199, 132) },
                // Preset 4: Purple & Magenta (Tím & Hồng)
                new Color[] { Color.FromArgb(74, 20, 140), Color.FromArgb(123, 31, 162), Color.FromArgb(171, 71, 188), Color.FromArgb(216, 27, 96), Color.FromArgb(173, 20, 87) },
                // Preset 5: Monochrome Gray (Đơn Sắc)
                new Color[] { Color.FromArgb(38, 50, 56), Color.FromArgb(55, 71, 79), Color.FromArgb(69, 90, 100), Color.FromArgb(84, 110, 122), Color.FromArgb(120, 144, 156) }
            };

            int pIdx = Math.Max(0, Math.Min(presets.Length - 1, presetIndex));
            Color[] src = presets[pIdx];

            Color[] res = new Color[count];
            for (int i = 0; i < count; i++)
            {
                res[i] = src[i % src.Length];
            }
            return res;
        }

        public static void DrawSmartArt(Graphics g, Rectangle bounds, string smartArtType, string smartArtTitle, Color baseColor, Color outlineColor, string customText, bool isSelected, float zoomLevel)
        {
            DrawSmartArt(g, bounds, smartArtType, smartArtTitle, 0, customText, isSelected, zoomLevel);
        }

        private static Dictionary<string, Action<Graphics, Rectangle, string[], Color[], float>> renderDispatch;

        private static void InitRenderDispatch()
        {
            renderDispatch = new Dictionary<string, Action<Graphics, Rectangle, string[], Color[], float>>();

            // LIST
            renderDispatch["List_AlternatingHexagon"] = (g, r, t, c, s) => DrawHexagonCluster(g, r, t, c, s);
            renderDispatch["List_Tab"] = (g, r, t, c, s) => DrawTabLayout(g, r, t, c, s);
            renderDispatch["List_Trapezoid"] = (g, r, t, c, s) => DrawPyramidLayers(g, r, t, c, s);
            renderDispatch["List_VerticalBracket"] = (g, r, t, c, s) => DrawBracketLayout(g, r, t, c, s);
            renderDispatch["List_Target"] = (g, r, t, c, s) => DrawTargetRings(g, r, t, c, s);
            renderDispatch["List_Pyramid"] = (g, r, t, c, s) => DrawPyramidList(g, r, t, c, s);
            renderDispatch["List_PieProcess"] = (g, r, t, c, s) => DrawCyclePie(g, r, t, c, s);
            renderDispatch["List_VerticalChevron"] = (g, r, t, c, s) => DrawChevronVertical(g, r, t, c, s);
            renderDispatch["List_DescendingBlock"] = (g, r, t, c, s) => DrawStepDown(g, r, t, c, s);
            renderDispatch["List_IncreasingCircle"] = (g, r, t, c, s) => DrawCycleRing(g, r, t, c, s);

            // PROCESS
            Action<Graphics, Rectangle, string[], Color[], float> procChev = (g, r, t, c, s) => DrawProcessChevron(g, r, t, c, s);
            renderDispatch["Process_BasicChevron"] = procChev;
            renderDispatch["Process_ChevronAccent"] = procChev;
            renderDispatch["Process_ClosedChevron"] = procChev;

            Action<Graphics, Rectangle, string[], Color[], float> procBend = (g, r, t, c, s) => DrawProcessBending(g, r, t, c, s);
            renderDispatch["Process_BasicBending"] = procBend;
            renderDispatch["Process_CircularBending"] = procBend;
            renderDispatch["Process_RepeatingBending"] = procBend;
            renderDispatch["Process_VerticalBending"] = procBend;

            Action<Graphics, Rectangle, string[], Color[], float> procTime = (g, r, t, c, s) => DrawTimelineHorizontal(g, r, t, c, s);
            renderDispatch["Process_BasicTimeline"] = procTime;
            renderDispatch["Process_CircleAccentTimeline"] = procTime;
            renderDispatch["Process_TimelinePipe"] = procTime;

            renderDispatch["Process_Gear"] = (g, r, t, c, s) => DrawGearInterlock(g, r, t, c, s);
            renderDispatch["Process_Funnel"] = (g, r, t, c, s) => DrawFunnelShape(g, r, t, c, s);
            renderDispatch["Process_Equation"] = (g, r, t, c, s) => DrawEquationBlocks(g, r, t, c, s);
            renderDispatch["Process_VerticalEquation"] = (g, r, t, c, s) => DrawEquationBlocks(g, r, t, c, s);
            renderDispatch["Process_ConvergingArrows"] = (g, r, t, c, s) => DrawArrowsConverging(g, r, t, c, s);
            renderDispatch["Process_DivergingArrows"] = (g, r, t, c, s) => DrawArrowsConverging(g, r, t, c, s); // reversed in full impl
            renderDispatch["Process_OpposingArrows"] = (g, r, t, c, s) => DrawArrowsOpposing(g, r, t, c, s);
            renderDispatch["Process_ArrowRibbon"] = (g, r, t, c, s) => DrawArrowsOpposing(g, r, t, c, s);

            Action<Graphics, Rectangle, string[], Color[], float> procStep = (g, r, t, c, s) => DrawStepDown(g, r, t, c, s);
            renderDispatch["Process_StepDown"] = procStep;
            renderDispatch["Process_Descending"] = procStep;
            renderDispatch["Process_Staggered"] = procStep;
            renderDispatch["Process_StepUp"] = procStep;
            renderDispatch["Process_UpwardArrow"] = procStep;
            renderDispatch["Process_IncreasingArrows"] = procStep;

            renderDispatch["Process_PieProcess"] = (g, r, t, c, s) => DrawCyclePie(g, r, t, c, s);
            renderDispatch["Process_CircleArrow"] = (g, r, t, c, s) => DrawCycleRing(g, r, t, c, s);
            renderDispatch["Process_AlternatingFlow"] = (g, r, t, c, s) => DrawAlternatingFlow(g, r, t, c, s);
            renderDispatch["Process_Alternating"] = (g, r, t, c, s) => DrawAlternatingFlow(g, r, t, c, s);

            // CYCLE
            Action<Graphics, Rectangle, string[], Color[], float> cycRadial = (g, r, t, c, s) => DrawCycleRadial(g, r, t, c, 0, s);
            renderDispatch["Cycle_BasicRadial"] = cycRadial;
            renderDispatch["Cycle_Radial"] = cycRadial;
            renderDispatch["Cycle_RadialCluster"] = cycRadial;
            renderDispatch["Cycle_DivergingRadial"] = cycRadial;
            
            renderDispatch["Cycle_BasicPie"] = (g, r, t, c, s) => DrawCycleBasicPie(g, r, t, c, s);
            renderDispatch["Cycle_Segmented"] = (g, r, t, c, s) => DrawCycleSegmented(g, r, t, c, s);
            renderDispatch["Cycle_BlockLoop"] = (g, r, t, c, s) => DrawCycleBlockLoop(g, r, t, c, s);

            renderDispatch["Cycle_Gear"] = (g, r, t, c, s) => DrawGearInterlock(g, r, t, c, s);
            renderDispatch["Cycle_CycleMatrix"] = (g, r, t, c, s) => DrawMatrixGrid(g, r, t, c, s);
            renderDispatch["Cycle_RadialVenn"] = (g, r, t, c, s) => DrawVennDiagram(g, r, t, c, s);

            Action<Graphics, Rectangle, string[], Color[], float> cycRing = (g, r, t, c, s) => DrawCycleRing(g, r, t, c, s);
            renderDispatch["Cycle_Ring"] = cycRing;
            renderDispatch["Cycle_Basic"] = cycRing;
            renderDispatch["Cycle_BasicCycle"] = cycRing;
            renderDispatch["Cycle_Block"] = cycRing;
            renderDispatch["Cycle_Continuous"] = cycRing;
            renderDispatch["Cycle_Text"] = cycRing;
            renderDispatch["Cycle_Nondirectional"] = cycRing;
            renderDispatch["Cycle_Multidirectional"] = cycRing;
            renderDispatch["Cycle_CircleArrow"] = (g, r, t, c, s) => DrawCycleCircleArrow(g, r, t, c, s);
            renderDispatch["Cycle_CircleArrowProcess"] = (g, r, t, c, s) => DrawCycleCircleArrow(g, r, t, c, s);

            // HIERARCHY
            renderDispatch["Hierarchy_Labeled"] = (g, r, t, c, s) => DrawHierarchyLabeled(g, r, t, c, s);
            Action<Graphics, Rectangle, string[], Color[], float> hierHoriz = (g, r, t, c, s) => DrawHierarchyHorizontal(g, r, t, c, s);
            renderDispatch["Hierarchy_Horizontal"] = hierHoriz;
            renderDispatch["Hierarchy_HorizontalOrg"] = hierHoriz;
            renderDispatch["Hierarchy_HorizontalLabeled"] = hierHoriz;
            renderDispatch["Hierarchy_HorizontalMultiLevel"] = hierHoriz;
            renderDispatch["Hierarchy_Architecture"] = procStep;
            renderDispatch["Hierarchy_Table"] = (g, r, t, c, s) => DrawListBlocks(g, r, t, c, s);
            // Default tree fallback handled in DrawSmartArt

            // RELATIONSHIP
            renderDispatch["Rel_Balance"] = (g, r, t, c, s) => DrawBalanceScale(g, r, t, c, s);
            renderDispatch["Rel_CounterbalanceArrows"] = (g, r, t, c, s) => DrawBalanceScale(g, r, t, c, s);
            renderDispatch["Rel_Funnel"] = (g, r, t, c, s) => DrawFunnelShape(g, r, t, c, s);
            renderDispatch["Rel_Gear"] = (g, r, t, c, s) => DrawGearInterlock(g, r, t, c, s);
            renderDispatch["Rel_BasicTarget"] = (g, r, t, c, s) => DrawTargetRings(g, r, t, c, s);
            renderDispatch["Rel_NestedTarget"] = (g, r, t, c, s) => DrawTargetRings(g, r, t, c, s);
            renderDispatch["Rel_TargetList"] = (g, r, t, c, s) => DrawTargetRings(g, r, t, c, s);
            
            Action<Graphics, Rectangle, string[], Color[], float> relVenn = (g, r, t, c, s) => DrawVennDiagram(g, r, t, c, s);
            renderDispatch["Rel_BasicVenn"] = relVenn;
            renderDispatch["Rel_LinearVenn"] = relVenn;
            renderDispatch["Rel_RadialVenn"] = relVenn;
            renderDispatch["Rel_StackedVenn"] = relVenn;
            
            renderDispatch["Rel_Equation"] = (g, r, t, c, s) => DrawEquationBlocks(g, r, t, c, s);
            renderDispatch["Rel_HexagonCluster"] = (g, r, t, c, s) => DrawHexagonCluster(g, r, t, c, s);
            renderDispatch["Rel_BasicRadial"] = cycRadial;
            renderDispatch["Rel_RadialCluster"] = cycRadial;
            renderDispatch["Rel_ConvergingArrows"] = (g, r, t, c, s) => DrawArrowsConverging(g, r, t, c, s);
            renderDispatch["Rel_DivergingArrows"] = (g, r, t, c, s) => DrawArrowsConverging(g, r, t, c, s);
            renderDispatch["Rel_OpposingArrows"] = (g, r, t, c, s) => DrawArrowsOpposing(g, r, t, c, s);
            renderDispatch["Rel_ArrowRibbon"] = (g, r, t, c, s) => DrawArrowsOpposing(g, r, t, c, s);
            renderDispatch["Rel_OpposingIdeas"] = (g, r, t, c, s) => DrawArrowsOpposing(g, r, t, c, s);
            renderDispatch["Rel_BasicPie"] = (g, r, t, c, s) => DrawCycleBasicPie(g, r, t, c, s);
            renderDispatch["Rel_NondirectionalCycle"] = cycRing;
            renderDispatch["Rel_RadialCycle"] = cycRing;
            renderDispatch["Rel_CycleMatrix"] = (g, r, t, c, s) => DrawMatrixGrid(g, r, t, c, s);
            renderDispatch["Rel_SegmentedPyramid"] = (g, r, t, c, s) => DrawPyramidLayers(g, r, t, c, s);

            // MATRIX
            Action<Graphics, Rectangle, string[], Color[], float> matGrid = (g, r, t, c, s) => DrawMatrixGrid(g, r, t, c, s);
            renderDispatch["Matrix_Basic"] = matGrid;
            renderDispatch["Matrix_Grid"] = matGrid;
            renderDispatch["Matrix_Titled"] = matGrid;
            renderDispatch["Matrix_Cycle"] = cycRadial;

            // PYRAMID
            renderDispatch["Pyramid_Basic"] = (g, r, t, c, s) => DrawPyramidLayers(g, r, t, c, s);
            renderDispatch["Pyramid_Segmented"] = (g, r, t, c, s) => DrawPyramidLayers(g, r, t, c, s);
            renderDispatch["Pyramid_Inverted"] = (g, r, t, c, s) => DrawPyramidInverted(g, r, t, c, s);
            renderDispatch["Pyramid_List"] = (g, r, t, c, s) => DrawPyramidList(g, r, t, c, s);

            // PICTURE
            renderDispatch["Picture_Grid"] = (g, r, t, c, s) => DrawPictureGrid(g, r, t, c, s);
            renderDispatch["Picture_AlternatingBlocks"] = (g, r, t, c, s) => DrawPictureAlternatingBlocks(g, r, t, c, s);
            Action<Graphics, Rectangle, string[], Color[], float> picStrips = (g, r, t, c, s) => DrawPictureStrips(g, r, t, c, s);
            renderDispatch["Picture_Strips"] = picStrips;
            renderDispatch["Picture_Lineup"] = picStrips;
            renderDispatch["Picture_HorizontalList"] = picStrips;
            renderDispatch["Picture_CircleHierarchy"] = (g, r, t, c, s) => DrawHierarchyTree(g, r, t, c, s);
            renderDispatch["Picture_HexagonCluster"] = (g, r, t, c, s) => DrawHexagonCluster(g, r, t, c, s);
            renderDispatch["Picture_Spiral"] = cycRing;

            // OFFICE.COM
            renderDispatch["Office_CircleProcess"] = cycRing;
            renderDispatch["Office_HexagonRadial"] = (g, r, t, c, s) => DrawHexagonCluster(g, r, t, c, s);
            renderDispatch["Office_TabList"] = (g, r, t, c, s) => DrawTabLayout(g, r, t, c, s);
        }

        public static void PopulateSubShapeBounds(Rectangle bounds, string smartArtType, int count, DocShapeObject shape)
        {
            if (shape == null || count <= 0) return;
            shape.SubShapeRelativeBounds.Clear();
            string type = smartArtType ?? "List_BasicBlock";

            if (type.StartsWith("Hierarchy_") || type.Contains("Tree") || type.Contains("Org"))
            {
                // Root node
                shape.SubShapeRelativeBounds.Add(new RectangleF(0.35f, 0.05f, 0.30f, 0.28f));
                int children = Math.Max(1, count - 1);
                float cw = Math.Min(0.28f, (0.92f - (children - 1) * 0.03f) / children);
                float startX = (1.0f - (children * cw + (children - 1) * 0.03f)) / 2.0f;
                for (int i = 0; i < children; i++)
                {
                    shape.SubShapeRelativeBounds.Add(new RectangleF(startX + i * (cw + 0.03f), 0.58f, cw, 0.34f));
                }
            }
            else if (type.Contains("Cycle") || type.Contains("Radial") || type.Contains("Ring") || type.Contains("BlockLoop") || type.Contains("Segmented") || type.Contains("Gear"))
            {
                float bw = Math.Max(10f, bounds.Width);
                float bh = Math.Max(10f, bounds.Height);
                float minDim = Math.Min(bounds.Width, bounds.Height);
                float pixelRad = Math.Max(30f, Math.Min(300f, minDim * 0.32f));

                if (shape.NodeShapeTypes == null) shape.NodeShapeTypes = new Dictionary<int, string>();

                if (type.Contains("Radial"))
                {
                    shape.AspectRatioLock = true;
                    // For Cycle_Radial: The FIRST node (i=0) MUST be the Central Core Hub!
                    // Position it at relative center (0.5f, 0.5f) with diameter 2 * R_core where R_core = pixelRad * 0.45f
                    float rCore = pixelRad * 0.45f;
                    float coreDiam = rCore * 2f;
                    float coreRelW = coreDiam / bw;
                    float coreRelH = coreDiam / bh;

                    shape.SubShapeRelativeBounds.Add(new RectangleF(0.5f - coreRelW / 2.0f, 0.5f - coreRelH / 2.0f, coreRelW, coreRelH));
                    if (!shape.NodeShapeTypes.ContainsKey(0)) shape.NodeShapeTypes[0] = "Circle";

                    int satCount = count - 1;
                    if (satCount > 0)
                    {
                        float maxR = minDim * 0.48f;
                        float rSat = (maxR - rCore) / 2f;
                        if (satCount > 1)
                        {
                            float sinVal = (float)Math.Sin(Math.PI / satCount);
                            if (sinVal < 0.99f && sinVal > 0f)
                            {
                                float rSatTangent = rCore * sinVal / (1f - sinVal);
                                if (rSatTangent < rSat) rSat = rSatTangent;
                            }
                        }
                        rSat = Math.Max(12f, Math.Min(rSat, pixelRad * 0.38f));

                        float deltaGap = 0f; // Seamless outer attachment to central circle perimeter
                        float rOrbit = rCore + rSat + deltaGap;
                        float satDiam = rSat * 2f;
                        float satRelW = satDiam / bw;
                        float satRelH = satDiam / bh;

                        for (int i = 1; i < count; i++)
                        {
                            double angleDeg = ((i - 1) * (360.0 / satCount)) - 90.0;
                            double rad = angleDeg * Math.PI / 180.0;
                            float cx = 0.5f + (rOrbit * (float)Math.Cos(rad)) / bw;
                            float cy = 0.5f + (rOrbit * (float)Math.Sin(rad)) / bh;
                            shape.SubShapeRelativeBounds.Add(new RectangleF(cx - satRelW / 2.0f, cy - satRelH / 2.0f, satRelW, satRelH));
                            if (!shape.NodeShapeTypes.ContainsKey(i)) shape.NodeShapeTypes[i] = "Circle";
                        }
                    }
                }
                else
                {
                    // Non-radial cycle (Ring, CircleArrow, BlockLoop, Segmented, Gear, BasicPie, Nondirectional)
                    shape.AspectRatioLock = false;
                    float rOrbit = pixelRad;
                    float nodeW = Math.Max(32f, minDim * 0.22f);
                    float nodeH = Math.Max(20f, minDim * 0.13f);
                    float satRelW = nodeW / bw;
                    float satRelH = nodeH / bh;

                    for (int i = 0; i < count; i++)
                    {
                        double angleDeg = (i * (360.0 / count)) - 90.0;
                        double rad = angleDeg * Math.PI / 180.0;
                        float cx = 0.5f + (rOrbit * (float)Math.Cos(rad)) / bw;
                        float cy = 0.5f + (rOrbit * (float)Math.Sin(rad)) / bh;
                        shape.SubShapeRelativeBounds.Add(new RectangleF(cx - satRelW / 2.0f, cy - satRelH / 2.0f, satRelW, satRelH));
                        if (!shape.NodeShapeTypes.ContainsKey(i)) shape.NodeShapeTypes[i] = "RoundedRectangle";
                    }
                }
            }
            else if (type.StartsWith("Matrix_"))
            {
                // 2x2 grid
                shape.SubShapeRelativeBounds.Add(new RectangleF(0.06f, 0.06f, 0.42f, 0.42f));
                shape.SubShapeRelativeBounds.Add(new RectangleF(0.52f, 0.06f, 0.42f, 0.42f));
                shape.SubShapeRelativeBounds.Add(new RectangleF(0.06f, 0.52f, 0.42f, 0.42f));
                shape.SubShapeRelativeBounds.Add(new RectangleF(0.52f, 0.52f, 0.42f, 0.42f));
            }
            else if (type.StartsWith("Pyramid_"))
            {
                // Slices from top to bottom
                float sh = 0.88f / count;
                for (int i = 0; i < count; i++)
                {
                    float sw = 0.30f + 0.65f * ((float)(i + 1) / count);
                    shape.SubShapeRelativeBounds.Add(new RectangleF((1.0f - sw) / 2.0f, 0.06f + i * sh, sw, sh * 0.90f));
                }
            }
            else if (type.Contains("Vertical"))
            {
                // Vertical list
                float sh = (0.92f - (count - 1) * 0.03f) / count;
                for (int i = 0; i < count; i++)
                {
                    shape.SubShapeRelativeBounds.Add(new RectangleF(0.08f, 0.04f + i * (sh + 0.03f), 0.84f, sh));
                }
            }
            else if (type.Contains("Alternating") || type.Contains("hProcess4"))
            {
                // Alternating cards and pills
                float gap = 0.04f;
                float cw = Math.Min(0.28f, (0.92f - (count - 1) * gap) / count);
                float startX = (1.0f - (count * cw + (count - 1) * gap)) / 2.0f;
                for (int i = 0; i < count; i++)
                {
                    bool isTop = (i % 2 == 1);
                    float py = isTop ? 0.08f : 0.60f;
                    shape.SubShapeRelativeBounds.Add(new RectangleF(startX + i * (cw + gap), py, cw, 0.32f));
                }
            }
            else
            {
                // Horizontal process / list / chevron
                float gap = 0.03f;
                float sw = (0.94f - (count - 1) * gap) / count;
                for (int i = 0; i < count; i++)
                {
                    shape.SubShapeRelativeBounds.Add(new RectangleF(0.03f + i * (sw + gap), 0.15f, sw, 0.70f));
                }
            }
        }

        public static void DrawSmartArt(Graphics g, Rectangle bounds, DocShapeObject shape, float zoomLevel)
        {
            if (shape == null || bounds.Width <= 10 || bounds.Height <= 10) return;
            string type = shape.SmartArtType ?? "List_BasicBlock";
            string[] nodes = GetSmartArtNodeList(type, shape.TextContent);
            Color[] colors;
            if (shape.ResolvedThemePalette != null && shape.ResolvedThemePalette.Length > 0)
            {
                colors = new Color[nodes.Length];
                for (int i = 0; i < nodes.Length; i++)
                {
                    colors[i] = (shape.StylePresetIndex == 2)
                        ? shape.ResolvedThemePalette[i % shape.ResolvedThemePalette.Length]
                        : shape.ResolvedThemePalette[0];
                }
            }
            else
            {
                colors = GetPaletteColors(shape.StylePresetIndex, nodes.Length);
            }

            if (shape.NodeColors != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (shape.NodeColors.ContainsKey(i)) colors[i] = shape.NodeColors[i];
                }
            }

            if (shape.SubShapeRelativeBounds == null || shape.SubShapeRelativeBounds.Count == 0)
            {
                PopulateSubShapeBounds(bounds, type, nodes.Length, shape);
            }

            DrawSmartArt(g, bounds, type, shape.SmartArtTitle, colors, nodes, shape.IsSelected, zoomLevel);

            if (shape.OutlineWidth > 0 && shape.OutlineColor != Color.Transparent && shape.OutlineColor != Color.Empty)
            {
                string bStyle = "solid";
                if (shape.OutlineDashStyle == DashStyle.Dash) bStyle = "dashed";
                else if (shape.OutlineDashStyle == DashStyle.Dot) bStyle = "dotted";
                else if (shape.VisualStylePreset == 2) bStyle = "3d";
                else if (shape.HasShadow) bStyle = "shadow";
                DrawStyledBorder(g, bounds, bStyle, shape.OutlineColor, shape.OutlineWidth, zoomLevel);
            }
        }

        public static void DrawSmartArt(Graphics g, Rectangle bounds, string smartArtType, string smartArtTitle, int colorPresetIndex, string customText, bool isSelected, float zoomLevel)
        {
            if (bounds.Width <= 10 || bounds.Height <= 10) return;
            string type = smartArtType ?? "Hierarchy_Labeled";
            string[] nodes = GetSmartArtNodeList(type, customText);
            Color[] colors = GetPaletteColors(colorPresetIndex, nodes.Length);
            DrawSmartArt(g, bounds, type, smartArtTitle, colors, nodes, isSelected, zoomLevel);
        }

        public static void DrawSmartArt(GdiGraphics g, Rectangle bounds, DocShapeObject shape, float zoomLevel)
        {
            if (g == null) return;
            using (var d2d = new Graphics(g, bounds.Width, bounds.Height))
            {
                DrawSmartArt(d2d, bounds, shape, zoomLevel);
            }
        }

        public static void DrawSmartArt(GdiGraphics g, Rectangle bounds, string smartArtType, string smartArtTitle, int colorPresetIndex, string customText, bool isSelected, float zoomLevel)
        {
            if (g == null) return;
            using (var d2d = new Graphics(g, bounds.Width, bounds.Height))
            {
                DrawSmartArt(d2d, bounds, smartArtType, smartArtTitle, colorPresetIndex, customText, isSelected, zoomLevel);
            }
        }

        public static void DrawSmartArt(Graphics g, Rectangle bounds, string smartArtType, string smartArtTitle, Color[] colors, string[] nodes, bool isSelected, float zoomLevel)
        {
            if (bounds.Width <= 10 || bounds.Height <= 10) return;

            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            string type = smartArtType ?? "Hierarchy_Labeled";

            Rectangle contentRect = bounds;

            if (renderDispatch == null) InitRenderDispatch();

            Action<Graphics, Rectangle, string[], Color[], float> renderer;
            if (renderDispatch.TryGetValue(type, out renderer))
            {
                renderer(g, contentRect, nodes, colors, zoomLevel);
            }
            else
            {
                // Fallbacks for unknown or generalized types
                if (type.StartsWith("Hierarchy_") || type.Contains("Org") || type.Contains("Tree"))
                    DrawHierarchyTree(g, contentRect, nodes, colors, zoomLevel);
                else if (type.StartsWith("Cycle_") || type.Contains("Ring") || type.Contains("Radial") || type.Contains("Cycle"))
                    DrawCycleRing(g, contentRect, nodes, colors, zoomLevel);
                else if (type.StartsWith("Process_") || type.Contains("Linear") || type.Contains("Flow"))
                    DrawProcessLinear(g, contentRect, nodes, colors, zoomLevel);
                else if (type.StartsWith("Picture_"))
                    DrawPictureBlocks(g, contentRect, nodes, colors, zoomLevel);
                else
                    DrawListBlocks(g, contentRect, nodes, colors, zoomLevel);
            }

            // Selection Handles if Selected
            if (isSelected)
            {
                using (Pen selPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                {
                    selPen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(selPen, bounds);
                }
                int hs = (int)(7 * zoomLevel);
                Point[] handles = new Point[]
                {
                    new Point(bounds.Left, bounds.Top),
                    new Point(bounds.Left + bounds.Width / 2, bounds.Top),
                    new Point(bounds.Right, bounds.Top),
                    new Point(bounds.Right, bounds.Top + bounds.Height / 2),
                    new Point(bounds.Right, bounds.Bottom),
                    new Point(bounds.Left + bounds.Width / 2, bounds.Bottom),
                    new Point(bounds.Left, bounds.Bottom),
                    new Point(bounds.Left, bounds.Top + bounds.Height / 2)
                };
                for (int hi = 0; hi < handles.Length; hi++)
                {
                    Rectangle hR = new Rectangle(handles[hi].X - hs / 2, handles[hi].Y - hs / 2, hs, hs);
                    g.FillRectangle(Brushes.White, hR);
                    using (Pen hPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                    {
                        g.DrawRectangle(hPen, hR);
                    }
                }
            }
        }

        // ==================== 1. CYCLE RADIAL (EXACT MATCH TO MS OFFICE) ====================
        private static void DrawCycleRadial(Graphics g, Rectangle r, string[] nodes, Color[] colors, int colorPreset, float zoom)
        {
            int count = (nodes != null) ? nodes.Length : 0;
            if (count == 0) return;

            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            float minDim = Math.Min(r.Width, r.Height);
            float pixelRad = Math.Max(30f, Math.Min(300f, minDim * 0.32f));
            float rCore = pixelRad * 0.45f;

            int satCount = count - 1;
            float maxR = minDim * 0.48f;
            float rSat = (maxR - rCore) / 2f;
            if (satCount > 1)
            {
                float sinVal = (float)Math.Sin(Math.PI / satCount);
                if (sinVal < 0.99f && sinVal > 0f)
                {
                    float rSatTangent = rCore * sinVal / (1f - sinVal);
                    if (rSatTangent < rSat) rSat = rSatTangent;
                }
            }
            rSat = Math.Max(12f, Math.Min(rSat, pixelRad * 0.38f));
            float rOrbit = rCore + rSat;

            // Connecting lines / spokes from tangent contact on core circle to satellites
            using (Pen spokePen = new Pen(Color.FromArgb(160, 185, 215), 1.8f * zoom))
            {
                for (int i = 0; i < satCount; i++)
                {
                    double angleDeg = (i * (360.0 / satCount)) - 90.0;
                    double rad = angleDeg * Math.PI / 180.0;
                    int sx = cx + (int)(rCore * Math.Cos(rad));
                    int sy = cy + (int)(rCore * Math.Sin(rad));
                    int ex = cx + (int)(rOrbit * Math.Cos(rad));
                    int ey = cy + (int)(rOrbit * Math.Sin(rad));
                    g.DrawLine(spokePen, sx, sy, ex, ey);
                }
            }

            // Central Core Hub outline ring (TRUE 1:1 CIRCLE)
            Color ringCol = (colors != null && colors.Length > 0) ? colors[0] : Color.FromArgb(68, 114, 196);
            using (Pen ringPen = new Pen(ringCol, 1.8f * zoom))
            {
                g.DrawEllipse(ringPen, cx - rCore, cy - rCore, rCore * 2f, rCore * 2f);
            }

            // Draw bodies when not suppressed
            if (!SuppressNodeBodies)
            {
                // 1. Draw Central Core Hub (i = 0)
                RectangleF coreRect = new RectangleF(cx - rCore, cy - rCore, rCore * 2f, rCore * 2f);
                using (SolidBrush cb = new SolidBrush(ringCol))
                {
                    g.FillEllipse(cb, coreRect);
                }
                using (Pen cp = new Pen(ControlPaint.Dark(ringCol, 0.2f), 1.5f * zoom))
                {
                    g.DrawEllipse(cp, coreRect.X, coreRect.Y, coreRect.Width, coreRect.Height);
                }
                Rectangle coreTextRect = new Rectangle((int)coreRect.X, (int)coreRect.Y, (int)coreRect.Width, (int)coreRect.Height);
                DrawAutoFitText(g, nodes[0], coreTextRect, Color.White);

                // 2. Draw Satellite Nodes (i = 1..count-1)
                for (int i = 1; i < count; i++)
                {
                    double angleDeg = ((i - 1) * (360.0 / satCount)) - 90.0;
                    double rad = angleDeg * Math.PI / 180.0;
                    int nx = cx + (int)(rOrbit * Math.Cos(rad));
                    int ny = cy + (int)(rOrbit * Math.Sin(rad));
                    Rectangle satRect = new Rectangle(nx - (int)rSat, ny - (int)rSat, (int)(rSat * 2f), (int)(rSat * 2f));

                    Color c = colors[i % colors.Length];
                    using (SolidBrush b = new SolidBrush(c))
                    {
                        g.FillEllipse(b, satRect);
                    }
                    using (Pen p = new Pen(ControlPaint.Dark(c, 0.2f), 1.5f * zoom))
                    {
                        g.DrawEllipse(p, satRect);
                    }
                    DrawAutoFitText(g, nodes[i], satRect, Color.White);
                }
            }
        }

        private static void DrawAutoFitText(Graphics g, string text, Rectangle rect, Color color, float maxFontSize = 14f, float minFontSize = 6.5f)
        {
            if (string.IsNullOrEmpty(text) || rect.Width <= 4 || rect.Height <= 4) return;
            float fSize = Math.Max(minFontSize, Math.Min(maxFontSize, rect.Height * 0.24f));
            while (fSize > minFontSize)
            {
                using (Font testF = new Font("Segoe UI", fSize, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(text, testF, rect.Width - 4);
                    if (sz.Height <= rect.Height - 4) break;
                }
                fSize -= 0.5f;
            }

            using (Font f = new Font("Segoe UI", fSize, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(color))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
            {
                g.DrawString(text, f, b, rect, sf);
            }
        }

        // ==================== 2. PROCESS LINEAR ====================
        private static void DrawProcessLinear(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int count = nodes.Length;
            if (count <= 0) return;

            int arrowW = Math.Max(12, Math.Min(30, (int)(r.Width * 0.08f)));
            int arrowPad = Math.Max(4, (int)(r.Width * 0.025f));
            int totalSpacing = (arrowW + arrowPad * 2) * (count - 1);
            int cardW = Math.Max(30, (r.Width - totalSpacing) / count);
            int cardH = Math.Min(r.Height - 12, Math.Max(40, (int)(r.Height * 0.65f)));
            int cardY = r.Y + (r.Height - cardH) / 2;

            int curX = r.X;
            for (int i = 0; i < count; i++)
            {
                if (!SuppressNodeBodies)
                {
                    Rectangle cardR = new Rectangle(curX, cardY, cardW, cardH);
                    Color c = colors[i % colors.Length];

                    using (SolidBrush cb = new SolidBrush(c))
                    {
                        g.FillRectangle(cb, cardR);
                    }

                    DrawAutoFitText(g, nodes[i], cardR, Color.White);
                }

                curX += cardW;

                // Connecting Right Arrow
                if (i < count - 1)
                {
                    int arrX = curX + arrowPad;
                    int arrY = cardY + cardH / 2;
                    DrawVectorArrowRight(g, arrX, arrY, arrowW, Math.Max(10, (int)(arrowW * 0.7f)), Color.FromArgb(148, 163, 184));
                    curX += arrowW + arrowPad * 2;
                }
            }
        }

        // ==================== 3. PROCESS CHEVRON ====================
        private static void DrawProcessChevron(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count <= 0) return;

            int notch = Math.Max(16, (int)(36 * zoom));
            int chW = (r.Width + (count - 1) * notch) / count;
            int chH = Math.Min(r.Height, Math.Max(120, (int)(r.Height * 0.65f)));
            int chY = r.Y + (r.Height - chH) / 2;

            int curX = r.X;
            for (int i = 0; i < count; i++)
            {
                PointF[] pts = new PointF[]
                {
                    new PointF(curX, chY),
                    new PointF(curX + chW - notch, chY),
                    new PointF(curX + chW, chY + chH / 2f),
                    new PointF(curX + chW - notch, chY + chH),
                    new PointF(curX, chY + chH),
                    new PointF(curX + (i == 0 ? 0 : notch), chY + chH / 2f)
                };

                using (GraphicsPath cp = new GraphicsPath())
                {
                    cp.AddPolygon(pts);
                    Color c = colors[i % colors.Length];
                    using (SolidBrush cBrush = new SolidBrush(c))
                    {
                        g.FillPath(cBrush, cp);
                    }
                    using (Pen p = new Pen(Color.White, 2.5f * zoom))
                    {
                        g.DrawPath(p, cp);
                    }
                }

                int textLeft = curX + (i == 0 ? (int)(10 * zoom) : notch + (int)(6 * zoom));
                int textW = Math.Max(20, chW - notch - (int)(16 * zoom));
                Rectangle tRect = new Rectangle(textLeft, chY, textW, chH);

                DrawAutoFitText(g, nodes[i], tRect, Color.White);

                curX += chW - notch;
            }
        }

        // ==================== 4. HIERARCHY TREE ====================
        private static void DrawHierarchyTree(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (nodes.Length == 0) return;

            // Root box (nodes[0])
            int rootW = Math.Max(100, Math.Min(340, (int)(r.Width * 0.35f)));
            int rootH = Math.Max(40, Math.Min(90, (int)(r.Height * 0.22f)));
            int rootX = r.X + (r.Width - rootW) / 2;
            int rootY = r.Y + (int)(10 * zoom);
            Rectangle rootRect = new Rectangle(rootX, rootY, rootW, rootH);

            if (!SuppressNodeBodies)
            {
                Color rootCol = colors[0];
                using (SolidBrush rb = new SolidBrush(rootCol))
                {
                    g.FillRectangle(rb, rootRect);
                }
                float rfSize = Math.Max(12f, Math.Min(24f, rootH * 0.32f));
                using (Font rf = new Font("Segoe UI", rfSize, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(nodes[0], rf, Brushes.White, rootRect, sf);
                }
            }

            // Children boxes (nodes[1..N])
            int childCount = nodes.Length - 1;
            if (childCount <= 0) return;

            int gap = Math.Max(12, (int)(24 * zoom));
            int childW = (r.Width - gap * (childCount - 1)) / childCount;
            int childH = Math.Max(40, Math.Min(90, (int)(r.Height * 0.22f)));
            int childY = r.Bottom - childH - (int)(10 * zoom);

            int stemMidY = rootRect.Bottom + (childY - rootRect.Bottom) / 2;
            using (Pen linePen = new Pen(Color.FromArgb(100, 116, 139), 2.5f * zoom))
            {
                // Vertical line from root
                g.DrawLine(linePen, r.X + r.Width / 2, rootRect.Bottom, r.X + r.Width / 2, stemMidY);
                // Horizontal bar
                int leftMidX = r.X + childW / 2;
                int rightMidX = r.Right - childW / 2;
                g.DrawLine(linePen, leftMidX, stemMidY, rightMidX, stemMidY);
            }

            int curX = r.X;
            for (int i = 1; i <= childCount; i++)
            {
                Rectangle cRect = new Rectangle(curX, childY, childW, childH);
                using (Pen linePen = new Pen(Color.FromArgb(100, 116, 139), 2.5f * zoom))
                {
                    g.DrawLine(linePen, cRect.X + cRect.Width / 2, stemMidY, cRect.X + cRect.Width / 2, cRect.Y);
                }

                if (!SuppressNodeBodies)
                {
                    Color cc = colors[i % colors.Length];
                    using (SolidBrush cb = new SolidBrush(cc))
                    {
                        g.FillRectangle(cb, cRect);
                    }

                    float cfSize = Math.Max(11f, Math.Min(22f, childH * 0.30f));
                    using (Font cf = new Font("Segoe UI", cfSize, FontStyle.Bold))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                    {
                        g.DrawString(nodes[i], cf, Brushes.White, cRect, sf);
                    }
                }

                curX += childW + gap;
            }
        }

        // ==================== 4b. HIERARCHY LABELED (MS OFFICE SCREENSHOT PARITY) ====================
        private static void DrawHierarchyLabeled(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            Color bandBg = Color.FromArgb(247, 229, 222); // #F7E5DE
            int bandH = Math.Max(28, (r.Height - (int)(16 * zoom)) / 3);
            int gap = Math.Max(4, (int)(8 * zoom));
            int radius = (int)(6 * zoom);

            for (int b = 0; b < 3; b++)
            {
                int by = r.Y + (int)(4 * zoom) + b * (bandH + gap);
                Rectangle bandRect = new Rectangle(r.X + (int)(4 * zoom), by, r.Width - (int)(8 * zoom), bandH);
                using (GraphicsPath path = GetRoundedPath(bandRect, radius))
                using (SolidBrush bb = new SolidBrush(bandBg))
                {
                    g.FillPath(bb, path);
                }

                // Horizontal dash on left
                using (Pen dashPen = new Pen(Color.FromArgb(120, 120, 120), 2f * zoom))
                {
                    g.DrawLine(dashPen, r.X + (int)(16 * zoom), by + bandH / 2, r.X + (int)(28 * zoom), by + bandH / 2);
                }
            }

            // Stems (Orange branch lines)
            using (Pen connPen = new Pen(Color.FromArgb(237, 125, 49), 2f * zoom))
            {
                int topNodeBottomY = r.Y + (int)(4 * zoom) + bandH;
                int midNodeTopY = r.Y + (int)(4 * zoom) + bandH + gap;
                int midNodeBottomY = midNodeTopY + bandH;
                int botNodeTopY = r.Y + (int)(4 * zoom) + (bandH + gap) * 2;

                int topX = r.X + (int)(r.Width * 0.72f);
                int midX1 = topX - (int)(60 * zoom);
                int midX2 = topX + (int)(40 * zoom);

                g.DrawLine(connPen, topX, topNodeBottomY - 4, topX, midNodeTopY + 4);
                g.DrawLine(connPen, midX1, midNodeTopY - 2, midX2, midNodeTopY - 2);
                g.DrawLine(connPen, midX1, midNodeTopY - 2, midX1, midNodeTopY + 4);
                g.DrawLine(connPen, midX2, midNodeTopY - 2, midX2, midNodeTopY + 4);

                int botX1 = midX1 - (int)(30 * zoom);
                int botX2 = midX1 + (int)(25 * zoom);
                int botX3 = midX2 + (int)(25 * zoom);
                g.DrawLine(connPen, midX1, midNodeBottomY - 4, midX1, botNodeTopY + 4);
                g.DrawLine(connPen, botX1, botNodeTopY - 2, botX3, botNodeTopY - 2);
                g.DrawLine(connPen, botX1, botNodeTopY - 2, botX1, botNodeTopY + 4);
                g.DrawLine(connPen, botX2, botNodeTopY - 2, botX2, botNodeTopY + 4);
                g.DrawLine(connPen, botX3, botNodeTopY - 2, botX3, botNodeTopY + 4);
            }

            if (SuppressNodeBodies) return;

            // Top Node (Blue gradient)
            int nodeW = Math.Max(60, (int)(80 * zoom));
            int nodeH = Math.Max(22, (int)(bandH * 0.8f));
            int topNodeX = r.X + (int)(r.Width * 0.72f) - nodeW / 2;
            int topNodeY = r.Y + (int)(4 * zoom) + (bandH - nodeH) / 2;
            string t0 = (nodes.Length > 0) ? nodes[0] : "Lãnh đạo";
            DrawGradientCard(g, new Rectangle(topNodeX, topNodeY, nodeW, nodeH),
                Color.FromArgb(91, 155, 213), Color.FromArgb(52, 113, 177), Color.FromArgb(43, 91, 136), t0, zoom);

            // Middle Nodes (Orange gradient)
            int midY = r.Y + (int)(4 * zoom) + bandH + gap + (bandH - nodeH) / 2;
            int mid1X = r.X + (int)(r.Width * 0.72f) - (int)(60 * zoom) - nodeW / 2;
            int mid2X = r.X + (int)(r.Width * 0.72f) + (int)(40 * zoom) - nodeW / 2;
            string t1 = (nodes.Length > 1) ? nodes[1] : "Quản lý A";
            string t2 = (nodes.Length > 2) ? nodes[2] : "Quản lý B";
            DrawGradientCard(g, new Rectangle(mid1X, midY, nodeW, nodeH),
                Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), t1, zoom);
            DrawGradientCard(g, new Rectangle(mid2X, midY, nodeW, nodeH),
                Color.FromArgb(237, 125, 49), Color.FromArgb(198, 89, 17), Color.FromArgb(162, 70, 11), t2, zoom);

            // Bottom Nodes (Gray gradient)
            int botY = r.Y + (int)(4 * zoom) + (bandH + gap) * 2 + (bandH - nodeH) / 2;
            int bot1X = mid1X - (int)(30 * zoom);
            int bot2X = mid1X + (int)(25 * zoom);
            int bot3X = mid2X + (int)(25 * zoom);
            int botW = Math.Max(50, (int)(68 * zoom));
            string t3 = (nodes.Length > 3) ? nodes[3] : "Nhân sự";
            string t4 = (nodes.Length > 4) ? nodes[4] : "Kỹ thuật";
            string t5 = (nodes.Length > 5) ? nodes[5] : "Kinh doanh";
            DrawGradientCard(g, new Rectangle(bot1X, botY, botW, nodeH),
                Color.FromArgb(166, 166, 166), Color.FromArgb(127, 127, 127), Color.FromArgb(89, 89, 89), t3, zoom);
            DrawGradientCard(g, new Rectangle(bot2X, botY, botW, nodeH),
                Color.FromArgb(166, 166, 166), Color.FromArgb(127, 127, 127), Color.FromArgb(89, 89, 89), t4, zoom);
            DrawGradientCard(g, new Rectangle(bot3X, botY, botW, nodeH),
                Color.FromArgb(166, 166, 166), Color.FromArgb(127, 127, 127), Color.FromArgb(89, 89, 89), t5, zoom);
        }

        private static void DrawGradientCard(Graphics g, Rectangle r, Color cTop, Color cBot, Color cBorder, string text, float zoom)
        {
            using (GraphicsPath path = GetRoundedPath(r, (int)(4 * zoom)))
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(r, cTop, cBot, LinearGradientMode.Vertical))
                {
                    g.FillPath(lgb, path);
                }
                using (Pen p = new Pen(cBorder, 1.0f))
                {
                    g.DrawPath(p, path);
                }
            }
            float fSize = Math.Max(8f, Math.Min(13f, r.Height * 0.35f));
            using (Font f = new Font("Segoe UI", fSize, FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
            {
                g.DrawString(text, f, Brushes.White, r, sf);
            }
        }

        private static GraphicsPath GetRoundedPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ==================== 5. LIST BLOCKS ====================
        private static void DrawListBlocks(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count <= 0) return;

            int gap = Math.Max(8, (int)(14 * zoom));
            int blockH = (r.Height - gap * (count - 1)) / count;

            int curY = r.Y;
            for (int i = 0; i < count; i++)
            {
                Rectangle bRect = new Rectangle(r.X, curY, r.Width, blockH);
                Color c = colors[i % colors.Length];

                int badgeW = Math.Max(36, (int)(60 * zoom));
                Rectangle badgeR = new Rectangle(bRect.X, bRect.Y, badgeW, bRect.Height);
                using (SolidBrush bb = new SolidBrush(ControlPaint.Dark(c, 0.1f)))
                {
                    g.FillRectangle(bb, badgeR);
                }
                float bfSize = Math.Max(12f, Math.Min(24f, blockH * 0.35f));
                using (Font bf = new Font("Segoe UI", bfSize, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString((i + 1).ToString("D2"), bf, Brushes.White, badgeR, sf);
                }

                Rectangle textR = new Rectangle(bRect.X + badgeW, bRect.Y, bRect.Width - badgeW, bRect.Height);
                using (SolidBrush tb = new SolidBrush(c))
                {
                    g.FillRectangle(tb, textR);
                }
                float tfSize = Math.Max(12f, Math.Min(22f, blockH * 0.30f));
                using (Font tf = new Font("Segoe UI", tfSize, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    Rectangle innerText = new Rectangle(textR.X + (int)(16 * zoom), textR.Y, textR.Width - (int)(24 * zoom), textR.Height);
                    g.DrawString(nodes[i], tf, Brushes.White, innerText, sf);
                }

                curY += blockH + gap;
            }
        }

        // ==================== 6. MATRIX GRID ====================
        private static void DrawMatrixGrid(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int gap = Math.Max(8, (int)(16 * zoom));
            int cellW = (r.Width - gap) / 2;
            int cellH = (r.Height - gap) / 2;

            Rectangle[] cellRects = new Rectangle[]
            {
                new Rectangle(r.X, r.Y, cellW, cellH),                         // TL
                new Rectangle(r.X + cellW + gap, r.Y, cellW, cellH),           // TR
                new Rectangle(r.X, r.Y + cellH + gap, cellW, cellH),           // BL
                new Rectangle(r.X + cellW + gap, r.Y + cellH + gap, cellW, cellH)// BR
            };

            for (int i = 0; i < 4 && i < cellRects.Length; i++)
            {
                Rectangle cR = cellRects[i];
                Color c = colors[i % colors.Length];
                using (SolidBrush cb = new SolidBrush(c))
                {
                    g.FillRectangle(cb, cR);
                }

                string nodeText = (i < nodes.Length) ? nodes[i] : "Mục " + (i + 1);
                float tfSize = Math.Max(12f, Math.Min(24f, cellH * 0.18f));
                using (Font tf = new Font("Segoe UI", tfSize, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString(nodeText, tf, Brushes.White, cR, sf);
                }
            }
        }

        // ==================== 7. PYRAMID LAYERS ====================
        private static void DrawPyramidLayers(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count <= 0) return;

            int gap = Math.Max(4, (int)(6 * zoom));
            int layerH = (r.Height - gap * (count - 1)) / count;

            int centerX = r.X + r.Width / 2;
            int halfBase = r.Width / 2;

            for (int i = 0; i < count; i++)
            {
                float topRatio = (float)i / count;
                float botRatio = (float)(i + 1) / count;

                int topW = (int)(halfBase * topRatio * 2f);
                int botW = (int)(halfBase * botRatio * 2f);

                int layerY = r.Y + i * (layerH + gap);

                PointF[] pts = new PointF[]
                {
                    new PointF(centerX - topW / 2f, layerY),
                    new PointF(centerX + topW / 2f, layerY),
                    new PointF(centerX + botW / 2f, layerY + layerH),
                    new PointF(centerX - botW / 2f, layerY + layerH)
                };

                using (GraphicsPath pp = new GraphicsPath())
                {
                    pp.AddPolygon(pts);
                    Color c = colors[i % colors.Length];
                    using (SolidBrush pb = new SolidBrush(c))
                    {
                        g.FillPath(pb, pp);
                    }
                    using (Pen pPen = new Pen(Color.White, 2.5f * zoom))
                    {
                        g.DrawPath(pPen, pp);
                    }
                }

                Rectangle tBox = new Rectangle(centerX - botW / 2 + (int)(12 * zoom), layerY, Math.Max(20, botW - (int)(24 * zoom)), layerH);
                float pfSize = Math.Max(12f, Math.Min(22f, layerH * 0.32f));
                using (Font pf = new Font("Segoe UI", pfSize, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString(nodes[i], pf, Brushes.White, tBox, sf);
                }
            }
        }

        // ==================== 8. PICTURE BLOCKS ====================
        private static void DrawPictureBlocks(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count <= 0) return;

            int gap = Math.Max(8, (int)(16 * zoom));
            int cardW = (r.Width - gap * (count - 1)) / count;
            int cardH = Math.Min(r.Height - 12, (int)(r.Height * 0.85f));
            int cardY = r.Y + (r.Height - cardH) / 2;

            for (int i = 0; i < count; i++)
            {
                int cardX = r.X + i * (cardW + gap);
                Rectangle cardRect = new Rectangle(cardX, cardY, cardW, cardH);

                int picH = (int)(cardH * 0.65f);
                Rectangle picRect = new Rectangle(cardX, cardY, cardW, picH);
                using (SolidBrush pb = new SolidBrush(Color.FromArgb(240, 242, 245)))
                {
                    g.FillRectangle(pb, picRect);
                }
                using (Pen pp = new Pen(Color.FromArgb(200, 205, 215), 1.0f))
                {
                    g.DrawRectangle(pp, picRect);
                }

                // Photo placeholder icon (mountain + sun)
                int iconW = Math.Min(picRect.Width - 10, (int)(32 * zoom));
                int iconH = Math.Min(picRect.Height - 10, (int)(24 * zoom));
                int ix = picRect.X + (picRect.Width - iconW) / 2;
                int iy = picRect.Y + (picRect.Height - iconH) / 2;
                using (Pen ip = new Pen(Color.FromArgb(140, 145, 155), 1.5f * zoom))
                {
                    g.DrawRectangle(ip, ix, iy, iconW, iconH);
                    g.DrawEllipse(ip, ix + iconW - (int)(8 * zoom), iy + (int)(3 * zoom), (int)(4 * zoom), (int)(4 * zoom));
                    PointF[] mtn = new PointF[] {
                        new PointF(ix + (int)(2 * zoom), iy + iconH - (int)(2 * zoom)),
                        new PointF(ix + iconW * 0.4f, iy + (int)(6 * zoom)),
                        new PointF(ix + iconW * 0.7f, iy + iconH * 0.55f),
                        new PointF(ix + iconW * 0.85f, iy + (int)(10 * zoom)),
                        new PointF(ix + iconW - (int)(2 * zoom), iy + iconH - (int)(2 * zoom))
                    };
                    g.DrawLines(ip, mtn);
                }

                // Caption Box
                Rectangle capRect = new Rectangle(cardX, cardY + picH, cardW, cardH - picH);
                Color c = colors[i % colors.Length];
                using (SolidBrush cb = new SolidBrush(c))
                {
                    g.FillRectangle(cb, capRect);
                }

                float fSize = Math.Max(9f, Math.Min(16f, capRect.Height * 0.32f));
                using (Font f = new Font("Segoe UI", fSize, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString(nodes[i], f, Brushes.White, capRect, sf);
                }
            }
        }

        // ==================== 9. PROCESS BENDING ====================
        private static void DrawProcessBending(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;
            int cardW = Math.Max(30, (r.Width - (count * 20)) / count);
            int cardH = Math.Min(r.Height / 2, 60);
            using (Pen pathPen = new Pen(Color.FromArgb(180, 180, 180), 4f * zoom))
            {
                g.DrawBezier(pathPen, r.X, r.Y + cardH, r.X + r.Width / 2, r.Y - cardH, r.X + r.Width / 2, r.Bottom + cardH, r.Right, r.Bottom - cardH);
            }
            for (int i = 0; i < count; i++)
            {
                int cx = r.X + i * (cardW + 20);
                int cy = r.Y + (i % 2 == 0 ? 0 : r.Height - cardH);
                Rectangle card = new Rectangle(cx, cy, cardW, cardH);
                using (SolidBrush b = new SolidBrush(colors[i % colors.Length])) g.FillRectangle(b, card);
                DrawAutoFitText(g, nodes[i], card, Color.White);
            }
        }

        // ==================== 10. TIMELINE HORIZONTAL ====================
        private static void DrawTimelineHorizontal(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            using (Pen linePen = new Pen(Color.Gray, 3f * zoom))
                g.DrawLine(linePen, r.X, r.Y + r.Height / 2, r.Right, r.Y + r.Height / 2);
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;
            int w = r.Width / count;
            for (int i = 0; i < count; i++)
            {
                int cx = r.X + i * w + w / 2;
                using (SolidBrush b = new SolidBrush(colors[i % colors.Length]))
                    g.FillEllipse(b, cx - 10, r.Y + r.Height / 2 - 10, 20, 20);
                Rectangle tr = new Rectangle(cx - w / 2, r.Y + (i % 2 == 0 ? 0 : r.Height / 2 + 15), w, r.Height / 2 - 15);
                DrawAutoFitText(g, nodes[i], tr, Color.Black);
            }
        }

        // ==================== 11. VENN DIAGRAM ====================
        private static void DrawVennDiagram(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = Math.Min(nodes.Length, 3);
            if (count == 0) return;
            int d = Math.Min(r.Width, r.Height) * 2 / 3;
            Point[] centers = { new Point(r.X + r.Width / 2, r.Y + d / 2), new Point(r.X + r.Width / 2 - d / 4, r.Y + r.Height - d / 2), new Point(r.X + r.Width / 2 + d / 4, r.Y + r.Height - d / 2) };
            for (int i = 0; i < count; i++)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(180, colors[i % colors.Length])))
                    g.FillEllipse(b, centers[i].X - d / 2, centers[i].Y - d / 2, d, d);
                DrawAutoFitText(g, nodes[i], new Rectangle(centers[i].X - d / 3, centers[i].Y - d / 3, d * 2 / 3, d * 2 / 3), Color.White);
            }
        }

        // ==================== 12. GEAR INTERLOCK (AUTHENTIC VECTOR GEARS) ====================
        private static void DrawSingleGear(Graphics g, float cx, float cy, float outerR, float innerR, int teeth, Color cTop, Color cBot, Color cBorder)
        {
            int totalPoints = teeth * 4;
            PointF[] pts = new PointF[totalPoints];
            double step = Math.PI * 2.0 / totalPoints;

            for (int i = 0; i < totalPoints; i++)
            {
                double ang = i * step;
                float curR = (i % 4 == 1 || i % 4 == 2) ? outerR : innerR;
                pts[i] = new PointF((float)(cx + curR * Math.Cos(ang)), (float)(cy + curR * Math.Sin(ang)));
            }

            RectangleF gearBounds = new RectangleF(cx - outerR, cy - outerR, outerR * 2, outerR * 2);
            using (LinearGradientBrush lgb = new LinearGradientBrush(gearBounds, cTop, cBot, LinearGradientMode.ForwardDiagonal))
            {
                g.FillPolygon(lgb, pts);
            }
            using (Pen p = new Pen(cBorder, 1.5f))
            {
                g.DrawPolygon(p, pts);
            }

            // Axle hole with metallic cap
            float holeR = innerR * 0.45f;
            RectangleF holeBounds = new RectangleF(cx - holeR, cy - holeR, holeR * 2, holeR * 2);
            using (LinearGradientBrush lgb = new LinearGradientBrush(holeBounds, Color.FromArgb(245, 247, 250), Color.FromArgb(180, 185, 195), LinearGradientMode.Vertical))
            {
                g.FillEllipse(lgb, holeBounds);
            }
            using (Pen p = new Pen(cBorder, 1.2f))
            {
                g.DrawEllipse(p, holeBounds);
            }
        }

        private static void DrawGearInterlock(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;

            if (count == 3)
            {
                // Standard 3-gear triangular layout matching MS Office
                float cx1 = r.X + r.Width * 0.28f;
                float cy1 = r.Y + r.Height * 0.60f;
                float r1 = Math.Min(r.Width * 0.22f, r.Height * 0.35f);

                float cx2 = r.X + r.Width * 0.52f;
                float cy2 = r.Y + r.Height * 0.35f;
                float r2 = Math.Min(r.Width * 0.18f, r.Height * 0.29f);

                float cx3 = r.X + r.Width * 0.74f;
                float cy3 = r.Y + r.Height * 0.62f;
                float r3 = Math.Min(r.Width * 0.16f, r.Height * 0.26f);

                Color c1 = colors[0 % colors.Length];
                Color c2 = colors[1 % colors.Length];
                Color c3 = colors[2 % colors.Length];

                DrawSingleGear(g, cx1, cy1, r1, r1 * 0.74f, 12, c1, ControlPaint.Dark(c1, 0.2f), ControlPaint.Dark(c1, 0.4f));
                DrawSingleGear(g, cx2, cy2, r2, r2 * 0.74f, 10, c2, ControlPaint.Dark(c2, 0.2f), ControlPaint.Dark(c2, 0.4f));
                DrawSingleGear(g, cx3, cy3, r3, r3 * 0.74f, 8, c3, ControlPaint.Dark(c3, 0.2f), ControlPaint.Dark(c3, 0.4f));

                // Draw text labels
                RectangleF t1 = new RectangleF(cx1 - r1 * 0.55f, cy1 - r1 * 0.55f, r1 * 1.1f, r1 * 1.1f);
                RectangleF t2 = new RectangleF(cx2 - r2 * 0.55f, cy2 - r2 * 0.55f, r2 * 1.1f, r2 * 1.1f);
                RectangleF t3 = new RectangleF(cx3 - r3 * 0.55f, cy3 - r3 * 0.55f, r3 * 1.1f, r3 * 1.1f);

                DrawAutoFitText(g, nodes[0], Rectangle.Round(t1), Color.White);
                DrawAutoFitText(g, nodes[1], Rectangle.Round(t2), Color.White);
                DrawAutoFitText(g, nodes[2], Rectangle.Round(t3), Color.White);
            }
            else
            {
                // Dynamic gear train for N gears
                float stepX = (float)r.Width / count;
                for (int i = 0; i < count; i++)
                {
                    float cx = r.X + (i + 0.5f) * stepX;
                    float cy = r.Y + r.Height * (i % 2 == 0 ? 0.62f : 0.38f);
                    float outR = Math.Min(stepX * 0.55f, r.Height * 0.32f);
                    float inR = outR * 0.75f;
                    int teeth = Math.Max(7, Math.Min(14, (int)(outR * 0.25f)));

                    Color c = colors[i % colors.Length];
                    DrawSingleGear(g, cx, cy, outR, inR, teeth, c, ControlPaint.Dark(c, 0.2f), ControlPaint.Dark(c, 0.4f));

                    RectangleF tr = new RectangleF(cx - inR * 0.7f, cy - inR * 0.7f, inR * 1.4f, inR * 1.4f);
                    DrawAutoFitText(g, nodes[i], Rectangle.Round(tr), Color.White);
                }
            }
        }

        // ==================== 13. FUNNEL SHAPE (3D TAPERED FUNNEL WITH SPHERES) ====================
        private static void DrawFunnelShape(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;

            int cx = r.X + r.Width / 2;
            int topW = (int)(r.Width * 0.70f);
            int botW = (int)(r.Width * 0.18f);
            int topY = r.Y + (int)(r.Height * 0.22f);
            int botY = r.Bottom - (int)(r.Height * 0.16f);

            // 1. Top Opening Ellipse
            int topEllH = Math.Max(18, (int)(28 * zoom));
            Rectangle topEllRect = new Rectangle(cx - topW / 2, topY - topEllH / 2, topW, topEllH);
            using (LinearGradientBrush lgb = new LinearGradientBrush(topEllRect, Color.FromArgb(41, 128, 185), Color.FromArgb(21, 67, 96), LinearGradientMode.Horizontal))
            {
                g.FillEllipse(lgb, topEllRect);
            }
            using (Pen p = new Pen(Color.FromArgb(21, 67, 96), 1.5f))
            {
                g.DrawEllipse(p, topEllRect);
            }

            // 2. Tapered Conical Sections (One per step/node)
            int totalH = botY - topY;
            int bandH = totalH / count;

            for (int i = 0; i < count; i++)
            {
                float t1 = (float)i / count;
                float t2 = (float)(i + 1) / count;

                int y1 = topY + (int)(t1 * totalH);
                int y2 = topY + (int)(t2 * totalH);

                int w1 = (int)(topW - t1 * (topW - botW));
                int w2 = (int)(topW - t2 * (topW - botW));

                PointF[] slice = new PointF[]
                {
                    new PointF(cx - w1 / 2f, y1),
                    new PointF(cx + w1 / 2f, y1),
                    new PointF(cx + w2 / 2f, y2),
                    new PointF(cx - w2 / 2f, y2)
                };

                Color c = colors[i % colors.Length];
                Color cDark = ControlPaint.Dark(c, 0.25f);
                RectangleF sliceRect = new RectangleF(cx - w1 / 2f, y1, w1, y2 - y1);
                using (LinearGradientBrush lgb = new LinearGradientBrush(sliceRect, c, cDark, LinearGradientMode.Vertical))
                {
                    g.FillPolygon(lgb, slice);
                }
                using (Pen p = new Pen(Color.White, 1.2f))
                {
                    g.DrawPolygon(p, slice);
                }

                // Text label inside this funnel section
                Rectangle labelRect = new Rectangle(cx - w2 / 2 + 8, y1 + 2, Math.Max(10, w2 - 16), Math.Max(10, y2 - y1 - 4));
                DrawAutoFitText(g, nodes[i], labelRect, Color.White);
            }

            // 3. Floating colored spheres entering the funnel top
            int sphereR = Math.Max(8, (int)(12 * zoom));
            int[] sphereOffsetsX = new int[] { -topW / 3, -topW / 10, topW / 6, topW / 3 };
            int[] sphereOffsetsY = new int[] { -sphereR - 8, -sphereR * 2, -sphereR - 12, -sphereR - 4 };
            Color[] sphereCols = new Color[] { Color.FromArgb(237, 125, 49), Color.FromArgb(91, 155, 213), Color.FromArgb(112, 173, 71), Color.FromArgb(255, 192, 0) };

            for (int s = 0; s < 4; s++)
            {
                int scx = cx + sphereOffsetsX[s];
                int scy = topY + sphereOffsetsY[s];
                Rectangle sRect = new Rectangle(scx - sphereR, scy - sphereR, sphereR * 2, sphereR * 2);
                using (GraphicsPath sp = new GraphicsPath())
                {
                    sp.AddEllipse(sRect);
                    using (PathGradientBrush pgb = new PathGradientBrush(sp))
                    {
                        pgb.CenterPoint = new PointF(scx - sphereR * 0.3f, scy - sphereR * 0.3f);
                        pgb.CenterColor = Color.White;
                        pgb.SurroundColors = new Color[] { sphereCols[s] };
                        g.FillEllipse(pgb, sRect);
                    }
                }
                using (Pen p = new Pen(Color.FromArgb(120, 0, 0, 0), 1f))
                {
                    g.DrawEllipse(p, sRect);
                }
            }

            // 4. Output Droplet at the nozzle bottom
            int dropR = Math.Max(6, (int)(9 * zoom));
            int dropY = botY + dropR + 2;
            Rectangle dropRect = new Rectangle(cx - dropR, dropY - dropR, dropR * 2, dropR * 2);
            using (GraphicsPath dp = new GraphicsPath())
            {
                dp.AddEllipse(dropRect);
                using (PathGradientBrush pgb = new PathGradientBrush(dp))
                {
                    pgb.CenterPoint = new PointF(cx - dropR * 0.3f, dropY - dropR * 0.3f);
                    pgb.CenterColor = Color.White;
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 176, 240) };
                    g.FillEllipse(pgb, dropRect);
                }
            }
        }

        // ==================== 14. BALANCE SCALE (AUTHENTIC 3D TILTED SCALE) ====================
        private static void DrawBalanceScale(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            int cx = r.X + r.Width / 2;
            int cy = r.Y + Math.Max(26, (int)(32 * zoom));

            // 1. Triangular Fulcrum Base
            int baseW = Math.Min((int)(110 * zoom), (int)(r.Width * 0.26f));
            int baseH = Math.Min((int)(46 * zoom), (int)(r.Height * 0.20f));
            PointF[] baseTri = new PointF[] {
                new PointF(cx, r.Bottom - baseH - 6),
                new PointF(cx - baseW / 2f, r.Bottom - 6),
                new PointF(cx + baseW / 2f, r.Bottom - 6)
            };
            Rectangle baseRect = new Rectangle(cx - baseW / 2, r.Bottom - baseH - 6, baseW, baseH);
            using (LinearGradientBrush lgb = new LinearGradientBrush(baseRect, Color.FromArgb(150, 155, 165), Color.FromArgb(70, 75, 85), LinearGradientMode.Vertical))
            {
                g.FillPolygon(lgb, baseTri);
            }
            using (Pen p = new Pen(Color.FromArgb(50, 55, 65), 1.8f))
            {
                g.DrawPolygon(p, baseTri);
            }

            // 2. Center Vertical Pillar
            using (Pen stemPen = new Pen(Color.FromArgb(110, 115, 125), Math.Max(3.5f, 4.5f * zoom)))
            {
                g.DrawLine(stemPen, cx, cy, cx, r.Bottom - baseH - 6);
            }

            // 3. Golden Pivot Cap
            int pivotR = Math.Max(6, (int)(8 * zoom));
            Rectangle pivotRect = new Rectangle(cx - pivotR, cy - pivotR, pivotR * 2, pivotR * 2);
            g.FillEllipse(Brushes.Gold, pivotRect);
            using (Pen p = new Pen(Color.DarkGoldenrod, 1.5f))
            {
                g.DrawEllipse(p, pivotRect);
            }

            // 4. Tilted Balance Beam (~8 degrees tilt)
            int beamHalfW = (int)(r.Width * 0.32f);
            int tiltY = Math.Max(10, (int)(beamHalfW * 0.10f));
            int leftX = cx - beamHalfW;
            int leftY = cy - tiltY;
            int rightX = cx + beamHalfW;
            int rightY = cy + tiltY;

            using (Pen beamPen = new Pen(Color.FromArgb(70, 75, 85), Math.Max(3f, 4f * zoom)))
            {
                g.DrawLine(beamPen, leftX, leftY, rightX, rightY);
            }

            // 5. Left Pan (Higher) & Right Pan (Lower)
            int panW = Math.Min((int)(115 * zoom), (int)(r.Width * 0.28f));
            int panH = Math.Max(8, (int)(10 * zoom));
            int basePanDrop = Math.Max(60, (int)(r.Height * 0.48f));
            int leftPanY = Math.Min(r.Bottom - panH - 12 - 2 * tiltY, leftY + basePanDrop);
            int rightPanY = leftPanY + 2 * tiltY;

            // Suspension Cords: Inverted V from beam tips to pan edges
            using (Pen cordPen = new Pen(Color.FromArgb(140, 145, 155), 1.5f))
            {
                // Left cords
                g.DrawLine(cordPen, leftX, leftY, leftX - panW * 0.42f, leftPanY);
                g.DrawLine(cordPen, leftX, leftY, leftX + panW * 0.42f, leftPanY);

                // Right cords
                g.DrawLine(cordPen, rightX, rightY, rightX - panW * 0.42f, rightPanY);
                g.DrawLine(cordPen, rightX, rightY, rightX + panW * 0.42f, rightPanY);
            }

            // Beam tip hooks
            g.FillEllipse(Brushes.Gold, leftX - 3, leftY - 3, 6, 6);
            g.FillEllipse(Brushes.Gold, rightX - 3, rightY - 3, 6, 6);

            // Pans
            Rectangle leftPanRect = new Rectangle((int)(leftX - panW / 2f), leftPanY, panW, panH);
            Rectangle rightPanRect = new Rectangle((int)(rightX - panW / 2f), rightPanY, panW, panH);
            using (SolidBrush pb = new SolidBrush(Color.FromArgb(127, 132, 140)))
            {
                g.FillRectangle(pb, leftPanRect);
                g.FillRectangle(pb, rightPanRect);
            }
            using (Pen pp = new Pen(Color.FromArgb(80, 85, 95), 1.2f))
            {
                g.DrawRectangle(pp, leftPanRect);
                g.DrawRectangle(pp, rightPanRect);
            }

            // 6. Weights with Text Blocks sitting on the pans
            int leftCount = Math.Max(1, (count + 1) / 2);
            int rightCount = Math.Max(1, count - leftCount);
            int maxWeightH = Math.Min((int)(r.Height * 0.34f), Math.Max(30, leftPanY - leftY - 14));
            int boxW = (int)(panW * 0.88f);

            // Render Left Pan Weights
            int leftBoxH = (maxWeightH - (leftCount - 1) * 3) / leftCount;
            for (int k = 0; k < leftCount; k++)
            {
                Rectangle boxRect = new Rectangle(leftX - boxW / 2, leftPanY - (k + 1) * leftBoxH - k * 3, boxW, leftBoxH);
                Color c = colors[k % colors.Length];
                Color cDark = ControlPaint.Dark(c, 0.2f);
                using (LinearGradientBrush lgb = new LinearGradientBrush(boxRect, c, cDark, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(lgb, boxRect);
                }
                using (Pen bp = new Pen(ControlPaint.Dark(c, 0.4f), 1.2f))
                {
                    g.DrawRectangle(bp, boxRect);
                }
                string t = (k < count && !string.IsNullOrEmpty(nodes[k])) ? nodes[k] : (k == 0 ? "Ưu điểm / Ý kiến 1" : "Ý kiến " + (k + 1));
                DrawAutoFitText(g, t, boxRect, Color.White);
            }

            // Render Right Pan Weights
            int rightBoxH = (maxWeightH - (rightCount - 1) * 3) / rightCount;
            for (int k = 0; k < rightCount; k++)
            {
                int nodeIdx = leftCount + k;
                Rectangle boxRect = new Rectangle(rightX - boxW / 2, rightPanY - (k + 1) * rightBoxH - k * 3, boxW, rightBoxH);
                Color c = colors[(leftCount + k) % colors.Length];
                Color cDark = ControlPaint.Dark(c, 0.2f);
                using (LinearGradientBrush lgb = new LinearGradientBrush(boxRect, c, cDark, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(lgb, boxRect);
                }
                using (Pen bp = new Pen(ControlPaint.Dark(c, 0.4f), 1.2f))
                {
                    g.DrawRectangle(bp, boxRect);
                }
                string t = (nodeIdx < count && !string.IsNullOrEmpty(nodes[nodeIdx])) ? nodes[nodeIdx] : (k == 0 ? "Nhược điểm / Ý kiến 2" : "Ý kiến " + (nodeIdx + 1));
                DrawAutoFitText(g, t, boxRect, Color.White);
            }
        }

        // ==================== 15. TARGET RINGS (CONCENTRIC TARGET WITH LABELS) ====================
        private static void DrawTargetRings(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;

            int targetD = Math.Min((int)(r.Width * 0.44f), r.Height - 16);
            int cx = r.X + targetD / 2 + 10;
            int cy = r.Y + r.Height / 2;

            // Draw concentric rings from outside to inside
            for (int i = 0; i < count; i++)
            {
                int rad = (targetD / 2) * (count - i) / count;
                Rectangle ringRect = new Rectangle(cx - rad, cy - rad, rad * 2, rad * 2);
                Color c = colors[i % colors.Length];
                Color cDark = ControlPaint.Dark(c, 0.2f);
                using (LinearGradientBrush lgb = new LinearGradientBrush(ringRect, c, cDark, LinearGradientMode.ForwardDiagonal))
                {
                    g.FillEllipse(lgb, ringRect);
                }
                using (Pen p = new Pen(Color.White, 1.5f))
                {
                    g.DrawEllipse(p, ringRect);
                }
            }

            // Right side cards for all node labels with leader lines
            int cardX = cx + targetD / 2 + Math.Max(16, (int)(24 * zoom));
            int cardW = Math.Max(60, r.Right - cardX - 10);
            int cardH = Math.Max(24, (r.Height - 16 - (count - 1) * 6) / count);

            for (int i = 0; i < count; i++)
            {
                int cardY = r.Y + 8 + i * (cardH + 6);
                Rectangle cardRect = new Rectangle(cardX, cardY, cardW, cardH);
                Color c = colors[i % colors.Length];

                // Leader line from ring edge to card
                int ringRad = (targetD / 2) * (count - i) / count;
                using (Pen p = new Pen(c, 1.5f))
                {
                    p.DashStyle = DashStyle.Dot;
                    g.DrawLine(p, cx + ringRad, cardY + cardH / 2, cardX, cardY + cardH / 2);
                }

                // Card background
                using (SolidBrush b = new SolidBrush(c))
                {
                    g.FillRectangle(b, cardRect);
                }
                using (Pen p = new Pen(ControlPaint.Dark(c, 0.2f), 1.2f))
                {
                    g.DrawRectangle(p, cardRect);
                }

                DrawAutoFitText(g, nodes[i], cardRect, Color.White);
            }
        }

        // ==================== 16. EQUATION BLOCKS ====================
        private static void DrawEquationBlocks(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;
            int cardW = Math.Max(20, (r.Width - (count * 20)) / count);
            for (int i = 0; i < count; i++)
            {
                Rectangle cr = new Rectangle(r.X + i * (cardW + 20), r.Y + r.Height / 4, cardW, r.Height / 2);
                using (SolidBrush b = new SolidBrush(colors[i % colors.Length])) g.FillRectangle(b, cr);
                DrawAutoFitText(g, nodes[i], cr, Color.White);
                if (i < count - 2)
                    g.DrawString("+", new Font("Segoe UI", 14f, FontStyle.Bold), Brushes.Black, cr.Right + 2, cr.Y + cr.Height / 2 - 12);
                else if (i == count - 2)
                    g.DrawString("=", new Font("Segoe UI", 14f, FontStyle.Bold), Brushes.Black, cr.Right + 2, cr.Y + cr.Height / 2 - 12);
            }
        }

        // ==================== 17. ARROWS CONVERGING ====================
        private static void DrawArrowsConverging(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;
            int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2;
            int rad = Math.Min(r.Width, r.Height) / 3;
            for (int i = 0; i < count; i++)
            {
                double angle = i * 2 * Math.PI / count;
                int px = cx + (int)(rad * Math.Cos(angle));
                int py = cy + (int)(rad * Math.Sin(angle));
                using (Pen p = new Pen(colors[i % colors.Length], 10f * zoom))
                {
                    p.EndCap = LineCap.ArrowAnchor;
                    g.DrawLine(p, px, py, cx, cy);
                }
                Rectangle tr = new Rectangle(px - 30, py - 15, 60, 30);
                DrawAutoFitText(g, nodes[i], tr, Color.Black);
            }
        }

        // ==================== 18. ARROWS OPPOSING ====================
        private static void DrawArrowsOpposing(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            if (nodes.Length < 2) { DrawListBlocks(g, r, nodes, colors, zoom); return; }
            Rectangle r1 = new Rectangle(r.X, r.Y + r.Height / 4, r.Width / 2 - 10, r.Height / 2);
            Rectangle r2 = new Rectangle(r.X + r.Width / 2 + 10, r.Y + r.Height / 4, r.Width / 2 - 10, r.Height / 2);
            using (SolidBrush b1 = new SolidBrush(colors[0])) g.FillRectangle(b1, r1);
            using (SolidBrush b2 = new SolidBrush(colors[1 % colors.Length])) g.FillRectangle(b2, r2);
            DrawAutoFitText(g, nodes[0], r1, Color.White);
            DrawAutoFitText(g, nodes[1], r2, Color.White);
        }

        // ==================== 19. HEXAGON CLUSTER ====================
        private static void DrawHexagonCluster(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;
            int hw = r.Width / 3, hh = r.Height / 3;
            for (int i = 0; i < count; i++)
            {
                int cx = r.X + hw / 2 + (i % 3) * hw * 3 / 4;
                int cy = r.Y + hh / 2 + (i / 3) * hh + (i % 2 == 0 ? 0 : hh / 2);
                Point[] pts = {
                    new Point(cx - hw/4, cy - hh/2), new Point(cx + hw/4, cy - hh/2),
                    new Point(cx + hw/2, cy), new Point(cx + hw/4, cy + hh/2),
                    new Point(cx - hw/4, cy + hh/2), new Point(cx - hw/2, cy)
                };
                using (SolidBrush b = new SolidBrush(colors[i % colors.Length])) g.FillPolygon(b, pts);
                DrawAutoFitText(g, nodes[i], new Rectangle(cx - hw / 4, cy - hh / 4, hw / 2, hh / 2), Color.White);
            }
        }

        // ==================== 20. TAB LAYOUT ====================
        private static void DrawTabLayout(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;
            int tabW = r.Width / count;
            for (int i = 0; i < count; i++)
            {
                Rectangle tab = new Rectangle(r.X + i * tabW, r.Y, tabW - 2, 30);
                Rectangle body = new Rectangle(r.X + i * tabW, r.Y + 30, tabW - 2, r.Height - 30);
                using (SolidBrush tb = new SolidBrush(ControlPaint.Dark(colors[i % colors.Length]))) g.FillRectangle(tb, tab);
                using (SolidBrush bb = new SolidBrush(colors[i % colors.Length])) g.FillRectangle(bb, body);
                DrawAutoFitText(g, "Tab " + (i + 1).ToString(), tab, Color.White);
                DrawAutoFitText(g, nodes[i], body, Color.White);
            }
        }

        // ==================== 21. BRACKET LAYOUT ====================
        private static void DrawBracketLayout(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            using (Pen p = new Pen(Color.Gray, 3f * zoom))
            {
                g.DrawLine(p, r.X + 20, r.Y + 10, r.X + 20, r.Bottom - 10);
                g.DrawLine(p, r.X + 20, r.Y + 10, r.X + 30, r.Y + 10);
                g.DrawLine(p, r.X + 20, r.Bottom - 10, r.X + 30, r.Bottom - 10);
            }
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;
            int h = (r.Height - 20) / count;
            for (int i = 0; i < count; i++)
            {
                Rectangle br = new Rectangle(r.X + 40, r.Y + 10 + i * h, r.Width - 40, h - 5);
                using (SolidBrush b = new SolidBrush(colors[i % colors.Length])) g.FillRectangle(b, br);
                DrawAutoFitText(g, nodes[i], br, Color.White);
            }
        }

        // ==================== 22. PICTURE GRID ====================
        private static void DrawPictureGrid(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            DrawMatrixGrid(g, r, nodes, colors, zoom); // Fallback to matrix for simplicity
        }

        // ==================== 23. PICTURE STRIPS ====================
        private static void DrawPictureStrips(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = Math.Min(nodes.Length, 6);
            if (count <= 0) return;
            int gap = Math.Max(6, (int)(12 * zoom));
            int colW = (r.Width - gap * (count - 1)) / count;

            for (int i = 0; i < count; i++)
            {
                int x = r.X + i * (colW + gap);
                int picH = (int)(r.Height * 0.68f);
                int txtH = r.Height - picH - (int)(4 * zoom);

                Rectangle picRect = new Rectangle(x, r.Y, colW, picH);
                Rectangle txtRect = new Rectangle(x, r.Y + picH + (int)(4 * zoom), colW, txtH);

                // Photo placeholder
                using (SolidBrush pb = new SolidBrush(Color.FromArgb(240, 242, 245)))
                    g.FillRectangle(pb, picRect);
                using (Pen pp = new Pen(Color.FromArgb(200, 205, 215), 1.0f))
                    g.DrawRectangle(pp, picRect);

                int iconW = Math.Min(picRect.Width - 6, (int)(28 * zoom));
                int iconH = Math.Min(picRect.Height - 6, (int)(20 * zoom));
                int ix = picRect.X + (picRect.Width - iconW) / 2;
                int iy = picRect.Y + (picRect.Height - iconH) / 2;
                using (Pen ip = new Pen(Color.FromArgb(140, 145, 155), 1.2f * zoom))
                {
                    g.DrawRectangle(ip, ix, iy, iconW, iconH);
                    g.DrawEllipse(ip, ix + iconW - (int)(6 * zoom), iy + (int)(2 * zoom), (int)(3 * zoom), (int)(3 * zoom));
                }

                // Colored text bar
                Color col = colors[i % colors.Length];
                using (LinearGradientBrush lgb = new LinearGradientBrush(txtRect, col, ControlPaint.Dark(col, 0.15f), LinearGradientMode.Vertical))
                using (GraphicsPath path = CreateRoundedPath(txtRect, (int)(4 * zoom)))
                {
                    g.FillPath(lgb, path);
                    using (Pen p = new Pen(ControlPaint.Dark(col, 0.3f), 1f))
                        g.DrawPath(p, path);
                }
                DrawAutoFitText(g, nodes[i], txtRect, Color.White);
            }
        }

        // ==================== 24. CYCLE RING (MATCH MICROSOFT WORD) ====================
        private static void DrawCycleRing(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int count = (nodes != null) ? nodes.Length : 0;
            if (count == 0) return;
            int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2;
            int rad = (int)Math.Max(30f, Math.Min(r.Width, r.Height) * 0.32f);

            Color ringCol = (colors != null && colors.Length > 0) ? colors[0] : Color.FromArgb(68, 114, 196);
            using (Pen ringPen = new Pen(ringCol, 1.8f * zoom))
            {
                g.DrawEllipse(ringPen, cx - rad, cy - rad, rad * 2, rad * 2);
            }

            if (!SuppressNodeBodies)
            {
                int cardW = Math.Max(38, (int)(rad * 0.70f));
                int cardH = Math.Max(22, (int)(rad * 0.40f));
                for (int i = 0; i < count; i++)
                {
                    double angle = (i * (360.0 / count) - 90.0) * Math.PI / 180.0;
                    int px = cx + (int)(rad * Math.Cos(angle));
                    int py = cy + (int)(rad * Math.Sin(angle));
                    Rectangle nr = new Rectangle(px - cardW / 2, py - cardH / 2, cardW, cardH);
                    Color c = colors[i % colors.Length];
                    using (SolidBrush b = new SolidBrush(c))
                    using (GraphicsPath path = CreateRoundedPath(nr, Math.Max(2, (int)(6 * zoom))))
                    {
                        g.FillPath(b, path);
                        using (Pen p = new Pen(ControlPaint.Dark(c, 0.15f), 1.2f * zoom))
                            g.DrawPath(p, path);
                    }
                    DrawAutoFitText(g, nodes[i], nr, Color.White);
                }
            }
        }

        // ==================== 25. CYCLE PIE ====================
        private static void DrawCyclePie(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            DrawCycleBasicPie(g, r, nodes, colors, zoom);
        }

        // ==================== CYCLE BASIC PIE ====================
        private static void DrawCycleBasicPie(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = (nodes != null) ? nodes.Length : 0;
            if (count == 0) return;

            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            int rad = (int)Math.Max(30f, Math.Min(r.Width, r.Height) * 0.32f);
            Rectangle pieRect = new Rectangle(cx - rad, cy - rad, rad * 2, rad * 2);

            float sweep = 360f / count;
            for (int i = 0; i < count; i++)
            {
                Color c = colors[i % colors.Length];
                using (SolidBrush b = new SolidBrush(c))
                {
                    g.FillPie(b, pieRect, -90f + i * sweep, sweep);
                }
                using (Pen p = new Pen(Color.White, 1.5f * zoom))
                {
                    g.DrawPie(p, pieRect, -90f + i * sweep, sweep);
                }

                double midAngle = (-90.0 + i * sweep + sweep / 2.0) * Math.PI / 180.0;
                int px = cx + (int)((rad * 0.60f) * Math.Cos(midAngle));
                int py = cy + (int)((rad * 0.60f) * Math.Sin(midAngle));
                int tw = (int)(rad * 0.50f);
                int th = (int)(rad * 0.30f);
                DrawAutoFitText(g, nodes[i], new Rectangle(px - tw / 2, py - th / 2, tw, th), Color.White);
            }
        }

        // ==================== CYCLE SEGMENTED ====================
        private static void DrawCycleSegmented(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = (nodes != null) ? nodes.Length : 0;
            if (count == 0) return;

            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            int outerRad = (int)Math.Max(30f, Math.Min(r.Width, r.Height) * 0.32f);
            int innerRad = (int)(outerRad * 0.50f);

            float sweep = 360f / count;
            float gapDeg = Math.Min(4f, sweep * 0.15f);
            float actualSweep = sweep - gapDeg;

            Rectangle outerRect = new Rectangle(cx - outerRad, cy - outerRad, outerRad * 2, outerRad * 2);
            Rectangle innerRect = new Rectangle(cx - innerRad, cy - innerRad, innerRad * 2, innerRad * 2);

            for (int i = 0; i < count; i++)
            {
                float startAng = -90f + i * sweep + gapDeg / 2f;
                Color c = colors[i % colors.Length];

                using (GraphicsPath segPath = new GraphicsPath())
                {
                    segPath.AddArc(outerRect, startAng, actualSweep);
                    segPath.AddArc(innerRect, startAng + actualSweep, -actualSweep);
                    segPath.CloseFigure();

                    using (SolidBrush b = new SolidBrush(c))
                    {
                        g.FillPath(b, segPath);
                    }
                    using (Pen p = new Pen(ControlPaint.Dark(c, 0.15f), 1.2f * zoom))
                    {
                        g.DrawPath(p, segPath);
                    }
                }

                double midAngle = (startAng + actualSweep / 2f) * Math.PI / 180.0;
                float midR = (outerRad + innerRad) / 2f;
                int px = cx + (int)(midR * Math.Cos(midAngle));
                int py = cy + (int)(midR * Math.Sin(midAngle));
                int tw = (int)((outerRad - innerRad) * 1.2f);
                int th = (int)((outerRad - innerRad) * 0.7f);
                DrawAutoFitText(g, nodes[i], new Rectangle(px - tw / 2, py - th / 2, tw, th), Color.White);
            }
        }

        // ==================== CYCLE BLOCK LOOP ====================
        private static void DrawCycleBlockLoop(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int count = (nodes != null) ? nodes.Length : 0;
            if (count == 0) return;

            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            int rad = (int)Math.Max(30f, Math.Min(r.Width, r.Height) * 0.32f);
            int blockThick = Math.Max(12, (int)(rad * 0.32f));
            int outerR = rad + blockThick / 2;
            int innerR = rad - blockThick / 2;

            float sweep = 360f / count;
            float gapDeg = Math.Min(6f, sweep * 0.20f);
            float actualSweep = sweep - gapDeg;

            Rectangle outerRect = new Rectangle(cx - outerR, cy - outerR, outerR * 2, outerR * 2);
            Rectangle innerRect = new Rectangle(cx - innerR, cy - innerR, innerR * 2, innerR * 2);

            for (int i = 0; i < count; i++)
            {
                float startAng = -90f + i * sweep + gapDeg / 2f;
                Color c = colors[i % colors.Length];

                using (GraphicsPath blockPath = new GraphicsPath())
                {
                    blockPath.AddArc(outerRect, startAng, actualSweep);
                    blockPath.AddArc(innerRect, startAng + actualSweep, -actualSweep);
                    blockPath.CloseFigure();

                    using (SolidBrush b = new SolidBrush(c))
                    {
                        g.FillPath(b, blockPath);
                    }
                    using (Pen p = new Pen(ControlPaint.Dark(c, 0.2f), 1.2f * zoom))
                    {
                        g.DrawPath(p, blockPath);
                    }
                }

                // Arrow head at the end of block pointing clockwise
                double endAngle = (startAng + actualSweep) * Math.PI / 180.0;
                int ax = cx + (int)(rad * Math.Cos(endAngle));
                int ay = cy + (int)(rad * Math.Sin(endAngle));
                double tx = -Math.Sin(endAngle);
                double ty = Math.Cos(endAngle);
                double nx = Math.Cos(endAngle);
                double ny = Math.Sin(endAngle);
                float aLen = blockThick * 0.8f;
                PointF pTip = new PointF((float)(ax + aLen * tx), (float)(ay + aLen * ty));
                PointF pL = new PointF((float)(ax + aLen * 0.6f * nx), (float)(ay + aLen * 0.6f * ny));
                PointF pR = new PointF((float)(ax - aLen * 0.6f * nx), (float)(ay - aLen * 0.6f * ny));
                using (SolidBrush ab = new SolidBrush(ControlPaint.Dark(c, 0.1f)))
                {
                    g.FillPolygon(ab, new PointF[] { pTip, pL, pR });
                }

                if (!SuppressNodeBodies)
                {
                    double midAngle = (startAng + actualSweep / 2f) * Math.PI / 180.0;
                    int txP = cx + (int)(rad * Math.Cos(midAngle));
                    int tyP = cy + (int)(rad * Math.Sin(midAngle));
                    int tw = (int)(blockThick * 1.4f);
                    int th = (int)(blockThick * 0.8f);
                    DrawAutoFitText(g, nodes[i], new Rectangle(txP - tw / 2, tyP - th / 2, tw, th), Color.White);
                }
            }
        }

        // ==================== 26. HIERARCHY HORIZONTAL ====================
        private static void DrawHierarchyHorizontal(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            if (nodes.Length == 0) return;
            int rw = r.Width / 3, rh = Math.Min(r.Height / Math.Max(1, nodes.Length), 40);
            Rectangle root = new Rectangle(r.X, r.Y + r.Height / 2 - rh / 2, rw, rh);
            using (SolidBrush b = new SolidBrush(colors[0])) g.FillRectangle(b, root);
            DrawAutoFitText(g, nodes[0], root, Color.White);
            for (int i = 1; i < nodes.Length; i++)
            {
                Rectangle cr = new Rectangle(r.X + rw + 40, r.Y + (i - 1) * (rh + 10), rw, rh);
                using (Pen p = new Pen(Color.Gray, 2f * zoom))
                {
                    g.DrawLine(p, root.Right, root.Y + rh / 2, cr.Left - 20, root.Y + rh / 2);
                    g.DrawLine(p, cr.Left - 20, root.Y + rh / 2, cr.Left - 20, cr.Y + rh / 2);
                    g.DrawLine(p, cr.Left - 20, cr.Y + rh / 2, cr.Left, cr.Y + rh / 2);
                }
                using (SolidBrush b = new SolidBrush(colors[i % colors.Length])) g.FillRectangle(b, cr);
                DrawAutoFitText(g, nodes[i], cr, Color.White);
            }
        }

        // ==================== 27. PYRAMID INVERTED ====================
        private static void DrawPyramidInverted(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;
            int h = r.Height / count;
            for (int i = 0; i < count; i++)
            {
                int wTop = r.Width - i * (r.Width / count);
                int wBot = r.Width - (i + 1) * (r.Width / count);
                Point[] pts = {
                    new Point(r.X + (r.Width - wTop) / 2, r.Y + i * h),
                    new Point(r.X + (r.Width + wTop) / 2, r.Y + i * h),
                    new Point(r.X + (r.Width + wBot) / 2, r.Y + (i + 1) * h - 2),
                    new Point(r.X + (r.Width - wBot) / 2, r.Y + (i + 1) * h - 2)
                };
                using (SolidBrush b = new SolidBrush(colors[i % colors.Length])) g.FillPolygon(b, pts);
                DrawAutoFitText(g, nodes[i], new Rectangle(r.X, r.Y + i * h, r.Width, h), Color.White);
            }
        }

        // ==================== 28. PYRAMID LIST ====================
        private static void DrawPyramidList(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            DrawPyramidLayers(g, new Rectangle(r.X, r.Y, r.Width / 2, r.Height), nodes, colors, zoom);
            DrawListBlocks(g, new Rectangle(r.X + r.Width / 2 + 10, r.Y, r.Width / 2 - 10, r.Height), nodes, colors, zoom);
        }

        // ==================== 29. STEP DOWN ====================
        private static void DrawStepDown(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;
            int w = r.Width / count, h = r.Height / count;
            for (int i = 0; i < count; i++)
            {
                Rectangle cr = new Rectangle(r.X + i * w, r.Y + i * h, w - 5, h - 5);
                using (SolidBrush b = new SolidBrush(colors[i % colors.Length])) g.FillRectangle(b, cr);
                DrawAutoFitText(g, nodes[i], cr, Color.White);
            }
        }

        // ==================== 30. CHEVRON VERTICAL ====================
        private static void DrawChevronVertical(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            if (SuppressNodeBodies) return;
            int count = nodes.Length;
            if (count == 0) return;
            int h = r.Height / count;
            for (int i = 0; i < count; i++)
            {
                Point[] pts = {
                    new Point(r.X, r.Y + i * h), new Point(r.Right, r.Y + i * h),
                    new Point(r.Right, r.Y + (i + 1) * h - 10), new Point(r.X + r.Width / 2, r.Y + (i + 1) * h),
                    new Point(r.X, r.Y + (i + 1) * h - 10)
                };
                using (SolidBrush b = new SolidBrush(colors[i % colors.Length])) g.FillPolygon(b, pts);
                DrawAutoFitText(g, nodes[i], new Rectangle(r.X, r.Y + i * h, r.Width, h - 10), Color.White);
            }
        }

        private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            d = Math.Min(d, Math.Min(rect.Width, rect.Height));
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ==================== 26. PICTURE ALTERNATING BLOCKS ====================
        private static void DrawPictureAlternatingBlocks(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int count = Math.Min(nodes.Length, 4);
            if (count <= 0) return;
            int gap = Math.Max(4, (int)(8 * zoom));
            int rowH = (r.Height - gap * (count - 1)) / count;

            for (int i = 0; i < count; i++)
            {
                int y = r.Y + i * (rowH + gap);
                int photoW = (int)(rowH * 1.3f);
                int textW = r.Width - photoW - gap;

                bool photoLeft = (i % 2 == 0);
                Rectangle photoRect = photoLeft 
                    ? new Rectangle(r.X, y, photoW, rowH)
                    : new Rectangle(r.Right - photoW, y, photoW, rowH);
                Rectangle textRect = photoLeft 
                    ? new Rectangle(r.X + photoW + gap, y, textW, rowH)
                    : new Rectangle(r.X, y, textW, rowH);

                // Draw photo placeholder
                using (SolidBrush pb = new SolidBrush(Color.FromArgb(240, 242, 245)))
                    g.FillRectangle(pb, photoRect);
                using (Pen pp = new Pen(Color.FromArgb(200, 205, 215), 1.0f))
                    g.DrawRectangle(pp, photoRect);

                int iconW = Math.Min(photoRect.Width - 6, (int)(24 * zoom));
                int iconH = Math.Min(photoRect.Height - 6, (int)(18 * zoom));
                int ix = photoRect.X + (photoRect.Width - iconW) / 2;
                int iy = photoRect.Y + (photoRect.Height - iconH) / 2;
                using (Pen ip = new Pen(Color.FromArgb(140, 145, 155), 1.2f * zoom))
                {
                    g.DrawRectangle(ip, ix, iy, iconW, iconH);
                    g.DrawEllipse(ip, ix + iconW - (int)(6 * zoom), iy + (int)(2 * zoom), (int)(3 * zoom), (int)(3 * zoom));
                }

                // Draw text box with gradient
                Color col = colors[i % colors.Length];
                using (LinearGradientBrush lgb = new LinearGradientBrush(textRect, col, ControlPaint.Dark(col, 0.15f), LinearGradientMode.Vertical))
                using (GraphicsPath path = CreateRoundedPath(textRect, (int)(4 * zoom)))
                {
                    g.FillPath(lgb, path);
                    using (Pen p = new Pen(ControlPaint.Dark(col, 0.3f), 1f))
                        g.DrawPath(p, path);
                }
                DrawAutoFitText(g, nodes[i], textRect, Color.White);
            }
        }

        // ==================== 27. CYCLE CIRCLE ARROW PROCESS ====================
        private static void DrawCycleCircleArrow(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int count = Math.Max(3, Math.Min(nodes.Length, 8));
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            int rad = (int)Math.Max(30f, Math.Min(r.Width, r.Height) * 0.32f);

            // Draw thick circular ring with uniform radius (TRUE 1:1 CIRCLE)
            using (Pen ringPen = new Pen(Color.FromArgb(180, 205, 230), 5f * zoom))
            {
                g.DrawArc(ringPen, cx - rad, cy - rad, rad * 2, rad * 2, 0, 360);
            }

            int nodeR = Math.Max(16, (int)(rad * 0.28f));

            for (int i = 0; i < count; i++)
            {
                double ang = i * 2.0 * Math.PI / count - Math.PI / 2.0;
                int nx = (int)(cx + rad * Math.Cos(ang));
                int ny = (int)(cy + rad * Math.Sin(ang));

                if (!SuppressNodeBodies)
                {
                    Rectangle nodeRect = new Rectangle(nx - nodeR, ny - nodeR, nodeR * 2, nodeR * 2);
                    Color col = colors[i % colors.Length];
                    using (LinearGradientBrush lgb = new LinearGradientBrush(nodeRect, col, ControlPaint.Dark(col, 0.2f), LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillEllipse(lgb, nodeRect);
                    }
                    using (Pen p = new Pen(Color.White, 2.5f * zoom))
                        g.DrawEllipse(p, nodeRect);

                    string label = (i < nodes.Length) ? nodes[i] : ("Node " + (i + 1));
                    DrawAutoFitText(g, label, nodeRect, Color.White);
                }

                // Tangential directional arrow on the ring between this node and next node
                double midAng = ang + Math.PI / count;
                int mx = (int)(cx + rad * Math.Cos(midAng));
                int my = (int)(cy + rad * Math.Sin(midAng));

                // Tangent vector clockwise is (-sin(midAng), cos(midAng))
                double tx = -Math.Sin(midAng);
                double ty = Math.Cos(midAng);
                // Normal vector outward is (cos(midAng), sin(midAng))
                double nxVec = Math.Cos(midAng);
                double nyVec = Math.Sin(midAng);

                float asize = 7f * zoom;
                PointF tip = new PointF((float)(mx + asize * 0.8f * tx), (float)(my + asize * 0.8f * ty));
                PointF leftWing = new PointF((float)(mx - asize * 0.6f * tx + asize * 0.6f * nxVec), (float)(my - asize * 0.6f * ty + asize * 0.6f * nyVec));
                PointF rightWing = new PointF((float)(mx - asize * 0.6f * tx - asize * 0.6f * nxVec), (float)(my - asize * 0.6f * ty - asize * 0.6f * nyVec));

                PointF[] arrowPts = new PointF[] { tip, leftWing, rightWing };
                using (SolidBrush ab = new SolidBrush(Color.FromArgb(91, 155, 213)))
                {
                    g.FillPolygon(ab, arrowPts);
                }
            }
        }

        public static void DrawStyledBorder(Graphics g, Rectangle rect, string style, Color color, float widthPt, float zoom)
        {
            if (widthPt <= 0 || color == Color.Transparent || color == Color.Empty) return;
            float w = Math.Max(1f, widthPt * 1.333333f * zoom);
            string s = (style ?? "solid").ToLowerInvariant();

            if (s == "none" || s == "nil") return;

            if (s == "dashed" || s == "dash")
            {
                using (Pen p = new Pen(color, w))
                {
                    p.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(p, rect);
                }
            }
            else if (s == "dotted" || s == "dot")
            {
                using (Pen p = new Pen(color, w))
                {
                    p.DashStyle = DashStyle.Dot;
                    g.DrawRectangle(p, rect);
                }
            }
            else if (s == "double" || s == "dbl")
            {
                float off = Math.Max(2f, w + 1f);
                using (Pen p = new Pen(color, Math.Max(1f, w * 0.7f)))
                {
                    g.DrawRectangle(p, rect);
                    Rectangle inner = new Rectangle(rect.X + (int)off, rect.Y + (int)off, Math.Max(1, rect.Width - (int)(off * 2)), Math.Max(1, rect.Height - (int)(off * 2)));
                    g.DrawRectangle(p, inner);
                }
            }
            else if (s == "threedemboss" || s == "emboss" || s == "bevel" || s == "3d")
            {
                Color hiCol = ControlPaint.Light(color, 0.4f);
                Color shCol = ControlPaint.Dark(color, 0.3f);
                using (Pen hiPen = new Pen(hiCol, w))
                using (Pen shPen = new Pen(shCol, w))
                {
                    g.DrawLine(hiPen, rect.Left, rect.Top, rect.Right, rect.Top);
                    g.DrawLine(hiPen, rect.Left, rect.Top, rect.Left, rect.Bottom);
                    g.DrawLine(shPen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                    g.DrawLine(shPen, rect.Right, rect.Top, rect.Right, rect.Bottom);
                }
            }
            else if (s == "threedengrave" || s == "engrave")
            {
                Color hiCol = ControlPaint.Light(color, 0.4f);
                Color shCol = ControlPaint.Dark(color, 0.3f);
                using (Pen hiPen = new Pen(hiCol, w))
                using (Pen shPen = new Pen(shCol, w))
                {
                    g.DrawLine(shPen, rect.Left, rect.Top, rect.Right, rect.Top);
                    g.DrawLine(shPen, rect.Left, rect.Top, rect.Left, rect.Bottom);
                    g.DrawLine(hiPen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                    g.DrawLine(hiPen, rect.Right, rect.Top, rect.Right, rect.Bottom);
                }
            }
            else if (s.Contains("shadow"))
            {
                Rectangle shRect = new Rectangle(rect.X + (int)(3 * zoom), rect.Y + (int)(3 * zoom), rect.Width, rect.Height);
                using (SolidBrush shB = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                {
                    g.FillRectangle(shB, shRect);
                }
                using (Pen p = new Pen(color, w))
                {
                    g.DrawRectangle(p, rect);
                }
            }
            else
            {
                // Solid / single
                using (Pen p = new Pen(color, w))
                {
                    g.DrawRectangle(p, rect);
                }
            }
        }

        public static void DrawStyledBorder(Graphics g, GraphicsPath path, string style, Color color, float widthPt, float zoom)
        {
            if (path == null || widthPt <= 0 || color == Color.Transparent || color == Color.Empty) return;
            float w = Math.Max(1f, widthPt * 1.333333f * zoom);
            string s = (style ?? "solid").ToLowerInvariant();

            if (s == "none" || s == "nil") return;

            if (s.Contains("shadow"))
            {
                using (GraphicsPath shPath = (GraphicsPath)path.Clone())
                using (Matrix m = new Matrix())
                {
                    m.Translate(3f * zoom, 3f * zoom);
                    shPath.Transform(m);
                    using (SolidBrush shB = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        g.FillPath(shB, shPath);
                    }
                }
            }

            if (s == "dashed" || s == "dash")
            {
                using (Pen p = new Pen(color, w))
                {
                    p.DashStyle = DashStyle.Dash;
                    g.DrawPath(p, path);
                }
            }
            else if (s == "dotted" || s == "dot")
            {
                using (Pen p = new Pen(color, w))
                {
                    p.DashStyle = DashStyle.Dot;
                    g.DrawPath(p, path);
                }
            }
            else if (s == "double" || s == "dbl")
            {
                float dblW = Math.Max(3f, w * 2.2f);
                using (Pen dblPen = new Pen(color, dblW))
                {
                    dblPen.CompoundArray = new float[] { 0.0f, 0.33f, 0.67f, 1.0f };
                    g.DrawPath(dblPen, path);
                }
            }
            else if (s == "threedemboss" || s == "emboss" || s == "bevel" || s == "3d")
            {
                Color hiCol = ControlPaint.Light(color, 0.5f);
                Color shCol = ControlPaint.Dark(color, 0.35f);
                using (GraphicsPath hiPath = (GraphicsPath)path.Clone())
                using (Matrix m = new Matrix())
                using (Pen hiPen = new Pen(hiCol, Math.Max(1f, w * 0.8f)))
                using (Pen shPen = new Pen(shCol, w))
                {
                    m.Translate(-1f * zoom, -1f * zoom);
                    hiPath.Transform(m);
                    g.DrawPath(hiPen, hiPath);
                    g.DrawPath(shPen, path);
                }
            }
            else if (s == "threedengrave" || s == "engrave")
            {
                Color hiCol = ControlPaint.Light(color, 0.5f);
                Color shCol = ControlPaint.Dark(color, 0.35f);
                using (GraphicsPath shPath = (GraphicsPath)path.Clone())
                using (Matrix m = new Matrix())
                using (Pen shPen = new Pen(shCol, Math.Max(1f, w * 0.8f)))
                using (Pen hiPen = new Pen(hiCol, w))
                {
                    m.Translate(-1f * zoom, -1f * zoom);
                    shPath.Transform(m);
                    g.DrawPath(shPen, shPath);
                    g.DrawPath(hiPen, path);
                }
            }
            else
            {
                using (Pen p = new Pen(color, w))
                {
                    g.DrawPath(p, path);
                }
            }
        }

        private static void DrawVectorArrowRight(Graphics g, int x, int y, int length, int headH, Color col)
        {
            int headW = headH / 2 + 2;
            int stemH = Math.Max(3, headH / 3);
            PointF[] arrowPts = new PointF[]
            {
                new PointF(x, y - stemH / 2f),
                new PointF(x + length - headW, y - stemH / 2f),
                new PointF(x + length - headW, y - headH / 2f),
                new PointF(x + length, y),
                new PointF(x + length - headW, y + headH / 2f),
                new PointF(x + length - headW, y + stemH / 2f),
                new PointF(x, y + stemH / 2f)
            };
            using (SolidBrush ab = new SolidBrush(col))
            {
                g.FillPolygon(ab, arrowPts);
            }
        }

        public static Bitmap RenderToBitmap(DocShapeObject shape, int width, int height, float scale)
        {
            int w = Math.Max(200, (int)(width * scale));
            int h = Math.Max(120, (int)(height * scale));
            Bitmap bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var gdi = GdiGraphics.FromImage(bmp))
            using (var g = new Graphics(gdi, w, h))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.Transparent);

                Rectangle rect = new Rectangle(4, 4, w - 8, h - 8);
                string saType = (shape != null && !string.IsNullOrEmpty(shape.SmartArtType)) ? shape.SmartArtType : "Cycle_Radial";
                string title = shape != null ? shape.SmartArtTitle : "";
                string text = shape != null ? shape.TextContent : "";
                string[] nodes = GetSmartArtNodeList(saType, text);
                Color[] colors;
                if (shape != null && shape.ResolvedThemePalette != null && shape.ResolvedThemePalette.Length > 0)
                {
                    colors = new Color[nodes.Length];
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        colors[i] = (shape.StylePresetIndex == 2)
                            ? shape.ResolvedThemePalette[i % shape.ResolvedThemePalette.Length]
                            : shape.ResolvedThemePalette[0];
                    }
                }
                else
                {
                    int preset = shape != null ? shape.StylePresetIndex : 0;
                    colors = GetPaletteColors(preset, nodes.Length);
                }
                if (shape != null && shape.SubShapeRelativeBounds != null && shape.SubShapeRelativeBounds.Count > 0)
                {
                    ShapeRenderer.DrawShape(g, shape, rect, null, scale);
                }
                else
                {
                    DrawSmartArt(g, rect, saType, title, colors, nodes, false, scale);
                }
            }
            return bmp;
        }

        public static Bitmap RenderShapeToBitmap(DocShapeObject shape, int width, int height, float scale)
        {
            int w = Math.Max(60, (int)(width * scale));
            int h = Math.Max(40, (int)(height * scale));
            Bitmap bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var gdi = GdiGraphics.FromImage(bmp))
            using (var g = new Graphics(gdi, w, h))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.Transparent);

                Rectangle rect = new Rectangle(4, 4, w - 8, h - 8);
                ShapeRenderer.DrawShape(g, shape, rect, null, scale);
            }
            return bmp;
        }

        public static GraphicsPath CreateCircularArrowPath(bool isClockwiseTop, RectangleF rf)
        {
            GraphicsPath path = new GraphicsPath();
            float w = Math.Max(6f, Math.Min(rf.Width, rf.Height) * 0.12f);
            float headWing = w * 0.6f;

            RectangleF outerR = rf;
            RectangleF innerR = new RectangleF(rf.Left + w, rf.Top + w, Math.Max(2, rf.Width - w * 2), Math.Max(2, rf.Height - w * 2));

            if (isClockwiseTop)
            {
                path.AddArc(outerR, 180, 165);
                PointF outerWing = new PointF(rf.Right + headWing, rf.Top + rf.Height * 0.42f);
                PointF tip = new PointF(rf.Right - w / 2, rf.Top + rf.Height * 0.54f);
                PointF innerWing = new PointF(rf.Right - w - headWing, rf.Top + rf.Height * 0.42f);
                path.AddLine(path.GetLastPoint(), outerWing);
                path.AddLine(outerWing, tip);
                path.AddLine(tip, innerWing);
                path.AddArc(innerR, 342, -162);
                path.CloseFigure();
            }
            else
            {
                path.AddArc(outerR, 180, -165);
                PointF outerWing = new PointF(rf.Right + headWing, rf.Top + rf.Height * 0.58f);
                PointF tip = new PointF(rf.Right - w / 2, rf.Top + rf.Height * 0.46f);
                PointF innerWing = new PointF(rf.Right - w - headWing, rf.Top + rf.Height * 0.58f);
                path.AddLine(path.GetLastPoint(), outerWing);
                path.AddLine(outerWing, tip);
                path.AddLine(tip, innerWing);
                path.AddArc(innerR, 18, 162);
                path.CloseFigure();
            }
            return path;
        }

        private static void DrawAlternatingFlow(Graphics g, Rectangle r, string[] nodes, Color[] colors, float zoom)
        {
            int count = Math.Max(1, nodes.Length);
            int gap = Math.Max(6, (int)(12 * zoom));
            int cardW = Math.Max(60, (r.Width - gap * (count + 1)) / count);
            int cardH = (int)(r.Height * 0.68f);

            for (int i = 0; i < count; i++)
            {
                int cx = r.X + gap + i * (cardW + gap);
                int cy = r.Y + (int)(r.Height * 0.16f);
                Rectangle cardR = new Rectangle(cx, cy, cardW, cardH);

                // Draw card background
                using (GraphicsPath cp = CreateRoundedPath(cardR, (int)(8 * zoom)))
                using (SolidBrush cfb = new SolidBrush(Color.White))
                using (Pen clp = new Pen(Color.FromArgb(180, 200, 230), 1.5f))
                {
                    g.FillPath(cfb, cp);
                    g.DrawPath(clp, cp);
                }

                // Draw pill badge
                bool isTop = (i % 2 == 1);
                int pillH = Math.Max(24, (int)(cardH * 0.36f));
                int pillW = (int)(cardW * 0.92f);
                int px = cardR.X + (cardR.Width - pillW) / 2;
                int py = isTop ? (cardR.Top - pillH / 3) : (cardR.Bottom - pillH * 2 / 3);
                Rectangle pillR = new Rectangle(px, py, pillW, pillH);

                Color pillCol = colors[i % colors.Length];
                using (GraphicsPath pp = CreateRoundedPath(pillR, (int)(6 * zoom)))
                using (SolidBrush pfb = new SolidBrush(pillCol))
                using (Pen plp = new Pen(ControlPaint.Dark(pillCol, 0.2f), 1f))
                {
                    g.FillPath(pfb, pp);
                    g.DrawPath(plp, pp);
                }

                // Draw pill text
                DrawAutoFitText(g, nodes[i], pillR, Color.White);

                // Connecting curved arrow to next card
                if (i < count - 1)
                {
                    int arrowX = cardR.Right - cardW / 3;
                    int arrowY = isTop ? (cardR.Bottom - cardH / 2) : (cardR.Top - cardH / 4);
                    int arrowS = Math.Max(30, (int)(cardW * 0.85f));
                    RectangleF arrowR = new RectangleF(arrowX, arrowY, arrowS, arrowS);
                    using (GraphicsPath ap = CreateCircularArrowPath(!isTop, arrowR))
                    using (SolidBrush ab = new SolidBrush(Color.FromArgb(185, 205, 240)))
                    using (Pen apn = new Pen(Color.FromArgb(140, 170, 220), 1.2f))
                    {
                        g.FillPath(ab, ap);
                        g.DrawPath(apn, ap);
                    }
                }
            }
        }
    }
}
