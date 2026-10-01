using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GenOffice.UI
{
    public static class VectorIconPresets
    {
        public static GraphicsPath CreateIconPath(string iconId, float w, float h)
        {
            GraphicsPath path = new GraphicsPath();
            float cx = w / 2f;
            float cy = h / 2f;

            switch (iconId)
            {
                case "Target":
                    // Vòng tròn mục tiêu tâm ngắm
                    path.AddEllipse(w * 0.05f, h * 0.05f, w * 0.9f, h * 0.9f);
                    path.AddEllipse(w * 0.25f, h * 0.25f, w * 0.5f, h * 0.5f);
                    path.AddEllipse(w * 0.40f, h * 0.40f, w * 0.2f, h * 0.2f);
                    break;

                case "Rocket":
                    // Tên lửa vút bay
                    path.AddLines(new PointF[] {
                        new PointF(cx, h * 0.05f),
                        new PointF(w * 0.75f, h * 0.35f),
                        new PointF(w * 0.75f, h * 0.70f),
                        new PointF(w * 0.95f, h * 0.85f),
                        new PointF(w * 0.70f, h * 0.85f),
                        new PointF(w * 0.60f, h * 0.95f),
                        new PointF(w * 0.40f, h * 0.95f),
                        new PointF(w * 0.30f, h * 0.85f),
                        new PointF(w * 0.05f, h * 0.85f),
                        new PointF(w * 0.25f, h * 0.70f),
                        new PointF(w * 0.25f, h * 0.35f)
                    });
                    path.CloseFigure();
                    break;

                case "Lightbulb":
                    // Bóng đèn ý tưởng
                    path.AddArc(w * 0.2f, h * 0.05f, w * 0.6f, h * 0.55f, 135, 270);
                    path.AddLine(w * 0.65f, h * 0.68f, w * 0.62f, h * 0.82f);
                    path.AddLine(w * 0.62f, h * 0.82f, w * 0.38f, h * 0.82f);
                    path.AddLine(w * 0.38f, h * 0.82f, w * 0.35f, h * 0.68f);
                    path.CloseFigure();
                    path.AddRectangle(new RectangleF(w * 0.42f, h * 0.85f, w * 0.16f, h * 0.08f));
                    break;

                case "Chart":
                    // Biểu đồ tăng trưởng
                    path.AddRectangle(new RectangleF(w * 0.10f, h * 0.55f, w * 0.16f, h * 0.35f));
                    path.AddRectangle(new RectangleF(w * 0.32f, h * 0.35f, w * 0.16f, h * 0.55f));
                    path.AddRectangle(new RectangleF(w * 0.54f, h * 0.20f, w * 0.16f, h * 0.70f));
                    path.AddRectangle(new RectangleF(w * 0.76f, h * 0.08f, w * 0.16f, h * 0.82f));
                    break;

                case "ShoppingCart":
                    // Giỏ hàng
                    path.AddLines(new PointF[] {
                        new PointF(w * 0.05f, h * 0.15f),
                        new PointF(w * 0.22f, h * 0.15f),
                        new PointF(w * 0.35f, h * 0.65f),
                        new PointF(w * 0.85f, h * 0.65f),
                        new PointF(w * 0.95f, h * 0.30f),
                        new PointF(w * 0.26f, h * 0.30f)
                    });
                    path.AddEllipse(w * 0.35f, h * 0.75f, w * 0.15f, h * 0.15f);
                    path.AddEllipse(w * 0.72f, h * 0.75f, w * 0.15f, h * 0.15f);
                    break;

                case "Laptop":
                    // Màn hình máy tính
                    path.AddRectangle(new RectangleF(w * 0.15f, h * 0.15f, w * 0.70f, h * 0.55f));
                    path.AddLines(new PointF[] {
                        new PointF(w * 0.08f, h * 0.75f),
                        new PointF(w * 0.92f, h * 0.75f),
                        new PointF(w * 0.96f, h * 0.85f),
                        new PointF(w * 0.04f, h * 0.85f)
                    });
                    path.CloseFigure();
                    break;

                case "User":
                    // Người dùng
                    path.AddEllipse(w * 0.32f, h * 0.08f, w * 0.36f, h * 0.36f);
                    path.AddArc(w * 0.15f, h * 0.48f, w * 0.70f, h * 0.46f, 180, 180);
                    path.CloseFigure();
                    break;

                case "Trophy":
                    // Cúp vô địch
                    path.AddLines(new PointF[] {
                        new PointF(w * 0.25f, h * 0.10f),
                        new PointF(w * 0.75f, h * 0.10f),
                        new PointF(w * 0.68f, h * 0.52f),
                        new PointF(w * 0.56f, h * 0.65f),
                        new PointF(w * 0.56f, h * 0.78f),
                        new PointF(w * 0.70f, h * 0.88f),
                        new PointF(w * 0.30f, h * 0.88f),
                        new PointF(w * 0.44f, h * 0.78f),
                        new PointF(w * 0.44f, h * 0.65f),
                        new PointF(w * 0.32f, h * 0.52f)
                    });
                    path.CloseFigure();
                    break;

                case "Bell":
                    // Chuông thông báo
                    path.AddArc(w * 0.22f, h * 0.15f, w * 0.56f, h * 0.65f, 180, 180);
                    path.AddLines(new PointF[] {
                        new PointF(w * 0.78f, h * 0.48f),
                        new PointF(w * 0.88f, h * 0.75f),
                        new PointF(w * 0.12f, h * 0.75f),
                        new PointF(w * 0.22f, h * 0.48f)
                    });
                    path.CloseFigure();
                    path.AddArc(w * 0.42f, h * 0.75f, w * 0.16f, h * 0.16f, 0, 180);
                    break;

                case "Calendar":
                    // Lịch biểu
                    path.AddRectangle(new RectangleF(w * 0.15f, h * 0.18f, w * 0.70f, h * 0.70f));
                    path.AddRectangle(new RectangleF(w * 0.26f, h * 0.08f, w * 0.10f, h * 0.15f));
                    path.AddRectangle(new RectangleF(w * 0.64f, h * 0.08f, w * 0.10f, h * 0.15f));
                    break;

                case "Lock":
                    // Ổ khóa an toàn
                    path.AddArc(w * 0.28f, h * 0.10f, w * 0.44f, h * 0.46f, 180, 180);
                    path.AddLine(w * 0.72f, h * 0.33f, w * 0.72f, h * 0.46f);
                    path.AddLine(w * 0.28f, h * 0.46f, w * 0.28f, h * 0.33f);
                    path.AddRectangle(new RectangleF(w * 0.20f, h * 0.45f, w * 0.60f, h * 0.48f));
                    break;

                case "CheckCircle":
                    // Tích tròn hoàn thành
                    path.AddEllipse(w * 0.08f, h * 0.08f, w * 0.84f, h * 0.84f);
                    path.AddLines(new PointF[] {
                        new PointF(w * 0.28f, h * 0.50f),
                        new PointF(w * 0.44f, h * 0.68f),
                        new PointF(w * 0.72f, h * 0.32f)
                    });
                    break;

                case "StarBadge":
                    // Huy hiệu sao
                    path.AddEllipse(w * 0.12f, h * 0.12f, w * 0.76f, h * 0.76f);
                    path.AddLines(new PointF[] {
                        new PointF(w * 0.30f, h * 0.80f),
                        new PointF(w * 0.20f, h * 0.98f),
                        new PointF(w * 0.40f, h * 0.88f),
                        new PointF(w * 0.50f, h * 0.84f)
                    });
                    path.AddLines(new PointF[] {
                        new PointF(w * 0.70f, h * 0.80f),
                        new PointF(w * 0.80f, h * 0.98f),
                        new PointF(w * 0.60f, h * 0.88f),
                        new PointF(w * 0.50f, h * 0.84f)
                    });
                    break;

                case "WaveLine":
                    // Đường sóng trang trí
                    path.AddBezier(0, cy, w * 0.25f, cy - h * 0.4f, w * 0.25f, cy + h * 0.4f, w * 0.5f, cy);
                    path.AddBezier(w * 0.5f, cy, w * 0.75f, cy - h * 0.4f, w * 0.75f, cy + h * 0.4f, w, cy);
                    break;

                default:
                    path.AddEllipse(w * 0.1f, h * 0.1f, w * 0.8f, h * 0.8f);
                    break;
            }

            return path;
        }
    }
}
