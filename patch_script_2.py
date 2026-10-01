import os

file_path = r"src\format\FormatHandlers.cs"

with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Fix 1: Precise Twip Conversion (Avoid integer truncation line jumping)
content = content.replace("pgWTwips / 20;", "(int)(pgWTwips / 20f);")
content = content.replace("pgHTwips / 20;", "(int)(pgHTwips / 20f);")
content = content.replace("topTwips / 20;", "(int)(topTwips / 20f);")
content = content.replace("rightTwips / 20;", "(int)(rightTwips / 20f);")
content = content.replace("bottomTwips / 20;", "(int)(bottomTwips / 20f);")
content = content.replace("leftTwips / 20;", "(int)(leftTwips / 20f);")
content = content.replace("int.Parse(sz.Value) / 2;", "(int)(float.Parse(sz.Value) / 2f);")
content = content.replace("int.Parse(space.Value) / 20;", "(int)(float.Parse(space.Value) / 20f);")

# Fix 2: Upgrade TryParseShapeMetadata
old_func = """
        private static bool TryParseShapeMetadata(XmlNode drawingNode, XmlNamespaceManager nsmgr, out string shapeType, out string textContent, out float adj1, out float adj2, out float adj3)
        {
            shapeType = "Rectangle";
            textContent = "";
            adj1 = 0.2f; adj2 = 0.4f; adj3 = 0.8f;
            if (drawingNode == null) return false;

            XmlNode docPr = drawingNode.SelectSingleNode(".//wp:docPr", nsmgr) ?? drawingNode.SelectSingleNode(".//*[local-name()='docPr']");
            if (docPr != null && docPr.Attributes != null && docPr.Attributes["descr"] != null && docPr.Attributes["descr"].Value.StartsWith("Shape|"))
            {
                string descr = docPr.Attributes["descr"].Value;
                string[] parts = descr.Split('|');
                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) shapeType = parts[1];
                if (parts.Length > 2) textContent = parts[2];
                if (parts.Length > 3) float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out adj1);
                if (parts.Length > 4) float.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out adj2);
                if (parts.Length > 5) float.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out adj3);
                return true;
            }
            return false;
        }
"""

new_func = """
        private static bool TryParseShapeMetadata(XmlNode drawingNode, XmlNamespaceManager nsmgr, out string shapeType, out string textContent, out float adj1, out float adj2, out float adj3)
        {
            shapeType = "Rectangle";
            textContent = "";
            adj1 = 0.2f; adj2 = 0.4f; adj3 = 0.8f;
            if (drawingNode == null) return false;

            bool found = false;

            // 1. Try Legacy docPr descr mode
            XmlNode docPr = drawingNode.SelectSingleNode(".//wp:docPr", nsmgr) ?? drawingNode.SelectSingleNode(".//*[local-name()='docPr']");
            if (docPr != null && docPr.Attributes != null && docPr.Attributes["descr"] != null && docPr.Attributes["descr"].Value.StartsWith("Shape|"))
            {
                string descr = docPr.Attributes["descr"].Value;
                string[] parts = descr.Split('|');
                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) shapeType = parts[1];
                if (parts.Length > 2) textContent = parts[2];
                if (parts.Length > 3) float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out adj1);
                if (parts.Length > 4) float.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out adj2);
                if (parts.Length > 5) float.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out adj3);
                return true;
            }

            // 2. Map standard OpenXML prstGeom
            XmlNode prstGeom = drawingNode.SelectSingleNode(".//*[local-name()='prstGeom']");
            if (prstGeom != null && prstGeom.Attributes != null && prstGeom.Attributes["prst"] != null)
            {
                string prst = prstGeom.Attributes["prst"].Value;
                shapeType = MapOpenXmlGeomToShapeRenderer(prst);
                found = true;
            }

            // 3. Extract text content if present
            XmlNode tNode = drawingNode.SelectSingleNode(".//*[local-name()='t']");
            if (tNode != null)
            {
                textContent = tNode.InnerText;
                found = true;
            }

            return found;
        }

        private static string MapOpenXmlGeomToShapeRenderer(string prst)
        {
            switch (prst)
            {
                case "rect": return "Rectangle";
                case "roundRect": return "RoundedRectangle";
                case "ellipse": return "Oval";
                case "triangle": return "Triangle";
                case "rtTriangle": return "RightTriangle";
                case "parallelogram": return "Parallelogram";
                case "trapezoid": return "Trapezoid";
                case "diamond": return "Diamond";
                case "pentagon": return "Pentagon";
                case "hexagon": return "Hexagon";
                case "octagon": return "Octagon";
                case "star5": return "Star5";
                case "star4": return "Star4";
                case "smileyFace": return "SmileFace";
                case "heart": return "Heart";
                case "sun": return "Sun";
                case "moon": return "Moon";
                case "lightningBolt": return "Lightning";
                case "can": return "Cylinder";
                case "cube": return "Cube";
                case "donut": return "Donut";
                case "blockArc": return "Arc";
                case "noSmoking": return "NotEqual";
                case "mathPlus": return "Plus";
                case "mathMinus": return "Minus";
                case "mathMultiply": return "Multiply";
                case "mathDivide": return "Divide";
                case "mathEqual": return "Equal";
                case "leftArrow": return "LeftArrow";
                case "rightArrow": return "RightArrow";
                case "upArrow": return "UpArrow";
                case "downArrow": return "DownArrow";
                case "leftRightArrow": return "LeftRightArrow";
                case "upDownArrow": return "UpDownArrow";
                case "quadArrow": return "QuadArrow";
                case "bentArrow": return "BentArrow";
                case "snip1Rect": return "SingleCornerSnipped";
                case "snip2SameRect": return "TopCornersSnipped";
                case "snip2DiagRect": return "SnippedAndRounded";
                case "round1Rect": return "RoundSingleCorner";
                case "round2SameRect": return "RoundSameSideCorner";
                case "round2DiagRect": return "RoundDiagonalCorner";
                default: return "Rectangle";
            }
        }
"""

if old_func.strip() in content.strip():
    content = content.replace(old_func.strip(), new_func.strip())
    print("Function replaced successfully!")
else:
    print("Could not find the function to replace. Attempting fallback replacement.")
    # Fallback: Just insert new_func before the end of the class
    idx = content.rfind("}")
    idx = content.rfind("}", 0, idx)
    content = content[:idx] + new_func + content[idx:]

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
print("File FormatHandlers.cs patched successfully.")
