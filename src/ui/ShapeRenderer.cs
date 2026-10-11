using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ZenQOffice.UI
{
    public static class ShapeRenderer
    {
        public static GraphicsPath CreateShapePath(DocShapeObject shape, Rectangle bounds)
        {
            if (shape == null) return CreateShapePath("Rectangle", bounds, 0.2f, 0.4f, 0.8f);
            string st = shape.ShapeType;
            if (shape.AspectRatioLock && (st == "Oval" || string.Equals(st, "Circle", StringComparison.OrdinalIgnoreCase)))
            {
                st = "Circle";
            }
            return CreateShapePath(st, bounds, shape.AdjustValue1, shape.AdjustValue2, shape.AdjustValue3);
        }

        public static GraphicsPath CreateShapePath(string shapeType, Rectangle bounds)
        {
            return CreateShapePath(shapeType, bounds, 0.2f, 0.4f, 0.8f);
        }

        public static GraphicsPath CreateShapePath(string shapeType, Rectangle bounds, float adj1, float adj2, float adj3)
        {
            GraphicsPath path = new GraphicsPath();
            float x = bounds.X;
            float y = bounds.Y;
            float w = Math.Max(4, bounds.Width);
            float h = Math.Max(4, bounds.Height);

            switch (shapeType ?? "Rectangle")
            {
                // Rectangles
                case "Rectangle":
                case "rect":
                case "TextBox":
                case "Image":
                case "FlowchartProcess":
                    path.AddRectangle(new RectangleF(x, y, w, h));
                    break;

                case "RoundedRectangle":
                case "roundRect":
                    {
                        float rRatio = (adj1 > 0.01f && adj1 <= 0.5f) ? adj1 : 0.2f;
                        float r = Math.Min(w, h) * rRatio;
                        path.AddArc(x, y, r * 2, r * 2, 180, 90);
                        path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
                        path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
                        path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
                        path.CloseFigure();
                    }
                    break;

                case "SingleCornerSnipped":
                    {
                        float snip = Math.Min(w, h) * 0.2f;
                        path.AddLine(x + snip, y, x + w, y);
                        path.AddLine(x + w, y, x + w, y + h);
                        path.AddLine(x + w, y + h, x, y + h);
                        path.AddLine(x, y + h, x, y + snip);
                        path.CloseFigure();
                    }
                    break;

                case "TopCornersSnipped":
                    {
                        float snip2 = Math.Min(w, h) * 0.2f;
                        path.AddLine(x + snip2, y, x + w - snip2, y);
                        path.AddLine(x + w, y + snip2, x + w, y + h);
                        path.AddLine(x + w, y + h, x, y + h);
                        path.AddLine(x, y + h, x, y + snip2);
                        path.CloseFigure();
                    }
                    break;

                case "SnippedAndRounded":
                    {
                        float snipR = Math.Min(w, h) * 0.22f;
                        path.AddLine(x + snipR, y, x + w - snipR * 2, y);
                        path.AddArc(x + w - snipR * 2, y, snipR * 2, snipR * 2, 270, 90);
                        path.AddLine(x + w, y + snipR, x + w, y + h);
                        path.AddLine(x + w, y + h, x, y + h);
                        path.AddLine(x, y + h, x, y + snipR);
                        path.CloseFigure();
                    }
                    break;

                case "RoundSingleCorner":
                    {
                        float r1 = Math.Min(w, h) * 0.28f;
                        path.AddArc(x, y, r1 * 2, r1 * 2, 180, 90);
                        path.AddLine(x + r1, y, x + w, y);
                        path.AddLine(x + w, y, x + w, y + h);
                        path.AddLine(x + w, y + h, x, y + h);
                        path.AddLine(x, y + h, x, y + r1);
                        path.CloseFigure();
                    }
                    break;

                case "RoundSameSideCorner":
                    {
                        float rS = Math.Min(w, h) * 0.25f;
                        path.AddArc(x, y, rS * 2, rS * 2, 180, 90);
                        path.AddArc(x + w - rS * 2, y, rS * 2, rS * 2, 270, 90);
                        path.AddLine(x + w, y + rS, x + w, y + h);
                        path.AddLine(x + w, y + h, x, y + h);
                        path.AddLine(x, y + h, x, y + rS);
                        path.CloseFigure();
                    }
                    break;

                case "RoundDiagonalCorner":
                    {
                        float rD = Math.Min(w, h) * 0.25f;
                        path.AddArc(x, y, rD * 2, rD * 2, 180, 90);
                        path.AddLine(x + rD, y, x + w, y);
                        path.AddLine(x + w, y, x + w, y + h - rD * 2);
                        path.AddArc(x + w - rD * 2, y + h - rD * 2, rD * 2, rD * 2, 0, 90);
                        path.AddLine(x + w - rD, y + h, x, y + h);
                        path.AddLine(x, y + h, x, y + rD);
                        path.CloseFigure();
                    }
                    break;

                // Basic Shapes
                case "Oval":
                case "ellipse":
                    path.AddEllipse(x, y, w, h);
                    break;

                case "Circle":
                    {
                        int dim = Math.Min((int)w, (int)h);
                        int nx = (int)x + ((int)w - dim) / 2;
                        int ny = (int)y + ((int)h - dim) / 2;
                        path.AddEllipse(nx, ny, dim, dim);
                    }
                    break;

                case "Triangle":
                    path.AddPolygon(new PointF[]
                    {
                        new PointF(x + w / 2f, y),
                        new PointF(x + w, y + h),
                        new PointF(x, y + h)
                    });
                    break;

                case "RightTriangle":
                    path.AddPolygon(new PointF[]
                    {
                        new PointF(x, y),
                        new PointF(x + w, y + h),
                        new PointF(x, y + h)
                    });
                    break;

                case "Parallelogram":
                case "FlowchartData":
                    {
                        float pSkew = w * 0.25f;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + pSkew, y),
                            new PointF(x + w, y),
                            new PointF(x + w - pSkew, y + h),
                            new PointF(x, y + h)
                        });
                    }
                    break;

                case "Chevron":
                    {
                        float notch = Math.Min(w * 0.22f, h * 0.45f);
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x, y),
                            new PointF(x + w - notch, y),
                            new PointF(x + w, y + h / 2f),
                            new PointF(x + w - notch, y + h),
                            new PointF(x, y + h),
                            new PointF(x + notch, y + h / 2f)
                        });
                    }
                    break;

                case "Trapezoid":
                    {
                        float tSkew = w * 0.2f;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + tSkew, y),
                            new PointF(x + w - tSkew, y),
                            new PointF(x + w, y + h),
                            new PointF(x, y + h)
                        });
                    }
                    break;

                case "Diamond":
                case "FlowchartDecision":
                    path.AddPolygon(new PointF[]
                    {
                        new PointF(x + w / 2f, y),
                        new PointF(x + w, y + h / 2f),
                        new PointF(x + w / 2f, y + h),
                        new PointF(x, y + h / 2f)
                    });
                    break;

                case "Pentagon":
                    path.AddPolygon(new PointF[]
                    {
                        new PointF(x + w / 2f, y),
                        new PointF(x + w, y + h * 0.38f),
                        new PointF(x + w * 0.81f, y + h),
                        new PointF(x + w * 0.19f, y + h),
                        new PointF(x, y + h * 0.38f)
                    });
                    break;

                case "Hexagon":
                case "FlowchartPreparation":
                    path.AddPolygon(new PointF[]
                    {
                        new PointF(x + w * 0.25f, y),
                        new PointF(x + w * 0.75f, y),
                        new PointF(x + w, y + h / 2f),
                        new PointF(x + w * 0.75f, y + h),
                        new PointF(x + w * 0.25f, y + h),
                        new PointF(x, y + h / 2f)
                    });
                    break;

                case "Octagon":
                    {
                        float oCut = Math.Min(w, h) * 0.28f;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + oCut, y),
                            new PointF(x + w - oCut, y),
                            new PointF(x + w, y + oCut),
                            new PointF(x + w, y + h - oCut),
                            new PointF(x + w - oCut, y + h),
                            new PointF(x + oCut, y + h),
                            new PointF(x, y + h - oCut),
                            new PointF(x, y + oCut)
                        });
                    }
                    break;

                case "Decagon":
                    {
                        PointF[] decaPts = new PointF[10];
                        float cx = x + w / 2f;
                        float cy = y + h / 2f;
                        float rx = w / 2f;
                        float ry = h / 2f;
                        for (int di = 0; di < 10; di++)
                        {
                            double ang = -Math.PI / 2.0 + di * (2.0 * Math.PI / 10.0);
                            decaPts[di] = new PointF((float)(cx + rx * Math.Cos(ang)), (float)(cy + ry * Math.Sin(ang)));
                        }
                        path.AddPolygon(decaPts);
                    }
                    break;

                case "Cross":
                case "Plus":
                    {
                        float cW = w / 3f; float cH = h / 3f;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + cW, y), new PointF(x + 2 * cW, y),
                            new PointF(x + 2 * cW, y + cH), new PointF(x + w, y + cH),
                            new PointF(x + w, y + 2 * cH), new PointF(x + 2 * cW, y + 2 * cH),
                            new PointF(x + 2 * cW, y + h), new PointF(x + cW, y + h),
                            new PointF(x + cW, y + 2 * cH), new PointF(x, y + 2 * cH),
                            new PointF(x, y + cH), new PointF(x + cW, y + cH)
                        });
                    }
                    break;

                case "Minus":
                    path.AddRectangle(new RectangleF(x, y + h * 0.38f, w, h * 0.24f));
                    break;

                case "Multiply":
                    {
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + w * 0.2f, y), new PointF(x + w / 2f, y + h * 0.3f), new PointF(x + w * 0.8f, y),
                            new PointF(x + w, y + h * 0.2f), new PointF(x + w * 0.7f, y + h / 2f), new PointF(x + w, y + h * 0.8f),
                            new PointF(x + w * 0.8f, y + h), new PointF(x + w / 2f, y + h * 0.7f), new PointF(x + w * 0.2f, y + h),
                            new PointF(x, y + h * 0.8f), new PointF(x + w * 0.3f, y + h / 2f), new PointF(x, y + h * 0.2f)
                        });
                    }
                    break;

                case "Divide":
                    path.AddRectangle(new RectangleF(x, y + h * 0.42f, w, h * 0.16f));
                    path.AddEllipse(x + w / 2f - h * 0.1f, y + h * 0.15f, h * 0.2f, h * 0.2f);
                    path.AddEllipse(x + w / 2f - h * 0.1f, y + h * 0.65f, h * 0.2f, h * 0.2f);
                    break;

                case "Equal":
                    path.AddRectangle(new RectangleF(x, y + h * 0.22f, w, h * 0.2f));
                    path.AddRectangle(new RectangleF(x, y + h * 0.58f, w, h * 0.2f));
                    break;

                case "NotEqual":
                    path.AddRectangle(new RectangleF(x, y + h * 0.25f, w, h * 0.18f));
                    path.AddRectangle(new RectangleF(x, y + h * 0.57f, w, h * 0.18f));
                    path.AddPolygon(new PointF[]
                    {
                        new PointF(x + w * 0.65f, y),
                        new PointF(x + w * 0.75f, y),
                        new PointF(x + w * 0.35f, y + h),
                        new PointF(x + w * 0.25f, y + h)
                    });
                    break;

                case "Heart":
                    path.AddBezier(x + w / 2f, y + h * 0.3f, x + w / 2f, y, x, y, x, y + h * 0.35f);
                    path.AddBezier(x, y + h * 0.35f, x, y + h * 0.65f, x + w / 2f, y + h, x + w / 2f, y + h);
                    path.AddBezier(x + w / 2f, y + h, x + w, y + h * 0.65f, x + w, y + h * 0.35f, x + w, y + h * 0.35f);
                    path.AddBezier(x + w, y + h * 0.35f, x + w, y, x + w / 2f, y, x + w / 2f, y + h * 0.3f);
                    path.CloseFigure();
                    break;

                case "SmileFace":
                    path.AddEllipse(x, y, w, h);
                    break;

                case "Lightning":
                    path.AddPolygon(new PointF[]
                    {
                        new PointF(x + w * 0.55f, y),
                        new PointF(x + w, y),
                        new PointF(x + w * 0.4f, y + h * 0.52f),
                        new PointF(x + w * 0.8f, y + h * 0.52f),
                        new PointF(x + w * 0.1f, y + h),
                        new PointF(x + w * 0.35f, y + h * 0.45f),
                        new PointF(x, y + h * 0.45f)
                    });
                    break;

                case "Sun":
                    path.AddEllipse(x + w * 0.22f, y + h * 0.22f, w * 0.56f, h * 0.56f);
                    break;

                case "Moon":
                    path.AddArc(x, y, w, h, -80, 160);
                    path.AddArc(x + w * 0.25f, y + h * 0.05f, w * 0.7f, h * 0.9f, 80, -160);
                    path.CloseFigure();
                    break;

                case "Cloud":
                    path.AddEllipse(x + w * 0.15f, y + h * 0.35f, w * 0.35f, h * 0.55f);
                    path.AddEllipse(x + w * 0.35f, y + h * 0.15f, w * 0.4f, h * 0.6f);
                    path.AddEllipse(x + w * 0.55f, y + h * 0.35f, w * 0.35f, h * 0.55f);
                    path.AddRectangle(new RectangleF(x + w * 0.25f, y + h * 0.55f, w * 0.5f, h * 0.35f));
                    break;

                case "Teardrop":
                    path.AddLine(x + w / 2f, y, x + w, y + h * 0.6f);
                    path.AddArc(x, y + h * 0.2f, w, h * 0.8f, 0, 180);
                    path.AddLine(x, y + h * 0.6f, x + w / 2f, y);
                    path.CloseFigure();
                    break;

                case "Arc":
                    path.AddArc(x, y, w, h, 200, 200);
                    break;

                case "Donut":
                    path.AddEllipse(x, y, w, h);
                    path.AddEllipse(x + w * 0.25f, y + h * 0.25f, w * 0.5f, h * 0.5f);
                    break;

                case "Cylinder":
                case "Can":
                    {
                        float ch = h * 0.2f;
                        path.AddEllipse(x, y, w, ch);
                        path.AddLine(x, y + ch / 2f, x, y + h - ch / 2f);
                        path.AddArc(x, y + h - ch, w, ch, 180, -180);
                        path.AddLine(x + w, y + h - ch / 2f, x + w, y + ch / 2f);
                        path.CloseFigure();
                    }
                    break;

                case "Cube":
                    {
                        float skew = Math.Min(w, h) * 0.25f;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x, y + skew),
                            new PointF(x + skew, y),
                            new PointF(x + w, y),
                            new PointF(x + w, y + h - skew),
                            new PointF(x + w - skew, y + h),
                            new PointF(x, y + h),
                        });
                    }
                    break;

                // Block Arrows
                case "RightArrow":
                    {
                        float neckRatio = (adj1 > 0.05f && adj1 < 0.48f) ? adj1 : 0.25f;
                        float headRatio = (adj2 > 0.1f && adj2 < 0.7f) ? adj2 : 0.4f;
                        float headX = x + w * (1.0f - headRatio);
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x, y + h * neckRatio),
                            new PointF(headX, y + h * neckRatio),
                            new PointF(headX, y),
                            new PointF(x + w, y + h / 2f),
                            new PointF(headX, y + h),
                            new PointF(headX, y + h * (1.0f - neckRatio)),
                            new PointF(x, y + h * (1.0f - neckRatio))
                        });
                    }
                    break;

                case "LeftArrow":
                    {
                        float neckRatio = (adj1 > 0.05f && adj1 < 0.48f) ? adj1 : 0.25f;
                        float headRatio = (adj2 > 0.1f && adj2 < 0.7f) ? adj2 : 0.4f;
                        float headX = x + w * headRatio;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(headX, y),
                            new PointF(headX, y + h * neckRatio),
                            new PointF(x + w, y + h * neckRatio),
                            new PointF(x + w, y + h * (1.0f - neckRatio)),
                            new PointF(headX, y + h * (1.0f - neckRatio)),
                            new PointF(headX, y + h),
                            new PointF(x, y + h / 2f)
                        });
                    }
                    break;

                case "UpArrow":
                    {
                        float neckRatio = (adj1 > 0.05f && adj1 < 0.48f) ? adj1 : 0.25f;
                        float headRatio = (adj2 > 0.1f && adj2 < 0.7f) ? adj2 : 0.4f;
                        float headY = y + h * headRatio;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + w / 2f, y),
                            new PointF(x + w, headY),
                            new PointF(x + w * (1.0f - neckRatio), headY),
                            new PointF(x + w * (1.0f - neckRatio), y + h),
                            new PointF(x + w * neckRatio, y + h),
                            new PointF(x + w * neckRatio, headY),
                            new PointF(x, headY)
                        });
                    }
                    break;

                case "DownArrow":
                    {
                        float neckRatio = (adj1 > 0.05f && adj1 < 0.48f) ? adj1 : 0.25f;
                        float headRatio = (adj2 > 0.1f && adj2 < 0.7f) ? adj2 : 0.4f;
                        float headY = y + h * (1.0f - headRatio);
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + w * neckRatio, y),
                            new PointF(x + w * (1.0f - neckRatio), y),
                            new PointF(x + w * (1.0f - neckRatio), headY),
                            new PointF(x + w, headY),
                            new PointF(x + w / 2f, y + h),
                            new PointF(x, headY),
                            new PointF(x + w * neckRatio, headY)
                        });
                    }
                    break;

                case "LeftRightArrow":
                    {
                        float hW = w * 0.25f; float nH = h * 0.25f;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x, y + h / 2f),
                            new PointF(x + hW, y),
                            new PointF(x + hW, y + nH),
                            new PointF(x + w - hW, y + nH),
                            new PointF(x + w - hW, y),
                            new PointF(x + w, y + h / 2f),
                            new PointF(x + w - hW, y + h),
                            new PointF(x + w - hW, y + h - nH),
                            new PointF(x + hW, y + h - nH),
                            new PointF(x + hW, y + h)
                        });
                    }
                    break;

                case "UpDownArrow":
                    {
                        float hH = h * 0.25f; float nW = w * 0.25f;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + w / 2f, y),
                            new PointF(x + w, y + hH),
                            new PointF(x + w - nW, y + hH),
                            new PointF(x + w - nW, y + h - hH),
                            new PointF(x + w, y + h - hH),
                            new PointF(x + w / 2f, y + h),
                            new PointF(x, y + h - hH),
                            new PointF(x + nW, y + h - hH),
                            new PointF(x + nW, y + hH),
                            new PointF(x, y + hH)
                        });
                    }
                    break;

                case "QuadArrow":
                    {
                        float aL = Math.Min(w, h) * 0.3f;
                        float aW = Math.Min(w, h) * 0.15f;
                        float cx = x + w / 2f;
                        float cy = y + h / 2f;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(cx, y), new PointF(cx + aW, y + aL), new PointF(cx + aW / 2f, y + aL),
                            new PointF(cx + aW / 2f, cy - aW / 2f), new PointF(x + w - aL, cy - aW / 2f), new PointF(x + w - aL, cy - aW),
                            new PointF(x + w, cy), new PointF(x + w - aL, cy + aW), new PointF(x + w - aL, cy + aW / 2f),
                            new PointF(cx + aW / 2f, cy + aW / 2f), new PointF(cx + aW / 2f, y + h - aL), new PointF(cx + aW, y + h - aL),
                            new PointF(cx, y + h), new PointF(cx - aW, y + h - aL), new PointF(cx - aW / 2f, y + h - aL),
                            new PointF(cx - aW / 2f, cy + aW / 2f), new PointF(x + aL, cy + aW / 2f), new PointF(x + aL, cy + aW),
                            new PointF(x, cy), new PointF(x + aL, cy - aW), new PointF(x + aL, cy - aW / 2f),
                            new PointF(cx - aW / 2f, cy - aW / 2f), new PointF(cx - aW / 2f, y + aL), new PointF(cx - aW, y + aL)
                        });
                    }
                    break;

                case "BentArrow":
                    {
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x, y + h * 0.4f),
                            new PointF(x + w * 0.45f, y + h * 0.4f),
                            new PointF(x + w * 0.45f, y + h * 0.25f),
                            new PointF(x + w, y + h * 0.25f),
                            new PointF(x + w * 0.75f, y),
                            new PointF(x + w, y + h * 0.5f),
                            new PointF(x + w * 0.75f, y + h * 0.5f),
                            new PointF(x + w * 0.45f, y + h * 0.5f),
                            new PointF(x + w * 0.45f, y + h),
                            new PointF(x, y + h)
                        });
                    }
                    break;

                case "UTurnArrow":
                    {
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + w * 0.35f, y + h),
                            new PointF(x + w * 0.35f, y + h * 0.4f),
                            new PointF(x + w * 0.65f, y + h * 0.4f),
                            new PointF(x + w * 0.65f, y + h * 0.7f),
                            new PointF(x + w * 0.55f, y + h * 0.7f),
                            new PointF(x + w * 0.75f, y + h),
                            new PointF(x + w * 0.95f, y + h * 0.7f),
                            new PointF(x + w * 0.85f, y + h * 0.7f),
                            new PointF(x + w * 0.85f, y + h * 0.3f),
                            new PointF(x + w * 0.15f, y + h * 0.3f),
                            new PointF(x + w * 0.15f, y + h)
                        });
                    }
                    break;

                // Lines & Connectors
                case "Line":
                    path.AddLine(x, y + h, x + w, y);
                    break;

                case "ArrowLine":
                case "DoubleArrowLine":
                    path.AddLine(x, y + h, x + w, y);
                    break;

                case "ElbowConnector":
                case "ElbowArrowConnector":
                    path.AddLine(x, y + h, x + w / 2f, y + h);
                    path.AddLine(x + w / 2f, y + h, x + w / 2f, y);
                    path.AddLine(x + w / 2f, y, x + w, y);
                    break;

                case "CurvedConnector":
                    path.AddBezier(x, y + h, x, y, x + w, y + h, x + w, y);
                    break;

                // Flowchart
                case "FlowchartTerminator":
                    {
                        float r = Math.Min(w / 2f, h / 2f);
                        path.AddArc(x, y, r * 2, h, 90, 180);
                        path.AddLine(x + r, y, x + w - r, y);
                        path.AddArc(x + w - r * 2, y, r * 2, h, 270, 180);
                        path.AddLine(x + w - r, y + h, x + r, y + h);
                        path.CloseFigure();
                    }
                    break;

                case "FlowchartDocument":
                    path.AddLine(x, y, x + w, y);
                    path.AddLine(x + w, y, x + w, y + h * 0.85f);
                    path.AddBezier(x + w, y + h * 0.85f, x + w * 0.75f, y + h * 0.7f, x + w * 0.5f, y + h, x + w * 0.25f, y + h * 0.85f);
                    path.AddBezier(x + w * 0.25f, y + h * 0.85f, x + w * 0.1f, y + h * 0.7f, x, y + h * 0.9f, x, y + h * 0.85f);
                    path.AddLine(x, y + h * 0.85f, x, y);
                    path.CloseFigure();
                    break;

                // Stars and Banners
                case "Star4":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 4, 0.38f));
                    break;
                case "Star5":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 5, 0.38f));
                    break;
                case "Star6":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 6, 0.5f));
                    break;
                case "Star8":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 8, 0.55f));
                    break;
                case "Star10":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 10, 0.65f));
                    break;
                case "Star12":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 12, 0.7f));
                    break;
                case "Star16":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 16, 0.75f));
                    break;
                case "Star24":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 24, 0.8f));
                    break;
                case "Star32":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 32, 0.82f));
                    break;

                case "Explosion1":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 8, 0.42f));
                    break;
                case "Explosion2":
                    path.AddPolygon(CreateStarPoints(x, y, w, h, 14, 0.58f));
                    break;

                case "HorizontalScroll":
                    {
                        float curl = w * 0.12f;
                        path.AddArc(x, y, curl * 2, h * 0.3f, 180, 180);
                        path.AddLine(x + curl * 2, y + h * 0.15f, x + w - curl, y + h * 0.15f);
                        path.AddArc(x + w - curl * 2, y + h * 0.15f, curl * 2, h * 0.3f, 0, 180);
                        path.AddLine(x + w - curl * 2, y + h * 0.3f, x + w - curl * 2, y + h * 0.85f);
                        path.AddArc(x + w - curl * 2, y + h * 0.7f, curl * 2, h * 0.3f, 0, 180);
                        path.AddLine(x + w - curl, y + h * 0.85f, x + curl, y + h * 0.85f);
                        path.AddArc(x, y + h * 0.55f, curl * 2, h * 0.3f, 180, 180);
                        path.CloseFigure();
                    }
                    break;

                case "VerticalScroll":
                    {
                        float curl = h * 0.12f;
                        path.AddArc(x, y, w * 0.3f, curl * 2, 90, 180);
                        path.AddLine(x + w * 0.15f, y + curl * 2, x + w * 0.15f, y + h - curl);
                        path.AddArc(x + w * 0.15f, y + h - curl * 2, w * 0.3f, curl * 2, 270, 180);
                        path.AddLine(x + w * 0.3f, y + h - curl * 2, x + w * 0.85f, y + h - curl * 2);
                        path.AddArc(x + w * 0.7f, y + h - curl * 2, w * 0.3f, curl * 2, 270, 180);
                        path.AddLine(x + w * 0.85f, y + h - curl, x + w * 0.85f, y + curl);
                        path.AddArc(x + w * 0.55f, y, w * 0.3f, curl * 2, 90, 180);
                        path.CloseFigure();
                    }
                    break;

                case "RibbonUp":
                    {
                        float fold = w * 0.15f;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x, y + h * 0.3f), new PointF(x + fold, y), new PointF(x + w - fold, y),
                            new PointF(x + w, y + h * 0.3f), new PointF(x + w - fold * 0.6f, y + h * 0.6f),
                            new PointF(x + w - fold, y + h * 0.45f), new PointF(x + w - fold, y + h),
                            new PointF(x + fold, y + h), new PointF(x + fold, y + h * 0.45f),
                            new PointF(x + fold * 0.6f, y + h * 0.6f)
                        });
                    }
                    break;

                case "RibbonDown":
                    {
                        float fold = w * 0.15f;
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + fold, y), new PointF(x + w - fold, y),
                            new PointF(x + w - fold, y + h * 0.55f), new PointF(x + w - fold * 0.6f, y + h * 0.4f),
                            new PointF(x + w, y + h * 0.7f), new PointF(x + w - fold, y + h),
                            new PointF(x + fold, y + h), new PointF(x, y + h * 0.7f),
                            new PointF(x + fold * 0.6f, y + h * 0.4f), new PointF(x + fold, y + h * 0.55f)
                        });
                    }
                    break;

                // Callouts
                case "CalloutRect":
                    {
                        float tX = (adj2 > 0.05f && adj2 < 0.95f) ? (x + w * adj2) : (x + w * 0.25f);
                        float tY = (adj3 > 0.8f && adj3 < 1.6f) ? (y + h * adj3) : (y + h * 1.25f);
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x, y),
                            new PointF(x + w, y),
                            new PointF(x + w, y + h * 0.78f),
                            new PointF(x + w * 0.45f, y + h * 0.78f),
                            new PointF(tX, tY),
                            new PointF(x + w * 0.28f, y + h * 0.78f),
                            new PointF(x, y + h * 0.78f)
                        });
                    }
                    break;

                case "CalloutRoundedRect":
                    {
                        float r = Math.Min(w, h) * 0.18f;
                        float bH = h * 0.78f;
                        float tX = (adj2 > 0.05f && adj2 < 0.95f) ? (x + w * adj2) : (x + w * 0.25f);
                        float tY = (adj3 > 0.8f && adj3 < 1.6f) ? (y + h * adj3) : (y + h * 1.25f);
                        path.AddArc(x, y, r * 2, r * 2, 180, 90);
                        path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
                        path.AddArc(x + w - r * 2, y + bH - r * 2, r * 2, r * 2, 0, 90);
                        path.AddLine(x + w - r, y + bH, x + w * 0.45f, y + bH);
                        path.AddLine(x + w * 0.45f, y + bH, tX, tY);
                        path.AddLine(tX, tY, x + w * 0.28f, y + bH);
                        path.AddLine(x + w * 0.28f, y + bH, x + r, y + bH);
                        path.AddArc(x, y + bH - r * 2, r * 2, r * 2, 90, 90);
                        path.CloseFigure();
                    }
                    break;

                case "CalloutOval":
                    {
                        float bH = h * 0.78f;
                        float tX = (adj2 > 0.05f && adj2 < 0.95f) ? (x + w * adj2) : (x + w * 0.25f);
                        float tY = (adj3 > 0.8f && adj3 < 1.6f) ? (y + h * adj3) : (y + h * 1.25f);
                        path.AddEllipse(x, y, w, bH);
                        path.AddPolygon(new PointF[]
                        {
                            new PointF(x + w * 0.30f, y + bH * 0.9f),
                            new PointF(tX, tY),
                            new PointF(x + w * 0.45f, y + bH * 0.9f)
                        });
                    }
                    break;

                case "CalloutCloud":
                    {
                        float bH = h * 0.75f;
                        path.AddEllipse(x + w * 0.15f, y + bH * 0.35f, w * 0.35f, bH * 0.55f);
                        path.AddEllipse(x + w * 0.35f, y + bH * 0.15f, w * 0.4f, bH * 0.6f);
                        path.AddEllipse(x + w * 0.55f, y + bH * 0.35f, w * 0.35f, bH * 0.55f);
                        path.AddRectangle(new RectangleF(x + w * 0.25f, y + bH * 0.55f, w * 0.5f, bH * 0.35f));
                        // Speech bubbles
                        path.AddEllipse(x + w * 0.25f, y + h * 0.8f, w * 0.12f, h * 0.1f);
                        path.AddEllipse(x + w * 0.18f, y + h * 0.92f, w * 0.08f, h * 0.07f);
                    }
                    break;

                case "LineCallout1":
                    path.AddRectangle(new RectangleF(x, y, w, h * 0.7f));
                    path.AddLine(x + w * 0.2f, y + h * 0.7f, x, y + h);
                    break;

                case "LineCallout2":
                    path.AddRectangle(new RectangleF(x, y, w, h * 0.7f));
                    path.AddLine(x + w * 0.2f, y + h * 0.7f, x + w * 0.1f, y + h * 0.85f);
                    path.AddLine(x + w * 0.1f, y + h * 0.85f, x, y + h);
                    break;

                default:
                    path.AddRectangle(new RectangleF(x, y, w, h));
                    break;
            }
            return path;
        }

        private static PointF[] CreateStarPoints(float x, float y, float w, float h, int numPoints, float innerRatio)
        {
            PointF[] pts = new PointF[numPoints * 2];
            float cx = x + w / 2f;
            float cy = y + h / 2f;
            float rx = w / 2f;
            float ry = h / 2f;
            double step = Math.PI / numPoints;
            double startAngle = -Math.PI / 2.0;

            for (int i = 0; i < numPoints * 2; i++)
            {
                double angle = startAngle + i * step;
                float rCurrentX = (i % 2 == 0) ? rx : (rx * innerRatio);
                float rCurrentY = (i % 2 == 0) ? ry : (ry * innerRatio);
                pts[i] = new PointF((float)(cx + rCurrentX * Math.Cos(angle)), (float)(cy + rCurrentY * Math.Sin(angle)));
            }
            return pts;
        }

        public static void DrawShape(D2DGraphics g, DocShapeObject shape, Rectangle bounds, RibbonBar ribbon, float zoomLevel)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            if (shape != null) shape.ApplyCachedTagMetadata();

            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // SPECIAL: Render SmartArt Diagrams
            if (shape.ShapeType == "SmartArt" || !string.IsNullOrEmpty(shape.SmartArtType))
            {
                if (shape.CachedImage == null && !string.IsNullOrEmpty(shape.ImagePath) && System.IO.File.Exists(shape.ImagePath))
                {
                    shape.CachedImage = DocShapeObject.LoadOptimizedImage(shape.ImagePath, bounds.Width, bounds.Height);
                }

                if (shape.SubShapeRelativeBounds == null || shape.SubShapeRelativeBounds.Count == 0)
                {
                    string saType = shape.SmartArtType ?? "List_BasicBlock";
                    string[] saNodes = SmartArtRenderer.GetSmartArtNodeList(saType, shape.TextContent);
                    SmartArtRenderer.PopulateSubShapeBounds(bounds, saType, saNodes.Length, shape);
                }

                bool hasVectorNodes = (shape.SubShapeRelativeBounds != null && shape.SubShapeRelativeBounds.Count > 0);
                if (shape.CachedImage != null && !hasVectorNodes)
                {
                    g.DrawImage(shape.CachedImage, bounds);

                    if (shape.IsSelected)
                    {
                        using (Pen selPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f) { DashStyle = DashStyle.Dash })
                        {
                            g.DrawRectangle(selPen, bounds);
                        }
                        int hs = Math.Max(5, (int)(7 * zoomLevel));
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
                else
                {
                    SmartArtRenderer.SuppressNodeBodies = hasVectorNodes;
                    try
                    {
                        SmartArtRenderer.DrawSmartArt(g, bounds, shape, zoomLevel);
                    }
                    finally
                    {
                        SmartArtRenderer.SuppressNodeBodies = false;
                    }
                }

                // Draw customized or actively edited SmartArt nodes directly on canvas
                if (shape.SubShapeRelativeBounds != null && shape.SubShapeRelativeBounds.Count > 0)
                {
                    for (int ni = 0; ni < shape.SubShapeRelativeBounds.Count; ni++)
                    {
                        bool isActivelyEditing = shape.IsSelected && shape.IsEditingText && shape.SelectedNodeIndex == ni;
                        bool hasCustomColor = shape.NodeColors != null && shape.NodeColors.ContainsKey(ni);
                        bool isModifiedText = shape.ModifiedNodes != null && shape.ModifiedNodes.Contains(ni);
                        bool hasCustomShape = shape.NodeShapeTypes != null && shape.NodeShapeTypes.ContainsKey(ni);
                        bool hasCustomOutline = shape.NodeOutlineColors != null && shape.NodeOutlineColors.ContainsKey(ni);
                        bool hasCustomTextColor = shape.NodeTextColors != null && shape.NodeTextColors.ContainsKey(ni);
                        bool hasCustomScale = shape.NodeScaleDeltas != null && shape.NodeScaleDeltas.ContainsKey(ni);
                        bool hasVisualStyle = shape.VisualStylePreset > 0;
                        bool shouldDrawNode = true;

                        if (shouldDrawNode)
                        {
                            RectangleF rf = shape.SubShapeRelativeBounds[ni];
                            float scaleDelta = hasCustomScale ? shape.NodeScaleDeltas[ni] : 0f;
                            int baseW = (int)(rf.Width * bounds.Width);
                            int baseH = (int)(rf.Height * bounds.Height);

                            string sType = "RoundedRectangle";
                            if (hasCustomShape && !string.IsNullOrEmpty(shape.NodeShapeTypes[ni]))
                            {
                                sType = shape.NodeShapeTypes[ni];
                            }
                            else if (shape.SmartArtType != null && shape.SmartArtType.Contains("Chevron"))
                            {
                                sType = "Chevron";
                            }
                            else if (shape.SmartArtType != null && shape.SmartArtType.Contains("Radial"))
                            {
                                sType = (ni == 0) ? "Circle" : "RoundedRectangle";
                            }
                            else if (shape.SmartArtType != null && (shape.SmartArtType.StartsWith("Cycle_") || shape.SmartArtType.Contains("Ring") || shape.SmartArtType.Contains("Nondirectional")))
                            {
                                sType = "RoundedRectangle";
                            }

                            if (shape.AspectRatioLock && (sType == "Oval" || sType == "Circle"))
                            {
                                sType = "Circle";
                            }

                            Rectangle nodeRect;
                            if (sType == "Circle" || (shape.AspectRatioLock && sType == "Oval"))
                            {
                                int baseDim = Math.Min(baseW, baseH);
                                int nw = Math.Max(6, (int)(baseDim * (1f + scaleDelta)));
                                int nh = nw;
                                int nx = bounds.X + (int)(rf.X * bounds.Width) + (baseW - nw) / 2;
                                int ny = bounds.Y + (int)(rf.Y * bounds.Height) + (baseH - nh) / 2;
                                nodeRect = new Rectangle(nx, ny, nw, nh);
                            }
                            else
                            {
                                int nw = Math.Max(6, (int)(baseW * (1f + scaleDelta)));
                                int nh = Math.Max(6, (int)(baseH * (1f + scaleDelta)));
                                int nx = bounds.X + (int)(rf.X * bounds.Width) + (baseW - nw) / 2;
                                int ny = bounds.Y + (int)(rf.Y * bounds.Height) + (baseH - nh) / 2;
                                nodeRect = new Rectangle(nx, ny, nw, nh);
                            }

                            Color bgCol;
                            if (hasCustomColor)
                            {
                                bgCol = shape.NodeColors[ni];
                            }
                            else if (shape.DefaultNodeColors != null && shape.DefaultNodeColors.ContainsKey(ni))
                            {
                                bgCol = shape.DefaultNodeColors[ni];
                            }
                            else if (shape.ResolvedThemePalette != null && shape.ResolvedThemePalette.Length > 0)
                            {
                                if (shape.StylePresetIndex == 2)
                                {
                                    bgCol = shape.ResolvedThemePalette[ni % shape.ResolvedThemePalette.Length];
                                }
                                else
                                {
                                    bgCol = shape.ResolvedThemePalette[0];
                                }
                            }
                            else
                            {
                                bgCol = Color.FromArgb(68, 114, 196);
                            }

                            Color outlineCol = hasCustomOutline ? shape.NodeOutlineColors[ni] : ControlPaint.Dark(bgCol, 0.15f);
                            float outlineWidth = 1.2f;

                            int vStyle = shape.VisualStylePreset;
                            int radius = Math.Max(3, (int)(6 * zoomLevel));

                            // 1. Draw 3D Shadow if vStyle == 2
                            if (vStyle == 2)
                            {
                                Rectangle shRect = new Rectangle(nodeRect.X + 3, nodeRect.Y + 4, nodeRect.Width, nodeRect.Height);
                                using (GraphicsPath shPath = (sType == "RoundedRectangle") ? CreateRoundedPath(shRect, radius) : CreateShapePath(sType, shRect))
                                using (SolidBrush shB = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                                {
                                    g.FillPath(shB, shPath);
                                }
                            }

                            // 2. Main Node Path
                            using (GraphicsPath nPath = (sType == "RoundedRectangle") ? CreateRoundedPath(nodeRect, radius) : CreateShapePath(sType, nodeRect))
                            {
                                if (vStyle == 4) // Outline mode: White/transparent body, colored border
                                {
                                    using (SolidBrush whiteB = new SolidBrush(Color.White))
                                    {
                                        g.FillPath(whiteB, nPath);
                                    }
                                    outlineWidth = 2.2f;
                                    outlineCol = bgCol;
                                }
                                else if (vStyle == 1) // Gradient mode
                                {
                                    Color lightCol = ControlPaint.Light(bgCol, 0.35f);
                                    using (LinearGradientBrush lgb = new LinearGradientBrush(nodeRect, lightCol, bgCol, 90f))
                                    {
                                        g.FillPath(lgb, nPath);
                                    }
                                    outlineCol = ControlPaint.Dark(bgCol, 0.2f);
                                }
                                else if (vStyle == 3) // Glass mode
                                {
                                    Color lightCol = ControlPaint.Light(bgCol, 0.2f);
                                    Color darkCol = ControlPaint.Dark(bgCol, 0.1f);
                                    using (LinearGradientBrush lgb = new LinearGradientBrush(nodeRect, lightCol, darkCol, 90f))
                                    {
                                        g.FillPath(lgb, nPath);
                                    }
                                    // Glossy reflection highlight on top half
                                    Rectangle glassRect = new Rectangle(nodeRect.X + 2, nodeRect.Y + 2, Math.Max(2, nodeRect.Width - 4), Math.Max(2, nodeRect.Height / 2 - 1));
                                    using (GraphicsPath glPath = (sType == "RoundedRectangle") ? CreateRoundedPath(glassRect, Math.Max(2, radius - 1)) : CreateShapePath(sType, glassRect))
                                    using (LinearGradientBrush glB = new LinearGradientBrush(glassRect, Color.FromArgb(140, 255, 255, 255), Color.FromArgb(15, 255, 255, 255), 90f))
                                    {
                                        g.FillPath(glB, glPath);
                                    }
                                }
                                else // Flat (vStyle == 0) or 3D Bevel (vStyle == 2)
                                {
                                    using (SolidBrush bgB = new SolidBrush(bgCol))
                                    {
                                        g.FillPath(bgB, nPath);
                                    }
                                    if (vStyle == 2)
                                    {
                                        // Draw 3D Bevel inner highlight
                                        Rectangle bevelRect = new Rectangle(nodeRect.X + 1, nodeRect.Y + 1, Math.Max(2, nodeRect.Width - 2), Math.Max(2, nodeRect.Height - 2));
                                        using (GraphicsPath bPath = (sType == "RoundedRectangle") ? CreateRoundedPath(bevelRect, radius) : CreateShapePath(sType, bevelRect))
                                        using (Pen topPen = new Pen(Color.FromArgb(90, 255, 255, 255), 1.5f))
                                        {
                                            g.DrawPath(topPen, bPath);
                                        }
                                    }
                                }

                                // Border
                                using (Pen borderPen = new Pen(outlineCol, outlineWidth))
                                {
                                    g.DrawPath(borderPen, nPath);
                                }

                                string nTxt = shape.GetNodeText(ni);
                                if (string.IsNullOrEmpty(nTxt) || nTxt == "[Text]" || nTxt == "(Text)" || nTxt == "Text")
                                {
                                    nTxt = isActivelyEditing ? "" : "[Text]";
                                }

                                Color txtCol;
                                if (hasCustomTextColor)
                                {
                                    txtCol = shape.NodeTextColors[ni];
                                }
                                else if (vStyle == 4)
                                {
                                    txtCol = bgCol;
                                }
                                else
                                {
                                    txtCol = (bgCol.R * 0.299 + bgCol.G * 0.587 + bgCol.B * 0.114) > 160 ? Color.FromArgb(35, 35, 35) : Color.White;
                                }

                                float fontSize = Math.Max(8.0f, Math.Min(14.0f, (nodeRect.Height / 3.2f) * zoomLevel));
                                using (Font nFont = new Font("Segoe UI", fontSize, FontStyle.Bold))
                                using (SolidBrush tb = new SolidBrush(txtCol))
                                using (StringFormat sf = new StringFormat
                                {
                                    Alignment = StringAlignment.Center,
                                    LineAlignment = StringAlignment.Center,
                                    FormatFlags = StringFormatFlags.LineLimit,
                                    Trimming = StringTrimming.EllipsisWord
                                })
                                {
                                    Rectangle textR = new Rectangle(nodeRect.X + 4, nodeRect.Y + 4, Math.Max(4, nodeRect.Width - 8), Math.Max(4, nodeRect.Height - 8));
                                    Region oldClip = g.Clip;
                                    g.SetClip(nPath, CombineMode.Intersect);
                                    g.DrawString(nTxt, nFont, tb, textR, sf);
                                    g.Clip = oldClip;

                                    // If actively editing this node, draw the text cursor caret!
                                    if (isActivelyEditing)
                                    {
                                        int curIdx = Math.Max(0, Math.Min(shape.TextCursorIndex, nTxt.Length));
                                        string beforeCursor = nTxt.Substring(0, curIdx);

                                        SizeF fullSz = g.MeasureString(nTxt, nFont, textR.Width, sf);
                                        SizeF subSz = g.MeasureString(beforeCursor, nFont, textR.Width, StringFormat.GenericTypographic);

                                        float caretX = textR.X + (textR.Width - fullSz.Width) / 2f + subSz.Width;
                                        if (caretX < textR.X + 2) caretX = textR.X + 2;
                                        if (caretX > textR.Right - 4) caretX = textR.Right - 4;

                                        float caretY = textR.Y + (textR.Height - fullSz.Height) / 2f;
                                        float caretH = Math.Max(12f, fullSz.Height);

                                        using (Pen caretPen = new Pen(txtCol, 1.0f) { StartCap = System.Drawing.Drawing2D.LineCap.Flat, EndCap = System.Drawing.Drawing2D.LineCap.Flat })
                                        {
                                            g.DrawLine(caretPen, (int)caretX, (int)caretY, (int)caretX, (int)(caretY + caretH));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Sub-Selection: Highlight specific clicked node with dashed border and 4 corner handles!
                if (shape.IsSelected && shape.SelectedNodeIndex >= 0 && shape.SubShapeRelativeBounds != null && shape.SelectedNodeIndex < shape.SubShapeRelativeBounds.Count)
                {
                    RectangleF rf = shape.SubShapeRelativeBounds[shape.SelectedNodeIndex];
                    Rectangle nodeRect = new Rectangle(
                        bounds.X + (int)(rf.X * bounds.Width),
                        bounds.Y + (int)(rf.Y * bounds.Height),
                        Math.Max(6, (int)(rf.Width * bounds.Width)),
                        Math.Max(6, (int)(rf.Height * bounds.Height))
                    );

                    using (Pen nodePen = new Pen(Color.FromArgb(0, 120, 215), 2.0f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawRectangle(nodePen, nodeRect);
                    }
                    int nhs = Math.Max(4, (int)(6 * zoomLevel));
                    Point[] nhandles = new Point[]
                    {
                        new Point(nodeRect.Left, nodeRect.Top),
                        new Point(nodeRect.Left + nodeRect.Width / 2, nodeRect.Top),
                        new Point(nodeRect.Right, nodeRect.Top),
                        new Point(nodeRect.Right, nodeRect.Top + nodeRect.Height / 2),
                        new Point(nodeRect.Right, nodeRect.Bottom),
                        new Point(nodeRect.Left + nodeRect.Width / 2, nodeRect.Bottom),
                        new Point(nodeRect.Left, nodeRect.Bottom),
                        new Point(nodeRect.Left, nodeRect.Top + nodeRect.Height / 2)
                    };
                    for (int nhi = 0; nhi < nhandles.Length; nhi++)
                    {
                        Rectangle nhR = new Rectangle(nhandles[nhi].X - nhs / 2, nhandles[nhi].Y - nhs / 2, nhs, nhs);
                        g.FillRectangle(Brushes.White, nhR);
                        using (Pen nhPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                        {
                            g.DrawRectangle(nhPen, nhR);
                        }
                    }
                }

                return;
            }

            // SPECIAL: Render Grouped Shapes (ShapeType == "Group")
            if (shape.ShapeType == "Group")
            {
                if (shape.GroupChildren != null)
                {
                    foreach (var child in shape.GroupChildren)
                    {
                        Rectangle childBounds = new Rectangle(
                            bounds.X + (int)(child.PageX * zoomLevel),
                            bounds.Y + (int)(child.PageY * zoomLevel),
                            Math.Max(5, (int)((child.BaseWidth + child.WidthDelta) * zoomLevel)),
                            Math.Max(5, (int)((child.BaseHeight + child.HeightDelta) * zoomLevel))
                        );
                        child.LastBounds = childBounds;
                        DrawShape(g, child, childBounds, ribbon, zoomLevel);
                    }
                }

                // Draw outer dashed frame and handles when group is selected
                if (shape.IsSelected)
                {
                    using (Pen grpPen = new Pen(Color.FromArgb(0, 120, 215), 1.5f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawRectangle(grpPen, bounds);
                    }
                    int hs = 6;
                    int[] hx = new int[] { bounds.Left - hs / 2, bounds.Left + bounds.Width / 2 - hs / 2, bounds.Right - hs / 2, bounds.Right - hs / 2, bounds.Right - hs / 2, bounds.Left + bounds.Width / 2 - hs / 2, bounds.Left - hs / 2, bounds.Left - hs / 2 };
                    int[] hy = new int[] { bounds.Top - hs / 2, bounds.Top - hs / 2, bounds.Top - hs / 2, bounds.Top + bounds.Height / 2 - hs / 2, bounds.Bottom - hs / 2, bounds.Bottom - hs / 2, bounds.Bottom - hs / 2, bounds.Top + bounds.Height / 2 - hs / 2 };
                    using (SolidBrush hb = new SolidBrush(Color.White))
                    using (Pen hp = new Pen(Color.FromArgb(0, 120, 215), 1.0f))
                    {
                        for (int hi = 0; hi < 8; hi++)
                        {
                            g.FillRectangle(hb, hx[hi], hy[hi], hs, hs);
                            g.DrawRectangle(hp, hx[hi], hy[hi], hs, hs);
                        }
                    }
                }
                return;
            }

            // SPECIAL: Render Embedded Charts
            if (shape.ShapeType == "Chart" || shape.ChartData != null)
            {
                ChartRenderer.DrawChart(g, bounds, shape.ChartData ?? DocChartData.CreateDefault(shape.TextContent), shape.IsSelected, zoomLevel);
                return;
            }

            // SPECIAL: Render ShapeType == "Slicer"
            if (shape.ShapeType == "Slicer")
            {
                using (GraphicsPath cardPath = CreateRoundedPath(bounds, 6))
                {
                    using (SolidBrush shB = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        g.FillPath(shB, CreateRoundedPath(new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width, bounds.Height), 6));
                    }
                    g.FillPath(Brushes.White, cardPath);
                    using (Pen bp = new Pen(Color.FromArgb(16, 124, 65), 1.5f)) g.DrawPath(bp, cardPath);
                }

                Rectangle hdrRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, (int)(32 * zoomLevel));
                using (GraphicsPath topPath = CreateTopRoundedPath(hdrRect, 6))
                {
                    using (SolidBrush hdB = new SolidBrush(Color.FromArgb(16, 124, 65))) g.FillPath(hdB, topPath);
                }
                using (Font hFont = new Font("Segoe UI", 9.0f * zoomLevel, FontStyle.Bold))
                {
                    string hText = !string.IsNullOrEmpty(shape.CustomName) ? shape.CustomName : "BỘ CẮT LÁT (SLICER)";
                    g.DrawString(hText, hFont, Brushes.White, bounds.X + 8, bounds.Y + 6);
                }

                string[] items = new string[] { "(Tất cả / All)", "Nhóm A", "Nhóm B", "Nhóm C", "Khác" };
                int btnY = bounds.Y + (int)(38 * zoomLevel);
                int btnH = (int)(24 * zoomLevel);
                using (Font iFont = new Font("Segoe UI", 8.5f * zoomLevel, FontStyle.Regular))
                {
                    for (int i = 0; i < items.Length && btnY + btnH < bounds.Bottom - 4; i++)
                    {
                        Rectangle btnRect = new Rectangle(bounds.X + 6, btnY, bounds.Width - 12, btnH);
                        bool isFirst = (i == 0);
                        using (SolidBrush bBg = new SolidBrush(isFirst ? Color.FromArgb(220, 245, 230) : Color.FromArgb(248, 249, 250)))
                        using (Pen bPen = new Pen(isFirst ? Color.FromArgb(16, 124, 65) : Color.FromArgb(226, 232, 240)))
                        {
                            g.FillRectangle(bBg, btnRect);
                            g.DrawRectangle(bPen, btnRect);
                        }
                        g.DrawString(items[i], iFont, isFirst ? Brushes.DarkGreen : Brushes.Black, btnRect.X + 6, btnRect.Y + 3);
                        btnY += btnH + 4;
                    }
                }
                return;
            }

            // SPECIAL: Render ShapeType == "Timeline"
            if (shape.ShapeType == "Timeline")
            {
                using (GraphicsPath cardPath = CreateRoundedPath(bounds, 6))
                {
                    using (SolidBrush shB = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        g.FillPath(shB, CreateRoundedPath(new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width, bounds.Height), 6));
                    }
                    g.FillPath(Brushes.White, cardPath);
                    using (Pen bp = new Pen(Color.FromArgb(2, 132, 199), 1.5f)) g.DrawPath(bp, cardPath);
                }

                Rectangle hdrRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, (int)(28 * zoomLevel));
                using (GraphicsPath topPath = CreateTopRoundedPath(hdrRect, 6))
                {
                    using (SolidBrush hdB = new SolidBrush(Color.FromArgb(2, 132, 199))) g.FillPath(hdB, topPath);
                }
                using (Font hFont = new Font("Segoe UI", 9.0f * zoomLevel, FontStyle.Bold))
                {
                    g.DrawString("DÒNG THỜI GIAN (TIMELINE) - NĂM 2026", hFont, Brushes.White, bounds.X + 8, bounds.Y + 5);
                }

                string[] qtrs = new string[] { "Quý 1", "Quý 2", "Quý 3", "Quý 4" };
                int qW = (bounds.Width - 16) / 4;
                int qY = bounds.Y + (int)(34 * zoomLevel);
                int qH = bounds.Height - (int)(40 * zoomLevel);
                using (Font qFont = new Font("Segoe UI", 8.5f * zoomLevel, FontStyle.Bold))
                {
                    for (int qi = 0; qi < 4; qi++)
                    {
                        Rectangle qRect = new Rectangle(bounds.X + 8 + qi * qW, qY, qW - 4, qH);
                        bool isSel = (qi == 0);
                        using (SolidBrush qBg = new SolidBrush(isSel ? Color.FromArgb(224, 242, 254) : Color.FromArgb(248, 250, 252)))
                        using (Pen qPen = new Pen(isSel ? Color.FromArgb(2, 132, 199) : Color.FromArgb(226, 232, 240)))
                        {
                            g.FillRectangle(qBg, qRect);
                            g.DrawRectangle(qPen, qRect);
                        }
                        g.DrawString(qtrs[qi], qFont, isSel ? Brushes.Navy : Brushes.DimGray, qRect.X + 4, qRect.Y + (qH - 16) / 2);
                    }
                }
                return;
            }

            // SPECIAL: Render ShapeType == "WordArt"
            if (shape.ShapeType == "WordArt")
            {
                string text = !string.IsNullOrEmpty(shape.TextContent) ? shape.TextContent : "ZENQ WORDART";
                using (Font waFont = new Font("Impact", Math.Max(12, bounds.Height * 0.55f), FontStyle.Regular))
                {
                    using (SolidBrush shB = new SolidBrush(Color.FromArgb(80, 30, 58, 138)))
                    {
                        g.DrawString(text, waFont, shB, bounds.X + 3, bounds.Y + 3);
                    }
                    using (LinearGradientBrush lgb = new LinearGradientBrush(bounds, Color.FromArgb(37, 99, 235), Color.FromArgb(147, 51, 234), 45f))
                    {
                        g.DrawString(text, waFont, lgb, bounds.X, bounds.Y);
                    }
                }
                return;
            }

            // SPECIAL: Render ShapeType == "Image"
            if (shape.ShapeType == "Image")
            {
                if (shape.CachedImage == null)
                {
                    if (!string.IsNullOrEmpty(shape.ImagePath) && System.IO.File.Exists(shape.ImagePath))
                    {
                        int reqW = bounds.Width > 0 ? bounds.Width : (shape.BaseWidth > 0 ? shape.BaseWidth : 48);
                        int reqH = bounds.Height > 0 ? bounds.Height : (shape.BaseHeight > 0 ? shape.BaseHeight : 24);
                        shape.CachedImage = DocShapeObject.LoadOptimizedImage(shape.ImagePath, reqW, reqH);
                    }
                }

                if (shape.CachedImage == null)
                {
                    if (shape.WrapMode != 0 && bounds.Width > 30 && bounds.Height > 30)
                    {
                        using (SolidBrush emptyBg = new SolidBrush(Color.FromArgb(245, 246, 248)))
                        using (Pen emptyPen = new Pen(Color.FromArgb(210, 215, 222), 1.0f) { DashStyle = DashStyle.Dash })
                        {
                            g.FillRectangle(emptyBg, bounds);
                            g.DrawRectangle(emptyPen, bounds);
                            using (Font f = new Font("Segoe UI", 8.5f))
                            using (SolidBrush tb = new SolidBrush(Color.FromArgb(140, 145, 155)))
                            {
                                string msg = "[Hình ảnh]";
                                SizeF sz = g.MeasureString(msg, f);
                                g.DrawString(msg, f, tb, bounds.X + (bounds.Width - sz.Width) / 2, bounds.Y + (bounds.Height - sz.Height) / 2);
                            }
                        }
                    }
                    return;
                }

                // 1. Picture Style Presets Background & Frames
                if (shape.PictureStylePreset == 1) // Simple White Frame
                {
                    int pad = Math.Max(4, (int)(6 * zoomLevel));
                    Rectangle outer = new Rectangle(bounds.X - pad, bounds.Y - pad, bounds.Width + pad * 2, bounds.Height + pad * 2);
                    using (SolidBrush shB = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                    {
                        g.FillRectangle(shB, outer.X + 4, outer.Y + 4, outer.Width, outer.Height);
                    }
                    g.FillRectangle(Brushes.White, outer);
                    g.DrawRectangle(Pens.LightGray, outer);
                }
                else if (shape.PictureStylePreset == 2) // Matte Black Frame
                {
                    int pad = Math.Max(4, (int)(6 * zoomLevel));
                    Rectangle outer = new Rectangle(bounds.X - pad, bounds.Y - pad, bounds.Width + pad * 2, bounds.Height + pad * 2);
                    using (SolidBrush shB = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        g.FillRectangle(shB, outer.X + 4, outer.Y + 4, outer.Width, outer.Height);
                    }
                    using (SolidBrush blkB = new SolidBrush(Color.FromArgb(28, 28, 28)))
                    {
                        g.FillRectangle(blkB, outer);
                    }
                }
                else if (shape.PictureStylePreset == 3) // Reflected Bevel
                {
                    if (shape.CachedImage != null)
                    {
                        int refH = (int)(bounds.Height * 0.45f);
                        Rectangle refRect = new Rectangle(bounds.X, bounds.Bottom + 2, bounds.Width, refH);
                        using (Bitmap refBmp = new Bitmap(shape.CachedImage, bounds.Width, Math.Max(10, refH)))
                        {
                            refBmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                            g.DrawImage(refBmp, refRect);
                            using (LinearGradientBrush fadeB = new LinearGradientBrush(refRect, Color.FromArgb(120, 255, 255, 255), Color.White, 90f))
                            {
                                g.FillRectangle(fadeB, refRect);
                            }
                        }
                    }
                }
                else if (shape.PictureStylePreset == 5) // Metallic 3D Frame
                {
                    int pad = Math.Max(4, (int)(7 * zoomLevel));
                    Rectangle outer = new Rectangle(bounds.X - pad, bounds.Y - pad, bounds.Width + pad * 2, bounds.Height + pad * 2);
                    using (LinearGradientBrush metB = new LinearGradientBrush(outer, Color.FromArgb(235, 240, 245), Color.FromArgb(160, 170, 185), 45f))
                    {
                        g.FillRectangle(metB, outer);
                    }
                    using (Pen metP = new Pen(Color.FromArgb(120, 130, 145), 1.5f))
                    {
                        g.DrawRectangle(metP, outer);
                    }
                }
                else if (shape.HasShadow && shape.WrapMode != 5)
                {
                    using (SolidBrush shadowB = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                    {
                        g.FillRectangle(shadowB, bounds.X + 4, bounds.Y + 4, bounds.Width, bounds.Height);
                    }
                }

                // 2. Draw the Main Image with Effects
                if (shape.CachedImage != null)
                {
                    bool hasEffects = (shape.PictureTransparency > 0.01f || shape.PictureGrayscale || shape.PictureSepia || Math.Abs(shape.PictureBrightness) > 0.01f);
                    if (hasEffects)
                    {
                        using (ImageAttributes attr = new ImageAttributes())
                        {
                            if (shape.PictureTransparency > 0.01f)
                            {
                                float alpha = Math.Max(0.05f, 1.0f - shape.PictureTransparency);
                                ColorMatrix cm = new ColorMatrix();
                                cm.Matrix33 = alpha;
                                attr.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                            }
                            else if (shape.PictureGrayscale)
                            {
                                ColorMatrix cm = new ColorMatrix(new float[][] {
                                    new float[] {0.3f, 0.3f, 0.3f, 0, 0},
                                    new float[] {0.59f, 0.59f, 0.59f, 0, 0},
                                    new float[] {0.11f, 0.11f, 0.11f, 0, 0},
                                    new float[] {0, 0, 0, 1, 0},
                                    new float[] {0, 0, 0, 0, 1}
                                });
                                attr.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                            }
                            else if (shape.PictureSepia)
                            {
                                ColorMatrix cm = new ColorMatrix(new float[][] {
                                    new float[] {0.393f, 0.349f, 0.272f, 0, 0},
                                    new float[] {0.769f, 0.686f, 0.534f, 0, 0},
                                    new float[] {0.189f, 0.168f, 0.131f, 0, 0},
                                    new float[] {0, 0, 0, 1, 0},
                                    new float[] {0, 0, 0, 0, 1}
                                });
                                attr.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                            }
                            else if (Math.Abs(shape.PictureBrightness) > 0.01f)
                            {
                                float b = shape.PictureBrightness;
                                ColorMatrix cm = new ColorMatrix(new float[][] {
                                    new float[] {1, 0, 0, 0, 0},
                                    new float[] {0, 1, 0, 0, 0},
                                    new float[] {0, 0, 1, 0, 0},
                                    new float[] {0, 0, 0, 1, 0},
                                    new float[] {b, b, b, 0, 1}
                                });
                                attr.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                            }

                            if (shape.PictureStylePreset == 4) // Rounded Corner
                            {
                                using (GraphicsPath rPath = CreateShapePath("RoundedRectangle", bounds))
                                {
                                    var prevClip = g.Clip;
                                    g.SetClip(rPath, CombineMode.Intersect);
                                    g.DrawImage(shape.CachedImage, bounds, 0, 0, shape.CachedImage.Width, shape.CachedImage.Height, GraphicsUnit.Pixel, attr);
                                    g.Clip = prevClip;
                                }
                            }
                            else if (shape.PictureStylePreset == 6) // Soft Edge Oval
                            {
                                using (GraphicsPath oPath = CreateShapePath("Oval", bounds))
                                {
                                    var prevClip = g.Clip;
                                    g.SetClip(oPath, CombineMode.Intersect);
                                    g.DrawImage(shape.CachedImage, bounds, 0, 0, shape.CachedImage.Width, shape.CachedImage.Height, GraphicsUnit.Pixel, attr);
                                    g.Clip = prevClip;
                                }
                            }
                            else
                            {
                                g.DrawImage(shape.CachedImage, bounds, 0, 0, shape.CachedImage.Width, shape.CachedImage.Height, GraphicsUnit.Pixel, attr);
                            }
                        }
                    }
                    else
                    {
                        if (shape.PictureStylePreset == 4) // Rounded Corner
                        {
                            using (GraphicsPath rPath = CreateShapePath("RoundedRectangle", bounds))
                            {
                                var prevClip = g.Clip;
                                g.SetClip(rPath, CombineMode.Intersect);
                                g.DrawImage(shape.CachedImage, bounds);
                                g.Clip = prevClip;
                            }
                        }
                        else if (shape.PictureStylePreset == 6) // Soft Edge Oval
                        {
                            using (GraphicsPath oPath = CreateShapePath("Oval", bounds))
                            {
                                var prevClip = g.Clip;
                                g.SetClip(oPath, CombineMode.Intersect);
                                g.DrawImage(shape.CachedImage, bounds);
                                g.Clip = prevClip;
                            }
                        }
                        else
                        {
                            g.DrawImage(shape.CachedImage, bounds);
                        }
                    }
                }

                // 3. Outline Border if defined
                if (shape.OutlineWidth > 0 && shape.OutlineColor != Color.Transparent)
                {
                    using (Pen outlinePen = new Pen(shape.OutlineColor, shape.OutlineWidth * zoomLevel))
                    {
                        outlinePen.DashStyle = shape.OutlineDashStyle;
                        if (shape.PictureStylePreset == 4)
                        {
                            using (GraphicsPath rPath = CreateShapePath("RoundedRectangle", bounds)) g.DrawPath(outlinePen, rPath);
                        }
                        else if (shape.PictureStylePreset == 6 || shape.ShapeType == "Circle" || (shape.AspectRatioLock && shape.ShapeType == "Oval"))
                        {
                            using (GraphicsPath oPath = CreateShapePath("Circle", bounds)) g.DrawPath(outlinePen, oPath);
                        }
                        else
                        {
                            g.DrawRectangle(outlinePen, bounds);
                        }
                    }
                }

                // 4. Selection Handles & Floating Layout Popup
                if (shape.IsSelected)
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

                    int rotY = bounds.Top - (int)(16 * zoomLevel);
                    int rotX = bounds.Left + bounds.Width / 2;
                    using (Pen stemPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                    {
                        g.DrawLine(stemPen, rotX, bounds.Top, rotX, rotY);
                    }
                    Rectangle rotR = new Rectangle(rotX - hs / 2, rotY - hs / 2, hs, hs);
                    using (SolidBrush rotBrush = new SolidBrush(Color.FromArgb(0, 200, 83)))
                    using (Pen rotPen = new Pen(Color.White, 1f))
                    {
                        g.FillEllipse(rotBrush, rotR);
                        g.DrawEllipse(rotPen, rotR);
                    }

                    int btnW = (int)(22 * zoomLevel);
                    int btnH = (int)(22 * zoomLevel);
                    int btnX = bounds.Right + 6;
                    int btnY = bounds.Top;
                    Rectangle layoutBtnRect = new Rectangle(btnX, btnY, btnW, btnH);

                    using (SolidBrush btnBg = new SolidBrush(Color.White))
                    using (Pen btnPen = new Pen(Color.FromArgb(200, 205, 215), 1f))
                    {
                        g.FillRectangle(btnBg, layoutBtnRect);
                        g.DrawRectangle(btnPen, layoutBtnRect);
                    }
                    using (Pen archP = new Pen(Color.FromArgb(0, 120, 215), 1.8f))
                    {
                        g.DrawArc(archP, btnX + 3, btnY + 4, btnW - 6, btnH - 8, 200, 140);
                        g.DrawLine(archP, btnX + 4, btnY + btnH - 5, btnX + btnW - 4, btnY + btnH - 5);
                    }

                    if (shape.ShowLayoutOptions)
                    {
                        DrawLayoutOptionsPopup(g, shape, bounds, zoomLevel);
                    }
                }
                return;
            }

            // 1. Draw Drop Shadow if enabled
            if (shape.HasShadow && shape.WrapMode != 5 && shape.FillColor != Color.Transparent)
            {
                using (GraphicsPath sPath = CreateShapePath(shape, new Rectangle(bounds.X + 4, bounds.Y + 4, bounds.Width, bounds.Height)))
                using (SolidBrush shadowB = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                {
                    g.FillPath(shadowB, sPath);
                }
            }

            // 2. Main Shape Path
            using (GraphicsPath path = CreateShapePath(shape, bounds))
            {
                // Fill
                if (shape.FillColor != Color.Transparent)
                {
                    using (SolidBrush fillBrush = new SolidBrush(shape.FillColor))
                    {
                        g.FillPath(fillBrush, path);
                    }
                }

                // Outline
                if (shape.OutlineWidth > 0 && shape.OutlineColor != Color.Transparent)
                {
                    using (Pen outlinePen = new Pen(shape.OutlineColor, shape.OutlineWidth * zoomLevel))
                    {
                        outlinePen.DashStyle = shape.OutlineDashStyle;
                        g.DrawPath(outlinePen, path);
                    }
                }
            }

            // 3. Special Details for Smile Face, Sun, Cube
            if (shape.ShapeType == "SmileFace")
            {
                using (Pen fPen = new Pen(shape.OutlineColor, 1.8f * zoomLevel))
                {
                    int eyeW = (int)(bounds.Width * 0.12f);
                    int eyeH = (int)(bounds.Height * 0.14f);
                    g.FillEllipse(new SolidBrush(shape.OutlineColor), bounds.X + bounds.Width * 0.3f - eyeW/2, bounds.Y + bounds.Height * 0.32f - eyeH/2, eyeW, eyeH);
                    g.FillEllipse(new SolidBrush(shape.OutlineColor), bounds.X + bounds.Width * 0.7f - eyeW/2, bounds.Y + bounds.Height * 0.32f - eyeH/2, eyeW, eyeH);
                    g.DrawArc(fPen, bounds.X + bounds.Width * 0.25f, bounds.Y + bounds.Height * 0.35f, bounds.Width * 0.5f, bounds.Height * 0.45f, 20, 140);
                }
            }

            // 4. Text inside shape
            Rectangle textBounds = new Rectangle(bounds.X + 8, bounds.Y + 6, Math.Max(10, bounds.Width - 16), Math.Max(10, bounds.Height - 12));
            if (string.IsNullOrEmpty(shape.TextContent))
            {
                // Draw Ghost Placeholder text
                if (!string.IsNullOrEmpty(shape.PlaceholderText))
                {
                    using (Font phFont = new Font(shape.FontFamily, shape.FontSize * zoomLevel, FontStyle.Italic))
                    using (SolidBrush phBrush = new SolidBrush(Color.FromArgb(160, 160, 160)))
                    using (StringFormat sf = new StringFormat
                    {
                        Alignment = shape.TextAlignment,
                        LineAlignment = shape.TextLineAlignment,
                        Trimming = StringTrimming.EllipsisWord
                    })
                    {
                        g.DrawString(shape.PlaceholderText, phFont, phBrush, textBounds, sf);
                    }
                }

                // Draw Cursor at start of box if selected
                if (shape.IsSelected)
                {
                    int cX = textBounds.X;
                    int cY = textBounds.Y + 2;
                    int cH = (int)(shape.FontSize * 1.3f * zoomLevel);
                    using (Pen curPen = new Pen(shape.ShapeType == "TextBox" ? Color.Black : shape.TextColor, 1.5f))
                    {
                        g.DrawLine(curPen, cX, cY, cX, cY + cH);
                    }
                }
            }
            else
            {
                FontStyle fStyle = FontStyle.Regular;
                if (shape.Bold) fStyle |= FontStyle.Bold;
                if (shape.Italic) fStyle |= FontStyle.Italic;
                using (Font txtFont = new Font(shape.FontFamily, shape.FontSize * zoomLevel, fStyle))
                using (SolidBrush txtBrush = new SolidBrush(shape.TextColor))
                {
                    var lines = LayoutShapeText(g, shape, textBounds, txtFont, zoomLevel);
                    for (int li = 0; li < lines.Count; li++)
                    {
                        var l = lines[li];
                        if (l.Y + l.Height <= bounds.Bottom + 4)
                        {
                            g.DrawString(l.Text, txtFont, txtBrush, l.X, l.Y, StringFormat.GenericTypographic);
                        }
                    }

                    // Draw Cursor at exact (X, Y) inside shape if selected
                    if (shape.IsSelected)
                    {
                        PointF curPt = GetShapeCursorCoordinates(g, shape, textBounds, txtFont, zoomLevel);
                        int cH = (int)(shape.FontSize * 1.3f * zoomLevel);
                        using (Pen curPen = new Pen(shape.ShapeType == "TextBox" ? Color.Black : shape.TextColor, 1.5f))
                        {
                            g.DrawLine(curPen, (int)curPt.X, (int)curPt.Y, (int)curPt.X, (int)curPt.Y + cH);
                        }
                    }
                }
            }

            // 5. If Selected -> Draw 8 Resize Handles + Rotation Handle + Yellow Adjustment Handles + Floating Layout Options Button
            if (shape.IsSelected)
            {
                // Bounding Selection Outline
                using (Pen selPen = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
                {
                    selPen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(selPen, bounds);
                }

                // 8 Circle Handles
                int hs = (int)(7 * zoomLevel);
                Point[] handles = new Point[]
                {
                    new Point(bounds.Left - hs/2, bounds.Top - hs/2),
                    new Point(bounds.Left + bounds.Width/2 - hs/2, bounds.Top - hs/2),
                    new Point(bounds.Right - hs/2, bounds.Top - hs/2),
                    new Point(bounds.Right - hs/2, bounds.Top + bounds.Height/2 - hs/2),
                    new Point(bounds.Right - hs/2, bounds.Bottom - hs/2),
                    new Point(bounds.Left + bounds.Width/2 - hs/2, bounds.Bottom - hs/2),
                    new Point(bounds.Left - hs/2, bounds.Bottom - hs/2),
                    new Point(bounds.Left - hs/2, bounds.Top + bounds.Height/2 - hs/2)
                };

                for (int h = 0; h < 8; h++)
                {
                    Rectangle hRect = new Rectangle(handles[h].X, handles[h].Y, hs, hs);
                    g.FillEllipse(Brushes.White, hRect);
                    g.DrawEllipse(new Pen(Color.FromArgb(0, 120, 215), 1.2f), hRect);
                }

                // Rotation Handle at Top
                int rotY = bounds.Top - (int)(22 * zoomLevel);
                int rotX = bounds.Left + bounds.Width / 2;
                g.DrawLine(new Pen(Color.FromArgb(0, 120, 215), 1.0f), rotX, bounds.Top, rotX, rotY + hs/2);
                Rectangle rotRect = new Rectangle(rotX - hs/2, rotY, hs, hs);
                g.FillEllipse(Brushes.LightGreen, rotRect);
                g.DrawEllipse(new Pen(Color.FromArgb(0, 120, 215), 1.2f), rotRect);

                // Yellow Adjustment Handles (Grip vàng điều chỉnh góc bo / mũi tên / chú thích)
                Point[] adjHandles = GetAdjustmentHandles(shape, bounds, zoomLevel);
                if (adjHandles != null && adjHandles.Length > 0)
                {
                    int adjSize = Math.Max(7, (int)(8 * zoomLevel));
                    using (SolidBrush yb = new SolidBrush(Color.FromArgb(255, 215, 0))) // Gold Yellow
                    using (Pen yp = new Pen(Color.FromArgb(50, 50, 50), 1.2f))
                    {
                        for (int ai = 0; ai < adjHandles.Length; ai++)
                        {
                            Point ap = adjHandles[ai];
                            PointF[] diamond = new PointF[]
                            {
                                new PointF(ap.X, ap.Y - adjSize / 2f),
                                new PointF(ap.X + adjSize / 2f, ap.Y),
                                new PointF(ap.X, ap.Y + adjSize / 2f),
                                new PointF(ap.X - adjSize / 2f, ap.Y)
                            };
                            g.FillPolygon(yb, diamond);
                            g.DrawPolygon(yp, diamond);
                        }
                    }
                }

                // Floating Layout Options Button at Top-Right
                Rectangle wrapBtn = new Rectangle(bounds.Right + 5, bounds.Top, 22, 22);
                g.FillRectangle(Brushes.White, wrapBtn);
                g.DrawRectangle(new Pen(Color.FromArgb(0, 120, 215), 1.2f), wrapBtn);
                using (Pen iconPen = new Pen(Color.FromArgb(0, 120, 215), 1.5f))
                {
                    g.DrawArc(iconPen, wrapBtn.X + 4, wrapBtn.Y + 5, 14, 10, 180, 180);
                    g.DrawLine(iconPen, wrapBtn.X + 4, wrapBtn.Y + 14, wrapBtn.X + 18, wrapBtn.Y + 14);
                    g.DrawLine(iconPen, wrapBtn.X + 4, wrapBtn.Y + 17, wrapBtn.X + 14, wrapBtn.Y + 17);
                }
            }
        }

        public static void DrawLayoutOptionsPopup(D2DGraphics g, DocShapeObject shape, Rectangle bounds, float zoomLevel)
        {
            if (shape == null || !shape.ShowLayoutOptions) return;

            Rectangle wrapBtn = new Rectangle(bounds.Right + 5, bounds.Top, 22, 22);
            int popW = 185; int popH = 275;
            int popX = wrapBtn.Right + 8; int popY = wrapBtn.Top - 5;
            Rectangle popRect = new Rectangle(popX, popY, popW, popH);

            g.FillRectangle(Brushes.White, popX, popY, popW, popH);
            using (Pen popBorder = new Pen(Color.FromArgb(0, 120, 215), 1.2f))
            {
                g.DrawRectangle(popBorder, popX, popY, popW, popH);
                Point[] pointerPts = new Point[] { new Point(popX, popY + 12), new Point(popX - 6, popY + 18), new Point(popX, popY + 24) };
                g.FillPolygon(Brushes.White, pointerPts);
                g.DrawLine(popBorder, popX, popY + 12, popX - 6, popY + 18);
                g.DrawLine(popBorder, popX - 6, popY + 18, popX, popY + 24);
            }

            using (Font headerFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font sectionFont = new Font("Segoe UI", 8.0f, FontStyle.Bold))
            using (Font itemFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font linkFont = new Font("Segoe UI", 8.5f, FontStyle.Underline))
            {
                g.DrawString("Tùy chọn bố trí", headerFont, new SolidBrush(Color.FromArgb(0, 95, 184)), popX + 12, popY + 10);
                using (Pen closePen = new Pen(Color.FromArgb(100, 100, 100), 1.5f))
                {
                    g.DrawLine(closePen, popX + popW - 20, popY + 12, popX + popW - 10, popY + 22);
                    g.DrawLine(closePen, popX + popW - 10, popY + 12, popX + popW - 20, popY + 22);
                }

                Rectangle sec1Bar = new Rectangle(popX + 10, popY + 36, popW - 20, 18);
                using (SolidBrush secBarBrush = new SolidBrush(Color.FromArgb(236, 238, 241))) g.FillRectangle(secBarBrush, sec1Bar);
                g.DrawString("Cùng dòng với văn bản", sectionFont, new SolidBrush(Color.FromArgb(85, 90, 95)), sec1Bar.X + 6, sec1Bar.Y + 2);

                Rectangle inlineCard = new Rectangle(popX + 14, popY + 58, 42, 34);
                DrawVectorWrapCard(g, 0, inlineCard, shape.WrapMode == 0);

                Rectangle sec2Bar = new Rectangle(popX + 10, popY + 98, popW - 20, 18);
                using (SolidBrush secBarBrush = new SolidBrush(Color.FromArgb(236, 238, 241))) g.FillRectangle(secBarBrush, sec2Bar);
                g.DrawString("Ngắt dòng văn bản", sectionFont, new SolidBrush(Color.FromArgb(85, 90, 95)), sec2Bar.X + 6, sec2Bar.Y + 2);

                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int wIdx = r == 0 ? (1 + c) : (4 + c);
                        Rectangle cardBox = new Rectangle(popX + 14 + c * 52, popY + 120 + r * 38, 42, 34);
                        DrawVectorWrapCard(g, wIdx, cardBox, shape.WrapMode == wIdx);
                    }
                }

                int r1Y = popY + 202;
                DrawVectorRadioButton(g, popX + 14, r1Y, !shape.FixPosition, "Di chuyển cùng văn bản", itemFont);
                DrawVectorInfoIcon(g, popX + popW - 26, r1Y + 2);

                int r2Y = popY + 226;
                DrawVectorRadioButton(g, popX + 14, r2Y, shape.FixPosition, "Cố định vị trí trên trang", itemFont);
                DrawVectorInfoIcon(g, popX + popW - 26, r2Y + 2);

                // See more... link
                using (SolidBrush linkBrush = new SolidBrush(Color.FromArgb(0, 95, 184)))
                {
                    g.DrawString("Xem thêm...", linkFont, linkBrush, popX + popW - 85, popY + 252);
                }
            }
        }

        public static void DrawVectorWrapCard(D2DGraphics g, int wrapMode, Rectangle box, bool isSelected)
        {
            if (isSelected)
            {
                using (SolidBrush selBg = new SolidBrush(Color.FromArgb(215, 220, 225)))
                {
                    g.FillRectangle(selBg, box);
                }
                using (Pen selBorder = new Pen(Color.FromArgb(120, 125, 130), 1.2f))
                {
                    g.DrawRectangle(selBorder, box);
                }
            }
            else
            {
                using (Pen borderPen = new Pen(Color.FromArgb(218, 224, 230), 1.0f))
                {
                    g.DrawRectangle(borderPen, box);
                }
            }

            using (Pen bluePen = new Pen(Color.FromArgb(0, 120, 215), 1.6f))
            using (Pen darkArchPen = new Pen(Color.FromArgb(40, 40, 40), 3.0f))
            using (Pen ghostArchPen = new Pen(Color.FromArgb(180, 185, 192), 3.0f))
            {
                float archX = box.X + (box.Width - 14) / 2.0f;
                float archY = box.Y + (box.Height - 12) / 2.0f + 1;

                if (wrapMode == 0) // In Line With Text
                {
                    float ax = box.X + 6;
                    float ay = box.Y + 12;
                    g.DrawLine(bluePen, box.X + 5, box.Y + 7, box.X + 22, box.Y + 7);
                    DrawVectorArch(g, darkArchPen, ax, ay, 13, 12);
                    g.DrawLine(bluePen, box.X + 22, box.Y + 17, box.X + 36, box.Y + 17);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 26, box.X + 36, box.Y + 26);
                }
                else if (wrapMode == 1) // Square (Vuông)
                {
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 13, box.X + 11, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 31, box.Y + 13, box.X + 37, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 20, box.X + 11, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 31, box.Y + 20, box.X + 37, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                    DrawVectorArch(g, darkArchPen, archX, archY, 13, 12);
                }
                else if (wrapMode == 2) // Tight (Chặt)
                {
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 13, box.X + 12, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 30, box.Y + 13, box.X + 37, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 20, box.X + 11, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 31, box.Y + 20, box.X + 37, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                    DrawVectorArch(g, darkArchPen, archX, archY, 13, 12);
                }
                else if (wrapMode == 3) // Through (Xuyên qua)
                {
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 13, box.X + 13, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 29, box.Y + 13, box.X + 37, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 20, box.X + 11, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 31, box.Y + 20, box.X + 37, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                    DrawVectorArch(g, darkArchPen, archX, archY, 13, 12);
                }
                else if (wrapMode == 4) // Top and Bottom (Trên và Dưới)
                {
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                    DrawVectorArch(g, darkArchPen, archX, archY, 13, 12);
                }
                else if (wrapMode == 5) // Behind Text (Dưới văn bản)
                {
                    DrawVectorArch(g, ghostArchPen, archX, archY, 13, 12);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 13, box.X + 37, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 20, box.X + 37, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                }
                else if (wrapMode == 6) // In Front of Text (Trước văn bản)
                {
                    g.DrawLine(bluePen, box.X + 5, box.Y + 6, box.X + 37, box.Y + 6);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 13, box.X + 37, box.Y + 13);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 20, box.X + 37, box.Y + 20);
                    g.DrawLine(bluePen, box.X + 5, box.Y + 27, box.X + 37, box.Y + 27);
                    DrawVectorArch(g, darkArchPen, archX, archY, 13, 12);
                }
            }
        }

        public static void DrawVectorRadioButton(D2DGraphics g, int x, int y, bool isChecked, string label, Font font)
        {
            Rectangle rCircle = new Rectangle(x, y + 2, 12, 12);
            using (Pen rPen = new Pen(isChecked ? Color.FromArgb(0, 120, 215) : Color.FromArgb(140, 145, 150), 1.2f))
            {
                g.DrawEllipse(rPen, rCircle);
                if (isChecked)
                {
                    using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                    {
                        g.FillEllipse(dotBrush, x + 3, y + 5, 6, 6);
                    }
                }
            }
            g.DrawString(label, font, isChecked ? new SolidBrush(Color.FromArgb(20, 20, 20)) : new SolidBrush(Color.FromArgb(70, 70, 70)), x + 16, y);
        }

        public static void DrawVectorInfoIcon(D2DGraphics g, int x, int y)
        {
            Rectangle infoCircle = new Rectangle(x, y, 11, 11);
            using (Pen infoPen = new Pen(Color.FromArgb(0, 120, 215), 1.0f))
            using (Font iFont = new Font("Segoe UI", 6.5f, FontStyle.Bold))
            {
                g.DrawEllipse(infoPen, infoCircle);
                g.DrawString("i", iFont, new SolidBrush(Color.FromArgb(0, 120, 215)), x + 2.5f, y - 0.5f);
            }
        }

        public static void DrawVectorArch(D2DGraphics g, Pen archPen, float x, float y, float w, float h)
        {
            if (g == null || archPen == null) return;
            g.DrawArc(archPen, x, y, w, h, 180, 180);
            g.DrawLine(archPen, x, y + h / 2, x + w, y + h / 2);
        }

        public struct ShapeLineInfo
        {
            public string Text;
            public int StartIndex;
            public int Length;
            public float X;
            public float Y;
            public float Width;
            public float Height;
        }

        public static System.Collections.Generic.List<ShapeLineInfo> LayoutShapeText(D2DGraphics g, DocShapeObject shape, Rectangle textBounds, Font font, float zoomLevel)
        {
            var lines = new System.Collections.Generic.List<ShapeLineInfo>();
            if (string.IsNullOrEmpty(shape.TextContent)) return lines;

            string fullText = shape.TextContent;
            float maxW = Math.Max(20, textBounds.Width);
            float lineH = font.GetHeight(g);
            if (lineH < 12) lineH = (shape.FontSize * 1.35f * zoomLevel);

            string[] paragraphs = fullText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            int charIndex = 0;
            float curY = textBounds.Y + 2;

            for (int p = 0; p < paragraphs.Length; p++)
            {
                string para = paragraphs[p];
                if (para.Length == 0)
                {
                    lines.Add(new ShapeLineInfo { Text = "", StartIndex = charIndex, Length = 0, X = textBounds.X, Y = curY, Width = 0, Height = lineH });
                    curY += lineH;
                    charIndex += 1;
                    continue;
                }

                if (!shape.WordWrap)
                {
                    // WordWrap == false: Do not wrap across lines
                    SizeF sz = g.MeasureString(para, font, PointF.Empty, StringFormat.GenericTypographic);
                    float lineX = textBounds.X;
                    if (shape.TextAlignment == StringAlignment.Center)
                    {
                        lineX = textBounds.X + Math.Max(0, (textBounds.Width - sz.Width) / 2f);
                    }
                    else if (shape.TextAlignment == StringAlignment.Far)
                    {
                        lineX = textBounds.Right - sz.Width;
                    }

                    lines.Add(new ShapeLineInfo
                    {
                        Text = para,
                        StartIndex = charIndex,
                        Length = para.Length,
                        X = lineX,
                        Y = curY,
                        Width = sz.Width,
                        Height = lineH
                    });
                    curY += lineH;
                }
                else
                {
                    // WordWrap == true: Wrap lines on word boundaries
                    int pIndex = 0;
                    while (pIndex < para.Length)
                    {
                        int fitCount = 0;
                        for (int len = 1; len <= para.Length - pIndex; len++)
                        {
                            string sub = para.Substring(pIndex, len);
                            SizeF sz = g.MeasureString(sub, font, PointF.Empty, StringFormat.GenericTypographic);
                            if (sz.Width <= maxW || len == 1)
                            {
                                fitCount = len;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (pIndex + fitCount < para.Length)
                        {
                            int lastSpace = para.LastIndexOf(' ', pIndex + fitCount - 1, fitCount);
                            if (lastSpace >= pIndex)
                            {
                                fitCount = (lastSpace - pIndex) + 1;
                            }
                        }

                        string lineStr = para.Substring(pIndex, fitCount);
                        SizeF lineSz = g.MeasureString(lineStr, font, PointF.Empty, StringFormat.GenericTypographic);
                        float lineX = textBounds.X;
                        if (shape.TextAlignment == StringAlignment.Center)
                        {
                            lineX = textBounds.X + Math.Max(0, (textBounds.Width - lineSz.Width) / 2f);
                        }
                        else if (shape.TextAlignment == StringAlignment.Far)
                        {
                            lineX = textBounds.Right - lineSz.Width;
                        }

                        lines.Add(new ShapeLineInfo
                        {
                            Text = lineStr,
                            StartIndex = charIndex + pIndex,
                            Length = fitCount,
                            X = lineX,
                            Y = curY,
                            Width = lineSz.Width,
                            Height = lineH
                        });

                        curY += lineH;
                        pIndex += fitCount;
                    }
                }

                charIndex += para.Length + 1;
            }

            // Apply Vertical Alignment Offset (Top, Middle, Bottom)
            float totalH = lines.Count * lineH;
            float vOffset = 0;
            if (shape.TextLineAlignment == StringAlignment.Center)
            {
                vOffset = Math.Max(0, (textBounds.Height - totalH) / 2f);
            }
            else if (shape.TextLineAlignment == StringAlignment.Far)
            {
                vOffset = Math.Max(0, textBounds.Height - totalH - 4);
            }

            if (vOffset > 0)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    ShapeLineInfo info = lines[i];
                    info.Y += vOffset;
                    lines[i] = info;
                }
            }

            return lines;
        }

        public static PointF GetShapeCursorCoordinates(D2DGraphics g, DocShapeObject shape, Rectangle textBounds, Font font, float zoomLevel)
        {
            if (string.IsNullOrEmpty(shape.TextContent))
            {
                float curX = textBounds.X;
                if (shape.TextAlignment == StringAlignment.Center) curX = textBounds.X + textBounds.Width / 2f;
                else if (shape.TextAlignment == StringAlignment.Far) curX = textBounds.Right - 4;

                float curY = textBounds.Y + 2;
                float lineH = font.GetHeight(g);
                if (lineH < 12) lineH = (shape.FontSize * 1.35f * zoomLevel);
                if (shape.TextLineAlignment == StringAlignment.Center) curY = textBounds.Y + Math.Max(0, (textBounds.Height - lineH) / 2f);
                else if (shape.TextLineAlignment == StringAlignment.Far) curY = textBounds.Bottom - lineH - 4;

                return new PointF(curX, curY);
            }

            int targetIndex = Math.Max(0, Math.Min(shape.TextCursorIndex, shape.TextContent.Length));
            var lines = LayoutShapeText(g, shape, textBounds, font, zoomLevel);
            if (lines.Count == 0)
            {
                return new PointF(textBounds.X, textBounds.Y + 2);
            }

            ShapeLineInfo targetLine = lines[lines.Count - 1];
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                if (targetIndex >= l.StartIndex && targetIndex <= l.StartIndex + l.Length)
                {
                    targetLine = l;
                    break;
                }
            }

            int offsetInLine = Math.Max(0, targetIndex - targetLine.StartIndex);
            string sub = targetLine.Text.Substring(0, Math.Min(offsetInLine, targetLine.Text.Length));
            SizeF subSz = g.MeasureString(sub, font, PointF.Empty, StringFormat.GenericTypographic);
            float curXPos = targetLine.X + subSz.Width;

            return new PointF(curXPos, targetLine.Y);
        }

        public static Point[] GetAdjustmentHandles(DocShapeObject shape, Rectangle bounds, float zoomLevel)
        {
            if (shape == null || bounds.Width <= 0 || bounds.Height <= 0) return new Point[0];

            string sType = shape.ShapeType ?? "Rectangle";
            if (sType == "RoundedRectangle" || sType == "RoundSingleCorner" || sType == "RoundSameSideCorner" || sType == "RoundDiagonalCorner" || sType == "SnippedAndRounded")
            {
                float rRatio = (shape.AdjustValue1 > 0.01f && shape.AdjustValue1 <= 0.5f) ? shape.AdjustValue1 : 0.2f;
                int shapeW = shape.BaseWidth + shape.WidthDelta;
                int shapeH = shape.BaseHeight + shape.HeightDelta;
                int baseDim = (shapeH > 0 && shapeW > 0) ? (int)(Math.Min(shapeW, shapeH) * zoomLevel) : Math.Min(bounds.Width, bounds.Height);
                int rPx = (int)(baseDim * rRatio);
                return new Point[] { new Point(bounds.Left + rPx, bounds.Top) };
            }
            else if (sType == "RightArrow")
            {
                float neckRatio = (shape.AdjustValue1 > 0.05f && shape.AdjustValue1 < 0.48f) ? shape.AdjustValue1 : 0.25f;
                float headRatio = (shape.AdjustValue2 > 0.1f && shape.AdjustValue2 < 0.7f) ? shape.AdjustValue2 : 0.4f;
                int neckY = bounds.Top + (int)(bounds.Height * neckRatio);
                int headX = bounds.Left + (int)(bounds.Width * (1.0f - headRatio));
                return new Point[] { new Point(headX, neckY), new Point(headX, bounds.Top) };
            }
            else if (sType == "LeftArrow")
            {
                float neckRatio = (shape.AdjustValue1 > 0.05f && shape.AdjustValue1 < 0.48f) ? shape.AdjustValue1 : 0.25f;
                float headRatio = (shape.AdjustValue2 > 0.1f && shape.AdjustValue2 < 0.7f) ? shape.AdjustValue2 : 0.4f;
                int neckY = bounds.Top + (int)(bounds.Height * neckRatio);
                int headX = bounds.Left + (int)(bounds.Width * headRatio);
                return new Point[] { new Point(headX, neckY), new Point(headX, bounds.Top) };
            }
            else if (sType.StartsWith("Callout"))
            {
                float tXRatio = (shape.AdjustValue2 > 0.05f && shape.AdjustValue2 < 0.95f) ? shape.AdjustValue2 : 0.25f;
                float tYRatio = (shape.AdjustValue3 > 0.8f && shape.AdjustValue3 < 1.6f) ? shape.AdjustValue3 : 1.25f;
                return new Point[] { new Point(bounds.Left + (int)(bounds.Width * tXRatio), bounds.Top + (int)(bounds.Height * tYRatio)) };
            }

            return new Point[0];
        }

        public static int HitTestAdjustmentHandle(DocShapeObject shape, Rectangle bounds, Point mousePt, float zoomLevel)
        {
            Point[] handles = GetAdjustmentHandles(shape, bounds, zoomLevel);
            int hitRad = Math.Max(6, (int)(8 * zoomLevel));
            for (int i = 0; i < handles.Length; i++)
            {
                int dx = mousePt.X - handles[i].X;
                int dy = mousePt.Y - handles[i].Y;
                if (dx * dx + dy * dy <= hitRad * hitRad) return i;
            }
            return -1;
        }

        public static void ApplyAdjustmentDrag(DocShapeObject shape, int handleIndex, Point mousePt, Rectangle bounds, float zoomLevel)
        {
            if (shape == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            string sType = shape.ShapeType ?? "Rectangle";
            if (sType == "RoundedRectangle" || sType == "RoundSingleCorner" || sType == "RoundSameSideCorner" || sType == "RoundDiagonalCorner" || sType == "SnippedAndRounded")
            {
                int shapeW = shape.BaseWidth + shape.WidthDelta;
                int shapeH = shape.BaseHeight + shape.HeightDelta;
                float baseDim = (shapeH > 0 && shapeW > 0) ? (Math.Min(shapeW, shapeH) * zoomLevel) : Math.Min(bounds.Width, bounds.Height);
                if (baseDim <= 0) baseDim = Math.Min(bounds.Width, bounds.Height);
                float r = (mousePt.X - bounds.Left) / baseDim;
                shape.AdjustValue1 = Math.Max(0.02f, Math.Min(0.5f, r));
            }
            else if (sType == "RightArrow")
            {
                if (handleIndex == 0)
                {
                    float neck = (mousePt.Y - bounds.Top) / (float)bounds.Height;
                    shape.AdjustValue1 = Math.Max(0.05f, Math.Min(0.45f, neck));
                }
                else if (handleIndex == 1)
                {
                    float head = (bounds.Right - mousePt.X) / (float)bounds.Width;
                    shape.AdjustValue2 = Math.Max(0.1f, Math.Min(0.7f, head));
                }
            }
            else if (sType == "LeftArrow")
            {
                if (handleIndex == 0)
                {
                    float neck = (mousePt.Y - bounds.Top) / (float)bounds.Height;
                    shape.AdjustValue1 = Math.Max(0.05f, Math.Min(0.45f, neck));
                }
                else if (handleIndex == 1)
                {
                    float head = (mousePt.X - bounds.Left) / (float)bounds.Width;
                    shape.AdjustValue2 = Math.Max(0.1f, Math.Min(0.7f, head));
                }
            }
            else if (sType.StartsWith("Callout"))
            {
                shape.AdjustValue2 = (mousePt.X - bounds.Left) / (float)bounds.Width;
                shape.AdjustValue3 = (mousePt.Y - bounds.Top) / (float)bounds.Height;
            }
        }

        public static int GetCharIndexFromPoint(D2DGraphics g, DocShapeObject shape, Rectangle textBounds, Font font, float zoomLevel, int clickX, int clickY)
        {
            if (string.IsNullOrEmpty(shape.TextContent)) return 0;
            var lines = LayoutShapeText(g, shape, textBounds, font, zoomLevel);
            if (lines.Count == 0) return 0;

            ShapeLineInfo bestLine = lines[0];
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                if (clickY >= l.Y && clickY <= l.Y + l.Height)
                {
                    bestLine = l;
                    break;
                }
                if (clickY > l.Y)
                {
                    bestLine = l;
                }
            }

            int bestIdx = bestLine.StartIndex;
            float bestDist = float.MaxValue;
            for (int k = 0; k <= bestLine.Text.Length; k++)
            {
                string sub = bestLine.Text.Substring(0, k);
                SizeF sz = g.MeasureString(sub, font, PointF.Empty, StringFormat.GenericTypographic);
                float x = bestLine.X + sz.Width;
                float dist = Math.Abs(clickX - x);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = bestLine.StartIndex + k;
                }
            }

            return Math.Max(0, Math.Min(bestIdx, shape.TextContent.Length));
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

        private static GraphicsPath CreateTopRoundedPath(Rectangle rect, int radius)
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
            path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
            path.CloseFigure();
            return path;
        }

        public static Image CreateDefaultSamplePhoto(int width, int height)
        {
            int w = Math.Max(480, width);
            int h = Math.Max(320, height);
            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;

                // 1. Sky Gradient
                using (LinearGradientBrush skyB = new LinearGradientBrush(new Rectangle(0, 0, w, (int)(h * 0.65f)), Color.FromArgb(56, 128, 220), Color.FromArgb(248, 196, 140), 90f))
                {
                    g.FillRectangle(skyB, 0, 0, w, (int)(h * 0.65f));
                }

                // 2. Glowing Sun
                int sunD = (int)(h * 0.22f);
                int sunX = (int)(w * 0.72f);
                int sunY = (int)(h * 0.16f);
                using (SolidBrush sunGlow = new SolidBrush(Color.FromArgb(80, 255, 240, 180)))
                {
                    g.FillEllipse(sunGlow, sunX - 14, sunY - 14, sunD + 28, sunD + 28);
                }
                using (SolidBrush sunB = new SolidBrush(Color.FromArgb(255, 235, 140)))
                {
                    g.FillEllipse(sunB, sunX, sunY, sunD, sunD);
                }

                // 3. Distant Mountains (Purple-Blue)
                PointF[] distantMtn = new PointF[] {
                    new PointF(0, h * 0.62f),
                    new PointF(w * 0.20f, h * 0.28f),
                    new PointF(w * 0.44f, h * 0.50f),
                    new PointF(w * 0.68f, h * 0.22f),
                    new PointF(w * 0.88f, h * 0.42f),
                    new PointF(w, h * 0.35f),
                    new PointF(w, h * 0.65f),
                    new PointF(0, h * 0.65f)
                };
                using (LinearGradientBrush mtnB1 = new LinearGradientBrush(new Rectangle(0, (int)(h * 0.22f), w, (int)(h * 0.45f)), Color.FromArgb(115, 130, 185), Color.FromArgb(60, 75, 125), 90f))
                {
                    g.FillPolygon(mtnB1, distantMtn);
                }

                // 4. Foreground Green Hills & Mountain Peaks
                PointF[] foreMtn = new PointF[] {
                    new PointF(0, h * 0.65f),
                    new PointF(0, h * 0.48f),
                    new PointF(w * 0.34f, h * 0.36f),
                    new PointF(w * 0.58f, h * 0.58f),
                    new PointF(w * 0.82f, h * 0.42f),
                    new PointF(w, h * 0.52f),
                    new PointF(w, h * 0.65f)
                };
                using (LinearGradientBrush mtnB2 = new LinearGradientBrush(new Rectangle(0, (int)(h * 0.36f), w, (int)(h * 0.3f)), Color.FromArgb(40, 125, 75), Color.FromArgb(20, 65, 40), 90f))
                {
                    g.FillPolygon(mtnB2, foreMtn);
                }

                // Snow Cap on high peak
                PointF[] snow1 = new PointF[] {
                    new PointF(w * 0.34f, h * 0.36f),
                    new PointF(w * 0.29f, h * 0.42f),
                    new PointF(w * 0.39f, h * 0.42f)
                };
                g.FillPolygon(Brushes.White, snow1);

                // 5. Sparkling Water / Lake with reflections
                using (LinearGradientBrush lakeB = new LinearGradientBrush(new Rectangle(0, (int)(h * 0.64f), w, (int)(h * 0.36f)), Color.FromArgb(45, 135, 195), Color.FromArgb(12, 55, 105), 90f))
                {
                    g.FillRectangle(lakeB, 0, (int)(h * 0.64f), w, (int)(h * 0.36f));
                }

                // Water Shimmer Lines
                using (Pen shimP = new Pen(Color.FromArgb(130, 255, 255, 255), 1.6f))
                {
                    for (int li = 0; li < 6; li++)
                    {
                        float yL = h * (0.68f + li * 0.05f);
                        float xStart = (li % 2 == 0) ? w * 0.15f : w * 0.35f;
                        float xEnd = xStart + w * 0.45f;
                        g.DrawLine(shimP, xStart, yL, xEnd, yL);
                    }
                }

                // 6. Pine Trees on Bank
                for (int t = 0; t < 5; t++)
                {
                    float tx = w * (0.06f + t * 0.06f);
                    float ty = h * (0.64f + (t % 2) * 0.03f);
                    float th = h * 0.18f;
                    float tw = w * 0.045f;
                    PointF[] tree = new PointF[] {
                        new PointF(tx, ty - th),
                        new PointF(tx - tw / 2, ty),
                        new PointF(tx + tw / 2, ty)
                    };
                    using (SolidBrush treeB = new SolidBrush(Color.FromArgb(12, 45, 25)))
                    {
                        g.FillPolygon(treeB, tree);
                    }
                }
            }
            return bmp;
        }
    }
}
