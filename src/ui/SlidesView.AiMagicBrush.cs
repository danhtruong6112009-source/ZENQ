using System;
using System.Drawing;
using System.Windows.Forms;
using ZenQOffice.Core;

namespace ZenQOffice.UI
{
    public partial class SlidesView
    {
        public AiMagicBrushManager MagicBrush { get; } = new AiMagicBrushManager();

        public void ToggleMagicBrushMode(MagicBrushType? initialBrush = null)
        {
            if (initialBrush.HasValue)
            {
                MagicBrush.ActiveBrush = initialBrush.Value;
                MagicBrush.IsActive = true;
            }
            else
            {
                MagicBrush.IsActive = !MagicBrush.IsActive;
            }

            if (MagicBrush.IsActive)
            {
                EndInlineEditing();
                SelectedShapeIndex = -1;
                SelectedShapeIndices?.Clear();
                IsRubberbandSelecting = false;
                IsDrawingActive = false;
                IsDrawingShapeMode = false;
            }
            else
            {
                MagicBrush.ClearStrokes();
            }

            if (InvalidateCallback != null) InvalidateCallback();
        }

        public void DrawMagicBrushOverlay(D2DGraphics g, int slideX, int slideY, float scale, int canvasX, int contentY, int canvasW, int contentH)
        {
            if (MagicBrush != null && MagicBrush.IsActive)
            {
                MagicBrush.Draw(g, slideX, slideY, scale, canvasX, contentY, canvasW, contentH, this);
            }
        }

        public bool HandleMagicBrushMouseDown(Point mousePos, int slideX, int slideY, float scale, int canvasX, int contentY, int canvasW, int contentH, MouseButtons buttons, Control parentControl)
        {
            if (MagicBrush != null && MagicBrush.IsActive)
            {
                return MagicBrush.HandleMouseDown(mousePos, slideX, slideY, scale, canvasX, contentY, canvasW, contentH, buttons, this, parentControl);
            }
            return false;
        }

        public bool HandleMagicBrushMouseMove(Point mousePos, int slideX, int slideY, float scale, int canvasX, int contentY, int canvasW, int contentH, Control parentControl)
        {
            if (MagicBrush != null && MagicBrush.IsActive)
            {
                return MagicBrush.HandleMouseMove(mousePos, slideX, slideY, scale, canvasX, contentY, canvasW, contentH, parentControl);
            }
            return false;
        }

        public bool HandleMagicBrushMouseUp(Point mousePos, int slideX, int slideY, float scale, int canvasX, int contentY, int canvasW, int contentH, Control parentControl)
        {
            if (MagicBrush != null && MagicBrush.IsActive)
            {
                return MagicBrush.HandleMouseUp(mousePos, slideX, slideY, scale, canvasX, contentY, canvasW, contentH, this, parentControl);
            }
            return false;
        }
    }
}
