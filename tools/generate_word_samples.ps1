# tools/generate_word_samples.ps1
# Automated Microsoft Word 16.0 COM Canonical Sample Generator
# GenOffice OpenXML Ground Truth & Canvas Synchronization Engine
# Generates 200+ SmartArt diagrams (8 categories, 3/4/5/7 nodes, colors/styles),
# Shapes library suite, Table borders & shading suite, OMML Math suite, and 5 application scenarios.

param(
    [string]$OutputDir = "$PSScriptRoot/../tests/samples",
    [int]$MaxSmartArt = 0,         # 0 means all (200+)
    [switch]$SkipSmartArt,
    [switch]$SkipSuites,
    [switch]$SkipScenarios,
    [switch]$VerboseLog
)

$ErrorActionPreference = "Stop"

$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)

# Ensure output directories exist
$smartArtDir = [System.IO.Path]::GetFullPath((Join-Path $OutputDir "smartart"))
$suitesDir = [System.IO.Path]::GetFullPath((Join-Path $OutputDir "suites"))
$scenariosDir = [System.IO.Path]::GetFullPath((Join-Path $OutputDir "scenarios"))

foreach ($dir in @($OutputDir, $smartArtDir, $suitesDir, $scenariosDir)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " GENOFFICE CANONICAL SAMPLE GENERATOR (MS WORD 16.0 COM)         " -ForegroundColor Cyan
Write-Host " Output Directory: $OutputDir                                    " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# Helper to sanitize file names
function Get-SafeFileName([string]$name) {
    return ($name -replace '[^a-zA-Z0-9_]', '_').Trim('_').ToLower()
}

# Helper to adjust SmartArt node count
function Set-SmartArtNodeCount($sa, [int]$targetCount, [string]$title) {
    if ($targetCount -le 0) { return }
    
    # Adjust top-level nodes count
    while ($sa.Nodes.Count -lt $targetCount) {
        try {
            [void]$sa.Nodes.Add()
        } catch {
            break
        }
    }
    while ($sa.Nodes.Count -gt $targetCount -and $sa.Nodes.Count -gt 1) {
        try {
            $sa.Nodes.Item($sa.Nodes.Count).Delete()
        } catch {
            break
        }
    }
    
    # Populate text for all nodes
    for ($ni = 1; $ni -le $sa.AllNodes.Count; $ni++) {
        try {
            $node = $sa.AllNodes.Item($ni)
            $node.TextFrame2.TextRange.Text = "Item $($ni): $($title)"
        } catch {}
    }
}

# Launch MS Word COM Application
Write-Host "[1/5] Initializing Microsoft Word 16.0 COM Automation..." -ForegroundColor Yellow
$word = New-Object -ComObject "Word.Application"
$word.Visible = $false
$word.DisplayAlerts = 0
try { $word.Options.SaveInterval = 0 } catch {}
try { $word.Options.SavePropertiesPrompt = $false } catch {}

# Keep a warm base document active to maintain Word's internal SmartArtLayouts cache
$baseDoc = $word.Documents.Add()

$swTotal = [System.Diagnostics.Stopwatch]::StartNew()
$generatedFiles = @()

try {
    $layouts = $word.SmartArtLayouts
    $colors = $word.SmartArtColors
    $styles = $word.SmartArtQuickStyles
    Write-Host "Connected to Word 16.0: $($layouts.Count) SmartArt Layouts, $($colors.Count) Colors, $($styles.Count) QuickStyles available." -ForegroundColor Green

    # =========================================================================
    # PART 1: SMARTART DIAGRAM SAMPLES (200+ DIAGRAMS ACROSS 8 CATEGORIES)
    # =========================================================================
    if (-not $SkipSmartArt) {
        Write-Host "`n[2/5] Generating 200+ SmartArt Diagrams across 8 categories..." -ForegroundColor Yellow
        $saGeneratedCount = 0

        # Phase 1A: Base Layouts (All 151 standard layouts)
        Write-Host "--- Phase 1A: Canonical Base Layouts (151 layouts) ---" -ForegroundColor Cyan
        $ErrorActionPreference = "Continue"

        for ($i = 1; $i -le $layouts.Count; $i++) {
            if ($MaxSmartArt -gt 0 -and $saGeneratedCount -ge $MaxSmartArt) { break }
            
            $doc = $null
            try {
                $layout = $layouts.Item($i)
                $cat = "smartart"
                try { if ($layout.Category) { $cat = $layout.Category.ToLower() } } catch {}
                $name = "layout_$i"
                try { if ($layout.Name) { $name = $layout.Name } } catch {}
                $safeName = Get-SafeFileName $name
                $fileName = "sa_${cat}_${safeName}_base.docx"
                $filePath = Join-Path $smartArtDir $fileName

                $doc = $word.Documents.Add()
                $p = $doc.Paragraphs.Add()
                $p.Range.Text = "SmartArt Canonical Sample: [$cat] $name (Base Default)"
                $p.Range.Font.Bold = 1
                $p.Range.Font.Size = 14
                
                # Add SmartArt shape
                $shape = $doc.Shapes.AddSmartArt($layout, 50, 70, 480, 320)
                $sa = $shape.SmartArt
                
                # Populate text
                for ($ni = 1; $ni -le $sa.AllNodes.Count; $ni++) {
                    try {
                        $node = $sa.AllNodes.Item($ni)
                        $node.TextFrame2.TextRange.Text = "Step $($ni): $($name)"
                    } catch {}
                }

                if (Test-Path $filePath) { Remove-Item $filePath -Force }
                $doc.SaveAs2($filePath, 16)
                $doc.Close(0)
                $doc = $null
                $generatedFiles += $filePath
                $saGeneratedCount++

                Write-Host "  [$saGeneratedCount] Generated: $fileName" -ForegroundColor Gray
            } catch {
                Write-Warning "Failed to generate SmartArt base layout #$($i): $_"
                if ($null -ne $doc) { try { $doc.Close(0) } catch {} }
            }
        }

        # Phase 1B: Varied Node Counts (3, 4, 5, 7 nodes) & Style/Color Matrix
        Write-Host "--- Phase 1B: Targeted Node Cardinality & Style Matrix (Variations) ---" -ForegroundColor Cyan
        
        # High-priority layouts representing all 8 categories for exhaustive node count and style variations
        $variationMatrix = @(
            # Category: Cycle (Circle 1:1, Radial, Ring)
            @{ Name = "Basic Cycle"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 1 },
            @{ Name = "Radial Cycle"; Nodes = @(3, 4, 5, 7); ColorIdx = 3; StyleIdx = 3 },
            @{ Name = "Block Cycle"; Nodes = @(3, 4, 5, 7); ColorIdx = 4; StyleIdx = 2 },
            @{ Name = "Continuous Cycle"; Nodes = @(3, 4, 5, 7); ColorIdx = 5; StyleIdx = 4 },
            @{ Name = "Cycle Matrix"; Nodes = @(3, 4, 5); ColorIdx = 6; StyleIdx = 5 },
            @{ Name = "Multidirectional Cycle"; Nodes = @(3, 4, 5, 7); ColorIdx = 1; StyleIdx = 6 },

            # Category: Hierarchy (Org Chart, Tree, Horizontal)
            @{ Name = "Organization Chart"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 1 },
            @{ Name = "Hierarchy"; Nodes = @(3, 4, 5, 7); ColorIdx = 3; StyleIdx = 2 },
            @{ Name = "Horizontal Hierarchy"; Nodes = @(3, 4, 5, 7); ColorIdx = 4; StyleIdx = 3 },
            @{ Name = "Horizontal Organization Chart"; Nodes = @(3, 4, 5, 7); ColorIdx = 5; StyleIdx = 1 },
            @{ Name = "Hierarchy List"; Nodes = @(3, 4, 5, 7); ColorIdx = 6; StyleIdx = 4 },
            @{ Name = "Labeled Hierarchy"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 5 },

            # Category: Process (Linear, Chevron, Timeline)
            @{ Name = "Basic Process"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 1 },
            @{ Name = "Basic Chevron Process"; Nodes = @(3, 4, 5, 7); ColorIdx = 3; StyleIdx = 2 },
            @{ Name = "Continuous Arrow Process"; Nodes = @(3, 4, 5, 7); ColorIdx = 4; StyleIdx = 3 },
            @{ Name = "Detailed Process"; Nodes = @(3, 4, 5, 7); ColorIdx = 5; StyleIdx = 1 },
            @{ Name = "Accent Process"; Nodes = @(3, 4, 5, 7); ColorIdx = 6; StyleIdx = 4 },
            @{ Name = "Increasing Arrows Process"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 5 },

            # Category: Matrix (2x2 Quadrants & Grid)
            @{ Name = "Basic Matrix"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 1 },
            @{ Name = "Grid Matrix"; Nodes = @(3, 4, 5, 7); ColorIdx = 3; StyleIdx = 2 },
            @{ Name = "Titled Matrix"; Nodes = @(3, 4, 5, 7); ColorIdx = 4; StyleIdx = 3 },

            # Category: Pyramid (Tiers)
            @{ Name = "Basic Pyramid"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 1 },
            @{ Name = "Inverted Pyramid"; Nodes = @(3, 4, 5, 7); ColorIdx = 3; StyleIdx = 2 },
            @{ Name = "Segmented Pyramid"; Nodes = @(3, 4, 5, 7); ColorIdx = 4; StyleIdx = 3 },
            @{ Name = "Pyramid List"; Nodes = @(3, 4, 5, 7); ColorIdx = 5; StyleIdx = 4 },

            # Category: Relationship (Venn, Target, Radial)
            @{ Name = "Basic Venn"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 1 },
            @{ Name = "Radial Venn"; Nodes = @(3, 4, 5, 7); ColorIdx = 3; StyleIdx = 2 },
            @{ Name = "Basic Target"; Nodes = @(3, 4, 5, 7); ColorIdx = 4; StyleIdx = 3 },
            @{ Name = "Counterbalance Arrows"; Nodes = @(3, 4, 5); ColorIdx = 5; StyleIdx = 1 },
            @{ Name = "Converging Arrows"; Nodes = @(3, 4, 5, 7); ColorIdx = 6; StyleIdx = 4 },
            @{ Name = "Diverging Arrows"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 5 },

            # Category: List (Hexagon, Blocks, Bullets)
            @{ Name = "Alternating Hexagons"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 1 },
            @{ Name = "Vertical Box List"; Nodes = @(3, 4, 5, 7); ColorIdx = 3; StyleIdx = 2 },
            @{ Name = "Horizontal Bullet List"; Nodes = @(3, 4, 5, 7); ColorIdx = 4; StyleIdx = 3 },
            @{ Name = "Trapezoid List"; Nodes = @(3, 4, 5, 7); ColorIdx = 5; StyleIdx = 4 },
            @{ Name = "Tab List"; Nodes = @(3, 4, 5, 7); ColorIdx = 6; StyleIdx = 5 },

            # Category: Picture
            @{ Name = "Bubble Picture List"; Nodes = @(3, 4, 5, 7); ColorIdx = 2; StyleIdx = 1 },
            @{ Name = "Continuous Picture List"; Nodes = @(3, 4, 5, 7); ColorIdx = 3; StyleIdx = 2 },
            @{ Name = "Picture Lineup"; Nodes = @(3, 4, 5, 7); ColorIdx = 4; StyleIdx = 3 }
        )

        foreach ($entry in $variationMatrix) {
            $targetName = $entry.Name
            
            # Find the matching layout object
            $targetLayout = $null
            for ($k = 1; $k -le $layouts.Count; $k++) {
                if ($layouts.Item($k).Name -eq $targetName) {
                    $targetLayout = $layouts.Item($k)
                    break
                }
            }
            if ($null -eq $targetLayout) { continue }

            $cat = "smartart"
            try { if ($targetLayout.Category) { $cat = $targetLayout.Category.ToLower() } } catch {}
            $safeName = Get-SafeFileName $targetName

            foreach ($nodeCount in $entry.Nodes) {
                if ($MaxSmartArt -gt 0 -and $saGeneratedCount -ge $MaxSmartArt) { break }

                $fileName = "sa_${cat}_${safeName}_n${nodeCount}_c$($entry.ColorIdx)_s$($entry.StyleIdx).docx"
                $filePath = Join-Path $smartArtDir $fileName

                $doc = $null
                try {
                    $doc = $word.Documents.Add()
                    $p = $doc.Paragraphs.Add()
                    $p.Range.Text = "SmartArt Variation: [$cat] $targetName ($nodeCount Nodes, Color $($entry.ColorIdx), Style $($entry.StyleIdx))"
                    $p.Range.Font.Bold = 1
                    $p.Range.Font.Size = 13

                    $shape = $doc.Shapes.AddSmartArt($targetLayout, 50, 70, 480, 320)
                    $sa = $shape.SmartArt

                    # Apply color and style
                    if ($entry.ColorIdx -le $colors.Count) {
                        try { $sa.Color = $colors.Item($entry.ColorIdx) } catch {}
                    }
                    if ($entry.StyleIdx -le $styles.Count) {
                        try { $sa.QuickStyle = $styles.Item($entry.StyleIdx) } catch {}
                    }

                    # Adjust node cardinality and populate text
                    Set-SmartArtNodeCount $sa $nodeCount $targetName

                    if (Test-Path $filePath) { Remove-Item $filePath -Force }
                    $doc.SaveAs2($filePath, 16)
                    $doc.Close(0)
                    $doc = $null
                    $generatedFiles += $filePath
                    $saGeneratedCount++

                    Write-Host "  [$saGeneratedCount] Generated: $fileName" -ForegroundColor Gray
                } catch {
                    Write-Warning "Failed to generate variation for $targetName (n=$nodeCount): $_"
                    if ($null -ne $doc) { try { $doc.Close(0) } catch {} }
                }
            }
        }

        Write-Host "SmartArt Generation Finished: Total $saGeneratedCount diagrams created in $smartArtDir" -ForegroundColor Green
    }

    # =========================================================================
    # PART 2: FULL SHAPE LIBRARY SUITE (RECTANGLES, BASIC, ARROWS, 3D, BORDERS)
    # =========================================================================
    if (-not $SkipSuites) {
        Write-Host "`n[3/5] Generating Comprehensive Shape Library Suite..." -ForegroundColor Yellow
        $shapeDocPath = Join-Path $suitesDir "shapes_library_suite.docx"
        $doc = $word.Documents.Add()

        try {
            # Title
            $p = $doc.Paragraphs.Add()
            $p.Range.Text = "GenOffice Canonical Shapes, Borders & 3D Test Suite"
            $p.Range.Font.Bold = 1
            $p.Range.Font.Size = 18
            $p.Range.InsertParagraphAfter()

            # Section 1: Rectangles & Lines with various border styles
            $p = $doc.Paragraphs.Add()
            $p.Range.Text = "1. Rectangles & Line Border Variations (Solid, Dash, Double, Compound)"
            $p.Range.Font.Bold = 1
            $p.Range.Font.Size = 14
            $p.Range.InsertParagraphAfter()

            # Shape 1: Solid Single Border Rectangle
            $s1 = $doc.Shapes.AddShape(1, 60, 110, 140, 70) # msoShapeRectangle
            $s1.Line.Weight = 2.0
            $s1.Line.DashStyle = 1 # msoLineSolid
            $s1.Fill.ForeColor.RGB = 0x4472C4
            $s1.TextFrame.TextRange.Text = "Solid 2pt"
            $s1.TextFrame.TextRange.Font.Color.RGB = 0xFFFFFF

            # Shape 2: Double Border Rectangle (ThinThin)
            $s2 = $doc.Shapes.AddShape(1, 220, 110, 140, 70)
            $s2.Line.Weight = 4.5
            $s2.Line.Style = 2 # msoLineThinThin (Double border)
            $s2.Fill.ForeColor.RGB = 0xED7D31
            $s2.TextFrame.TextRange.Text = "Double 4.5pt"
            $s2.TextFrame.TextRange.Font.Color.RGB = 0xFFFFFF

            # Shape 3: Dashed Border Rounded Rectangle
            $s3 = $doc.Shapes.AddShape(5, 380, 110, 140, 70) # msoShapeRoundedRectangle
            $s3.Line.Weight = 2.25
            $s3.Line.DashStyle = 4 # msoLineDash
            $s3.Fill.ForeColor.RGB = 0x70AD47
            $s3.TextFrame.TextRange.Text = "Dash 2.25pt"
            $s3.TextFrame.TextRange.Font.Color.RGB = 0xFFFFFF

            # Section 2: Basic Shapes & Circle 1:1 Aspect Ratio Testing
            $p2 = $doc.Paragraphs.Add()
            $p2.Range.Text = "`n`n`n`n`n`n2. Basic Shapes & True 1:1 Circle Geometry"
            $p2.Range.Font.Bold = 1
            $p2.Range.Font.Size = 14
            $p2.Range.InsertParagraphAfter()

            # Shape 4: True 1:1 Circle (Oval with Width == Height == 90pt)
            $s4 = $doc.Shapes.AddShape(9, 60, 230, 90, 90) # msoShapeOval (1:1 Ratio)
            $s4.Line.Weight = 2.0
            $s4.Fill.ForeColor.RGB = 0x5B9BD5
            $s4.TextFrame.TextRange.Text = "Circle 1:1"
            $s4.TextFrame.TextRange.Font.Color.RGB = 0xFFFFFF

            # Shape 5: Triangle
            $s5 = $doc.Shapes.AddShape(7, 170, 230, 90, 90) # msoShapeIsoscelesTriangle
            $s5.Fill.ForeColor.RGB = 0xFFC000
            $s5.TextFrame.TextRange.Text = "Triangle"

            # Shape 6: Hexagon
            $s6 = $doc.Shapes.AddShape(10, 280, 230, 90, 90) # msoShapeHexagon
            $s6.Fill.ForeColor.RGB = 0x70AD47
            $s6.TextFrame.TextRange.Text = "Hexagon"
            $s6.TextFrame.TextRange.Font.Color.RGB = 0xFFFFFF

            # Shape 7: Cube with 3D Depth
            $s7 = $doc.Shapes.AddShape(14, 390, 230, 90, 90) # msoShapeCube
            $s7.Fill.ForeColor.RGB = 0x8064A2
            $s7.ThreeD.Depth = 25
            $s7.TextFrame.TextRange.Text = "Cube 3D"
            $s7.TextFrame.TextRange.Font.Color.RGB = 0xFFFFFF

            # Section 3: Visual Effects (Drop Shadow, Gradients, 3D Bevel)
            $p3 = $doc.Paragraphs.Add()
            $p3.Range.Text = "`n`n`n`n`n`n3. Visual Effects (Drop Shadow, Two-Color Gradient, 3D Bevel Top)"
            $p3.Range.Font.Bold = 1
            $p3.Range.Font.Size = 14
            $p3.Range.InsertParagraphAfter()

            # Shape 8: Rectangle with Drop Shadow
            $s8 = $doc.Shapes.AddShape(1, 60, 360, 130, 75)
            $s8.Fill.ForeColor.RGB = 0x4472C4
            $s8.Shadow.Type = 21 # Outer Drop Shadow
            $s8.TextFrame.TextRange.Text = "Drop Shadow"
            $s8.TextFrame.TextRange.Font.Color.RGB = 0xFFFFFF

            # Shape 9: Rounded Rectangle with Two-Color Horizontal Gradient
            $s9 = $doc.Shapes.AddShape(5, 210, 360, 130, 75)
            $s9.Fill.TwoColorGradient(1, 1) # msoGradientHorizontal, style 1
            $s9.TextFrame.TextRange.Text = "Gradient Fill"
            $s9.TextFrame.TextRange.Font.Color.RGB = 0xFFFFFF

            # Shape 10: 1:1 Circle with 3D Bevel Top
            $s10 = $doc.Shapes.AddShape(9, 360, 350, 95, 95) # Oval 1:1
            $s10.Fill.ForeColor.RGB = 0xED7D31
            $s10.ThreeD.BevelTopType = 2 # msoBevelCircle
            $s10.ThreeD.Depth = 20
            $s10.TextFrame.TextRange.Text = "Bevel 3D"
            $s10.TextFrame.TextRange.Font.Color.RGB = 0xFFFFFF

            # Section 4: Block Arrows, Flowchart, Stars & Callouts
            $p4 = $doc.Paragraphs.Add()
            $p4.Range.Text = "`n`n`n`n`n`n4. Block Arrows, Flowchart, Stars & Callouts"
            $p4.Range.Font.Bold = 1
            $p4.Range.Font.Size = 14
            $p4.Range.InsertParagraphAfter()

            # Shape 11: Right Arrow
            $s11 = $doc.Shapes.AddShape(33, 60, 480, 110, 60) # msoShapeRightArrow
            $s11.Fill.ForeColor.RGB = 0x4472C4

            # Shape 12: Flowchart Decision (Diamond)
            $s12 = $doc.Shapes.AddShape(63, 190, 475, 90, 70) # msoShapeFlowchartDecision
            $s12.Fill.ForeColor.RGB = 0xFFC000
            $s12.TextFrame.TextRange.Text = "Decision"

            # Shape 13: 8-Point Star
            $s13 = $doc.Shapes.AddShape(94, 300, 470, 80, 80) # msoShape8pointStar
            $s13.Fill.ForeColor.RGB = 0xED7D31

            # Shape 14: Rectangular Callout
            $s14 = $doc.Shapes.AddShape(105, 400, 470, 110, 70) # msoShapeRectangularCallout
            $s14.Fill.ForeColor.RGB = 0x70AD47
            $s14.TextFrame.TextRange.Text = "Callout"
            $s14.TextFrame.TextRange.Font.Color.RGB = 0xFFFFFF

            if (Test-Path $shapeDocPath) { Remove-Item $shapeDocPath -Force }
            $doc.SaveAs2($shapeDocPath, 16)
            $doc.Close(0)
            $generatedFiles += $shapeDocPath
            Write-Host "Shape Library Suite generated: $shapeDocPath" -ForegroundColor Green
        } catch {
            Write-Warning "Failed to generate Shape Library Suite: $_"
            if ($null -ne $doc) { try { $doc.Close(0) } catch {} }
        }

        # =========================================================================
        # PART 3: TABLE BORDERS & SHADING SUITE
        # =========================================================================
        Write-Host "`n[4/5] Generating Table Borders & Shading Test Suite..." -ForegroundColor Yellow
        $tableDocPath = Join-Path $suitesDir "table_borders_suite.docx"
        $doc = $word.Documents.Add()

        try {
            $p = $doc.Paragraphs.Add()
            $p.Range.Text = "GenOffice Canonical Table Borders & Shading Test Suite"
            $p.Range.Font.Bold = 1
            $p.Range.Font.Size = 18
            $p.Range.InsertParagraphAfter()

            # Table 1: Single Solid Borders (sz=8, 1pt)
            $p1 = $doc.Paragraphs.Add()
            $p1.Range.Text = "Table 1: Single Solid Borders (1.0 pt / sz=8)"
            $p1.Range.Font.Bold = 1
            $p1.Range.Font.Size = 12
            $t1 = $doc.Tables.Add($p1.Range, 3, 3)
            for ($r = 1; $r -le 3; $r++) {
                for ($c = 1; $c -le 3; $c++) {
                    $t1.Cell($r, $c).Range.Text = "T1 [$r,$c]"
                }
            }
            for ($b = -1; $b -ge -6; $b--) {
                $t1.Borders.Item($b).LineStyle = 1 # wdLineStyleSingle
                $t1.Borders.Item($b).LineWidth = 8  # 1.0 pt
                $t1.Borders.Item($b).Color = 0x4472C4
            }
            $doc.Paragraphs.Add().Range.InsertParagraphAfter()

            # Table 2: Double Borders (sz=24, 3pt)
            $p2 = $doc.Paragraphs.Add()
            $p2.Range.Text = "Table 2: Double Line Borders (ThinThin / double)"
            $p2.Range.Font.Bold = 1
            $p2.Range.Font.Size = 12
            $t2 = $doc.Tables.Add($p2.Range, 3, 3)
            for ($r = 1; $r -le 3; $r++) {
                for ($c = 1; $c -le 3; $c++) {
                    $t2.Cell($r, $c).Range.Text = "T2 [$r,$c]"
                }
            }
            for ($b = -1; $b -ge -6; $b--) {
                $t2.Borders.Item($b).LineStyle = 7 # wdLineStyleDouble
                $t2.Borders.Item($b).LineWidth = 12 # 1.5 pt
                $t2.Borders.Item($b).Color = 0xED7D31
            }
            $doc.Paragraphs.Add().Range.InsertParagraphAfter()

            # Table 3: Dashed Borders (dashed / sz=12)
            $p3 = $doc.Paragraphs.Add()
            $p3.Range.Text = "Table 3: Dashed & Dotted Borders"
            $p3.Range.Font.Bold = 1
            $p3.Range.Font.Size = 12
            $t3 = $doc.Tables.Add($p3.Range, 3, 3)
            for ($r = 1; $r -le 3; $r++) {
                for ($c = 1; $c -le 3; $c++) {
                    $t3.Cell($r, $c).Range.Text = "T3 [$r,$c]"
                }
            }
            for ($b = -1; $b -ge -4; $b--) {
                $t3.Borders.Item($b).LineStyle = 4 # wdLineStyleDashLargeGap
                $t3.Borders.Item($b).LineWidth = 12
                $t3.Borders.Item($b).Color = 0x70AD47
            }
            $t3.Borders.Item(-5).LineStyle = 2 # wdLineStyleDot (inside horizontal)
            $t3.Borders.Item(-6).LineStyle = 2 # wdLineStyleDot (inside vertical)
            $doc.Paragraphs.Add().Range.InsertParagraphAfter()

            # Table 4: 3D Emboss & Inset/Outset Borders
            $p4 = $doc.Paragraphs.Add()
            $p4.Range.Text = "Table 4: 3D Emboss / Engrave Borders"
            $p4.Range.Font.Bold = 1
            $p4.Range.Font.Size = 12
            $t4 = $doc.Tables.Add($p4.Range, 3, 3)
            for ($r = 1; $r -le 3; $r++) {
                for ($c = 1; $c -le 3; $c++) {
                    $t4.Cell($r, $c).Range.Text = "T4 3D [$r,$c]"
                }
            }
            for ($b = -1; $b -ge -6; $b--) {
                $t4.Borders.Item($b).LineStyle = 21 # wdLineStyleEmboss3D
                $t4.Borders.Item($b).LineWidth = 18 # 2.25 pt
            }
            $doc.Paragraphs.Add().Range.InsertParagraphAfter()

            # Table 5: Shading & Theme Accent Colors
            $p5 = $doc.Paragraphs.Add()
            $p5.Range.Text = "Table 5: Cell Shading with Theme Colors"
            $p5.Range.Font.Bold = 1
            $p5.Range.Font.Size = 12
            $t5 = $doc.Tables.Add($p5.Range, 2, 4)
            $shades = @(0x4472C4, 0xED7D31, 0xA5A5A5, 0xFFC000)
            for ($c = 1; $c -le 4; $c++) {
                $t5.Cell(1, $c).Range.Text = "Accent $c"
                $t5.Cell(1, $c).Range.Font.Color.RGB = 0xFFFFFF
                $t5.Cell(1, $c).Shading.BackgroundPatternColor = $shades[$c-1]
                $t5.Cell(2, $c).Range.Text = "Data $c"
            }

            if (Test-Path $tableDocPath) { Remove-Item $tableDocPath -Force }
            $doc.SaveAs2($tableDocPath, 16)
            $doc.Close(0)
            $generatedFiles += $tableDocPath
            Write-Host "Table Borders Suite generated: $tableDocPath" -ForegroundColor Green
        } catch {
            Write-Warning "Failed to generate Table Borders Suite: $_"
            if ($null -ne $doc) { try { $doc.Close(0) } catch {} }
        }

        # =========================================================================
        # PART 4: OMML MATHEMATICAL EQUATIONS TEST SUITE
        # =========================================================================
        Write-Host "`n[5/5] Generating Canonical OMML Mathematical Suite..." -ForegroundColor Yellow
        $mathDocPath = Join-Path $suitesDir "math_omml_suite.docx"
        $doc = $word.Documents.Add()

        try {
            $p = $doc.Paragraphs.Add()
            $p.Range.Text = "GenOffice Canonical OMML Math Equations Test Suite"
            $p.Range.Font.Bold = 1
            $p.Range.Font.Size = 18
            $p.Range.InsertParagraphAfter()

            $sqrt = [char]0x221A
            $integral = [char]0x222B
            $sum = [char]0x2211
            $pi = [char]0x03C0
            $infty = [char]0x221E
            $pm = [char]0x00B1
            $sigma = [char]0x03C3
            $mu = [char]0x03BC

            $equations = @(
                @{ Name = "Quadratic Formula"; Expr = "x = (-b " + $pm + " " + $sqrt + "(b^2 - 4ac))/(2a)" },
                @{ Name = "Einstein Mass-Energy Equivalence"; Expr = 'E = mc^2' },
                @{ Name = "Pythagorean Theorem"; Expr = 'a^2 + b^2 = c^2' },
                @{ Name = "Definite Integral (Fundamental Theorem)"; Expr = $integral + '_a^b f(x)dx = F(b) - F(a)' },
                @{ Name = "Infinite Series (Basel Problem)"; Expr = $sum + '_(n=1)^' + $infty + ' 1/n^2 = ' + $pi + '^2/6' },
                @{ Name = "Trigonometric Limit"; Expr = '\lim_(x\to 0) (sin x)/x = 1' },
                @{ Name = "Combinatorics (Binomial Coefficient)"; Expr = 'C_n^k = (n!)/(k!(n-k)!)' },
                @{ Name = "Continued Fraction"; Expr = 'x = 1 + 1/(1 + 1/(1 + 1/2))' },
                @{ Name = "2x2 Matrix"; Expr = '[\matrix(a&b@c&d)]' },
                @{ Name = "3x3 Identity Matrix"; Expr = '[\matrix(1&0&0@0&1&0@0&0&1)]' },
                @{ Name = "Euler Identity"; Expr = 'e^(i' + $pi + ') + 1 = 0' },
                @{ Name = "Gaussian Normal Distribution"; Expr = 'f(x) = 1/(' + $sigma + $sqrt + '(2' + $pi + ')) e^(-1/2 ((x-' + $mu + ')/' + $sigma + ')^2)' }
            )

            foreach ($eq in $equations) {
                $pLabel = $doc.Paragraphs.Add()
                $pLabel.Range.Text = "Formula: " + $eq.Name
                $pLabel.Range.Font.Bold = 1
                $pLabel.Range.Font.Size = 12

                $pEq = $doc.Paragraphs.Add()
                $pEq.Range.Text = $eq.Expr
                $mathRange = $doc.OMaths.Add($pEq.Range)
                $doc.OMaths.Item($doc.OMaths.Count).BuildUp()
            }

            if (Test-Path $mathDocPath) { Remove-Item $mathDocPath -Force }
            $doc.SaveAs2($mathDocPath, 16)
            $doc.Close(0)
            $generatedFiles += $mathDocPath
            Write-Host "OMML Math Suite generated: $mathDocPath" -ForegroundColor Green
        } catch {
            Write-Warning "Failed to generate OMML Math Suite: $_"
            if ($null -ne $doc) { try { $doc.Close(0) } catch {} }
        }
    }

    # =========================================================================
    # PART 5: REAL-WORLD APPLICATION SCENARIOS (TIER 4)
    # =========================================================================
    if (-not $SkipScenarios) {
        Write-Host "`nGenerating 5 Real-World Tier 4 Application Scenarios..." -ForegroundColor Yellow

        # Scenario 1: Business Plan (Cycle SmartArt + Financial Tables with Double Borders + ROI formula)
        try {
            $sc1Path = Join-Path $scenariosDir "scenario_1_business_plan.docx"
            $doc = $word.Documents.Add()
            $p = $doc.Paragraphs.Add()
            $p.Range.Text = "BUSINESS PLAN & FINANCIAL FORECAST 2026"
            $p.Range.Font.Bold = 1
            $p.Range.Font.Size = 18
            $p.Range.InsertParagraphAfter()

            # Insert Cycle SmartArt
            $cycleLayout = $null
            for ($k = 1; $k -le $layouts.Count; $k++) {
                if ($layouts.Item($k).Name -eq "Basic Cycle") { $cycleLayout = $layouts.Item($k); break }
            }
            if ($null -ne $cycleLayout) {
                $sh = $doc.Shapes.AddSmartArt($cycleLayout, 50, 70, 480, 260)
                Set-SmartArtNodeCount $sh.SmartArt 4 "Plan Q"
            }

            # Insert Financial Table with Double Borders
            $pTable = $doc.Paragraphs.Add()
            $pTable.Range.Text = "`n`n`n`n`n`n`n`n`n`nFinancial Cash Flow & Revenue Forecast Table:"
            $pTable.Range.Font.Bold = 1
            $tbl = $doc.Tables.Add($pTable.Range, 4, 4)
            $headers = @("Quarter", "Revenue ($M)", "Cost ($M)", "Profit ($M)")
            for ($c = 1; $c -le 4; $c++) {
                $tbl.Cell(1, $c).Range.Text = $headers[$c-1]
                $tbl.Cell(1, $c).Shading.BackgroundPatternColor = 0x4472C4
                $tbl.Cell(1, $c).Range.Font.Color.RGB = 0xFFFFFF
            }
            $tbl.Cell(2, 1).Range.Text = "Q1 2026"; $tbl.Cell(2, 2).Range.Text = "12.5"; $tbl.Cell(2, 3).Range.Text = "8.2"; $tbl.Cell(2, 4).Range.Text = "4.3"
            $tbl.Cell(3, 1).Range.Text = "Q2 2026"; $tbl.Cell(3, 2).Range.Text = "15.8"; $tbl.Cell(3, 3).Range.Text = "9.5"; $tbl.Cell(3, 4).Range.Text = "6.3"
            $tbl.Cell(4, 1).Range.Text = "Total Year"; $tbl.Cell(4, 2).Range.Text = "28.3"; $tbl.Cell(4, 3).Range.Text = "17.7"; $tbl.Cell(4, 4).Range.Text = "10.6"
            for ($b = -1; $b -ge -6; $b--) {
                $tbl.Borders.Item($b).LineStyle = 7 # Double border
            }

            # Insert Math ROI formula
            $pRoi = $doc.Paragraphs.Add()
            $pRoi.Range.Text = "Return on Investment (ROI) Metric Formula:"
            $pRoi.Range.Font.Bold = 1
            $pMath = $doc.Paragraphs.Add()
            $pMath.Range.Text = "ROI = (Net Profit)/(Total Investment Cost) \times 100%"
            $doc.OMaths.Add($pMath.Range) | Out-Null
            $doc.OMaths.Item($doc.OMaths.Count).BuildUp()

            if (Test-Path $sc1Path) { Remove-Item $sc1Path -Force }
            $doc.SaveAs2($sc1Path, 16)
            $doc.Close(0)
            $generatedFiles += $sc1Path
            Write-Host "  Scenario 1 generated: $sc1Path" -ForegroundColor Green
        } catch {
            Write-Warning "Scenario 1 generation error: $_"
            if ($null -ne $doc) { try { $doc.Close(0) } catch {} }
        }

        # Scenario 2: Org Chart Hierarchy
        try {
            $sc2Path = Join-Path $scenariosDir "scenario_2_org_chart.docx"
            $doc = $word.Documents.Add()
            $p = $doc.Paragraphs.Add()
            $p.Range.Text = "ENTERPRISE GROUP CORPORATE ORGANIZATION CHART"
            $p.Range.Font.Bold = 1
            $p.Range.Font.Size = 18

            $orgLayout = $null
            for ($k = 1; $k -le $layouts.Count; $k++) {
                if ($layouts.Item($k).Name -eq "Organization Chart") { $orgLayout = $layouts.Item($k); break }
            }
            if ($null -ne $orgLayout) {
                $sh = $doc.Shapes.AddSmartArt($orgLayout, 50, 70, 500, 300)
                Set-SmartArtNodeCount $sh.SmartArt 5 "Division"
            }

            if (Test-Path $sc2Path) { Remove-Item $sc2Path -Force }
            $doc.SaveAs2($sc2Path, 16)
            $doc.Close(0)
            $generatedFiles += $sc2Path
            Write-Host "  Scenario 2 generated: $sc2Path" -ForegroundColor Green
        } catch {
            Write-Warning "Scenario 2 generation error: $_"
            if ($null -ne $doc) { try { $doc.Close(0) } catch {} }
        }

        # Scenario 3: ISO Process Pipeline
        try {
            $sc3Path = Join-Path $scenariosDir "scenario_3_iso_process.docx"
            $doc = $word.Documents.Add()
            $p = $doc.Paragraphs.Add()
            $p.Range.Text = "ISO 9001:2015 QUALITY ASSURANCE PIPELINE PROCESS"
            $p.Range.Font.Bold = 1
            $p.Range.Font.Size = 18

            $procLayout = $null
            for ($k = 1; $k -le $layouts.Count; $k++) {
                if ($layouts.Item($k).Name -eq "Basic Chevron Process") { $procLayout = $layouts.Item($k); break }
            }
            if ($null -ne $procLayout) {
                $sh = $doc.Shapes.AddSmartArt($procLayout, 50, 70, 500, 260)
                Set-SmartArtNodeCount $sh.SmartArt 5 "ISO Stage"
            }

            if (Test-Path $sc3Path) { Remove-Item $sc3Path -Force }
            $doc.SaveAs2($sc3Path, 16)
            $doc.Close(0)
            $generatedFiles += $sc3Path
            Write-Host "  Scenario 3 generated: $sc3Path" -ForegroundColor Green
        } catch {
            Write-Warning "Scenario 3 generation error: $_"
            if ($null -ne $doc) { try { $doc.Close(0) } catch {} }
        }

        # Scenario 4: STEM Academic Paper (Math formulas, fractions, radicals, matrices, tables)
        try {
            $sc4Path = Join-Path $scenariosDir "scenario_4_stem_paper.docx"
            $doc = $word.Documents.Add()
            $p = $doc.Paragraphs.Add()
            $p.Range.Text = "RESEARCH PAPER: NUMERICAL METHODS AND DYNAMICAL SYSTEMS"
            $p.Range.Font.Bold = 1
            $p.Range.Font.Size = 18

            $sqrt = [char]0x221A
            $integral = [char]0x222B
            $infty = [char]0x221E
            $pi = [char]0x03C0

            $pM1 = $doc.Paragraphs.Add()
            $pM1.Range.Text = $integral + '_(-' + $infty + ')^' + $infty + ' e^(-x^2) dx = ' + $sqrt + '(' + $pi + ')'
            $doc.OMaths.Add($pM1.Range) | Out-Null
            $doc.OMaths.Item($doc.OMaths.Count).BuildUp()

            $pM2 = $doc.Paragraphs.Add()
            $pM2.Range.Text = 'J = [\matrix(\partial f_1/\partial x_1 & \partial f_1/\partial x_2 @ \partial f_2/\partial x_1 & \partial f_2/\partial x_2)]'
            $doc.OMaths.Add($pM2.Range) | Out-Null
            $doc.OMaths.Item($doc.OMaths.Count).BuildUp()

            if (Test-Path $sc4Path) { Remove-Item $sc4Path -Force }
            $doc.SaveAs2($sc4Path, 16)
            $doc.Close(0)
            $generatedFiles += $sc4Path
            Write-Host "  Scenario 4 generated: $sc4Path" -ForegroundColor Green
        } catch {
            Write-Warning "Scenario 4 generation error: $_"
            if ($null -ne $doc) { try { $doc.Close(0) } catch {} }
        }

        # Scenario 5: Market Matrix & Pyramid Analysis
        try {
            $sc5Path = Join-Path $scenariosDir "scenario_5_market_matrix.docx"
            $doc = $word.Documents.Add()
            $p = $doc.Paragraphs.Add()
            $p.Range.Text = "MARKET MATRIX POSITIONING & TARGET SEGMENTATION"
            $p.Range.Font.Bold = 1
            $p.Range.Font.Size = 18

            $matrixLayout = $null
            for ($k = 1; $k -le $layouts.Count; $k++) {
                if ($layouts.Item($k).Name -eq "Basic Matrix") { $matrixLayout = $layouts.Item($k); break }
            }
            if ($null -ne $matrixLayout) {
                $sh = $doc.Shapes.AddSmartArt($matrixLayout, 50, 70, 480, 280)
                Set-SmartArtNodeCount $sh.SmartArt 4 "Quadrant"
            }

            if (Test-Path $sc5Path) { Remove-Item $sc5Path -Force }
            $doc.SaveAs2($sc5Path, 16)
            $doc.Close(0)
            $generatedFiles += $sc5Path
            Write-Host "  Scenario 5 generated: $sc5Path" -ForegroundColor Green
        } catch {
            Write-Warning "Scenario 5 generation error: $_"
            if ($null -ne $doc) { try { $doc.Close(0) } catch {} }
        }
    }

    $swTotal.Stop()
    Write-Host "`n=================================================================" -ForegroundColor Cyan
    Write-Host " GENERATION COMPLETE!                                            " -ForegroundColor Green
    Write-Host " Total Canonical Files Generated: $($generatedFiles.Count)       " -ForegroundColor Green
    Write-Host " Total Elapsed Time: $($swTotal.Elapsed.ToString('mm\:ss'))      " -ForegroundColor Green
    Write-Host "=================================================================" -ForegroundColor Cyan

} finally {
    # Proper cleanup
    try { $baseDoc.Close(0) } catch {}
    try { $word.Quit() } catch {}
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
}
