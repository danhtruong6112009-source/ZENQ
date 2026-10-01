#include "SlidesView.hpp"
#include <vector>
#include <cmath>

namespace GenOffice {

SlidesView::SlidesView() {
    // Add sample slide shapes
    if (!presentation.slides.empty()) {
        ShapeNode shape1;
        shape1.type = ShapeType::Rectangle;
        shape1.transform = {300.0f, 600.0f, 400.0f, 250.0f, 0.0f};
        shape1.fillColor = 0xD24726; // Orange-Red
        shape1.strokeColor = 0xC43E1C;
        shape1.text = "GenOffice Slide Shape";
        shape1.textColor = 0xFFFFFF;

        presentation.slides[0].addShape(shape1);
    }
}

void SlidesView::addSlide() {
    SlideNode newSlide;
    newSlide.title = "Slide " + std::to_string(presentation.slides.size() + 1);

    ShapeNode titleShape;
    titleShape.type = ShapeType::TextBox;
    titleShape.transform = {360.0f, 250.0f, 1200.0f, 150.0f, 0.0f};
    titleShape.text = "New Slide Title";
    titleShape.fontSize = 44.0f;
    titleShape.fillColor = 0xFFFFFF;
    titleShape.strokeWidth = 0.0f;
    titleShape.textColor = 0x222222;

    newSlide.addShape(titleShape);
    presentation.slides.push_back(newSlide);
    activeSlideIndex = presentation.slides.size() - 1;
}

void SlidesView::addShape(ShapeType type) {
    if (activeSlideIndex < presentation.slides.size()) {
        ShapeNode shape;
        shape.type = type;
        shape.transform = {500.0f, 400.0f, 300.0f, 200.0f, 0.0f};
        shape.fillColor = 0x2B579A;
        shape.strokeColor = 0x106EBE;
        shape.text = (type == ShapeType::TextBox) ? "Text Box" : "New Shape";
        shape.textColor = 0xFFFFFF;

        presentation.slides[activeSlideIndex].addShape(shape);
        selectedShapeIndex = (int)presentation.slides[activeSlideIndex].shapes.size() - 1;
    }
}

void SlidesView::drawThumbnailSidebar(HDC hdc, int height) {
    int sidebarW = 200;
    RECT sbRect = {0, 125, sidebarW, height - 28};
    HBRUSH sbBrush = CreateSolidBrush(RGB(240, 240, 240));
    FillRect(hdc, &sbRect, sbBrush);
    DeleteObject(sbBrush);

    HPEN sepPen = CreatePen(PS_SOLID, 1, RGB(215, 215, 215));
    HPEN oldPen = (HPEN)SelectObject(hdc, sepPen);
    MoveToEx(hdc, sidebarW, 125, NULL);
    LineTo(hdc, sidebarW, height - 28);

    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, RGB(40, 40, 40));
    HFONT hFont = CreateFontW(13, 0, 0, 0, FW_SEMIBOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_GUI_FONT, L"Segoe UI");
    HFONT hOldFont = (HFONT)SelectObject(hdc, hFont);

    // "+ Slide Mới" Button
    RECT btnRect = {12, 135, sidebarW - 12, 165};
    HBRUSH btnBrush = CreateSolidBrush(RGB(210, 71, 38));
    FillRect(hdc, &btnRect, btnBrush);
    DeleteObject(btnBrush);

    SetTextColor(hdc, RGB(255, 255, 255));
    TextOutW(hdc, 40, 142, L"+ Slide Mới", 11);

    // Render Slide Thumbnails
    int thumbY = 180;
    for (size_t i = 0; i < presentation.slides.size(); ++i) {
        int thumbW = 160;
        int thumbH = 90;
        int thumbX = 20;

        RECT frameRect = {thumbX, thumbY, thumbX + thumbW, thumbY + thumbH};
        HBRUSH frameBrush = CreateSolidBrush((i == activeSlideIndex) ? RGB(255, 255, 255) : RGB(230, 230, 230));
        FillRect(hdc, &frameRect, frameBrush);
        DeleteObject(frameBrush);

        HPEN borderPen = CreatePen(PS_SOLID, (i == activeSlideIndex) ? 2 : 1, (i == activeSlideIndex) ? RGB(210, 71, 38) : RGB(200, 200, 200));
        HPEN oldBorderPen = (HPEN)SelectObject(hdc, borderPen);
        HBRUSH nullBrush = (HBRUSH)GetStockObject(NULL_BRUSH);
        HBRUSH oldBrush = (HBRUSH)SelectObject(hdc, nullBrush);

        Rectangle(hdc, frameRect.left, frameRect.top, frameRect.right, frameRect.bottom);

        SelectObject(hdc, oldBrush);
        SelectObject(hdc, oldBorderPen);
        DeleteObject(borderPen);

        SetTextColor(hdc, (i == activeSlideIndex) ? RGB(210, 71, 38) : RGB(80, 80, 80));
        std::wstring wSlideNum = L"Slide " + std::to_wstring(i + 1);
        TextOutW(hdc, thumbX + 8, thumbY + 36, wSlideNum.c_str(), (int)wSlideNum.length());

        thumbY += thumbH + 20;
    }

    SelectObject(hdc, hOldFont);
    DeleteObject(hFont);
    SelectObject(hdc, oldPen);
    DeleteObject(sepPen);
}

void SlidesView::drawTransformHandles(HDC hdc, int x, int y, int w, int h) {
    // Outer Bounding Box dashed border
    HPEN boxPen = CreatePen(PS_DASH, 1, RGB(0, 120, 215));
    HPEN oldPen = (HPEN)SelectObject(hdc, boxPen);
    HBRUSH nullBrush = (HBRUSH)GetStockObject(NULL_BRUSH);
    HBRUSH oldBrush = (HBRUSH)SelectObject(hdc, nullBrush);

    Rectangle(hdc, x, y, x + w, y + h);

    // 8 Transform Handle Dots (Corners + Edges)
    HBRUSH dotBrush = CreateSolidBrush(RGB(255, 255, 255));
    HPEN dotPen = CreatePen(PS_SOLID, 1, RGB(0, 120, 215));
    SelectObject(hdc, dotBrush);
    SelectObject(hdc, dotPen);

    int handles[8][2] = {
        {x, y}, {x + w / 2, y}, {x + w, y},
        {x + w, y + h / 2}, {x + w, y + h}, {x + w / 2, y + h},
        {x, y + h}, {x, y + h / 2}
    };

    for (int i = 0; i < 8; ++i) {
        Rectangle(hdc, handles[i][0] - 4, handles[i][1] - 4, handles[i][0] + 4, handles[i][1] + 4);
    }

    // Top Rotation Handle Knob
    MoveToEx(hdc, x + w / 2, y, NULL);
    LineTo(hdc, x + w / 2, y - 20);
    Ellipse(hdc, x + w / 2 - 5, y - 25, x + w / 2 + 5, y - 15);

    SelectObject(hdc, oldBrush);
    SelectObject(hdc, oldPen);
    DeleteObject(dotBrush);
    DeleteObject(dotPen);
    DeleteObject(boxPen);
}

void SlidesView::draw(HDC hdc, int width, int height) {
    // 1. Left Thumbnail Sidebar
    drawThumbnailSidebar(hdc, height);

    // 2. Main Slide Canvas Area
    int sidebarW = 200;
    RECT canvasBg = {sidebarW, 125, width, height - 28};
    HBRUSH bgBrush = CreateSolidBrush(RGB(225, 225, 225));
    FillRect(hdc, &canvasBg, bgBrush);
    DeleteObject(bgBrush);

    // Compute Slide Canvas dimensions (16:9 ratio)
    int slideW = static_cast<int>(960 * zoomLevel);
    int slideH = static_cast<int>(540 * zoomLevel);
    int slideX = sidebarW + (width - sidebarW - slideW) / 2;
    int slideY = 125 + (height - 125 - 28 - slideH) / 2;

    // Slide Shadow
    RECT shadowRect = {slideX + 6, slideY + 6, slideX + slideW + 6, slideY + slideH + 6};
    HBRUSH shadowBrush = CreateSolidBrush(RGB(190, 190, 190));
    FillRect(hdc, &shadowRect, shadowBrush);
    DeleteObject(shadowBrush);

    // Slide White Canvas
    RECT slideRect = {slideX, slideY, slideX + slideW, slideY + slideH};
    HBRUSH paperBrush = CreateSolidBrush(RGB(255, 255, 255));
    FillRect(hdc, &slideRect, paperBrush);
    DeleteObject(paperBrush);

    // Render Active Slide Shapes
    if (activeSlideIndex < presentation.slides.size()) {
        const auto& slide = presentation.slides[activeSlideIndex];
        float scale = slideW / presentation.canvasWidth;

        SetBkMode(hdc, TRANSPARENT);

        for (size_t sIdx = 0; sIdx < slide.shapes.size(); ++sIdx) {
            const auto& shape = slide.shapes[sIdx];
            int shapeX = slideX + static_cast<int>(shape.transform.x * scale);
            int shapeY = slideY + static_cast<int>(shape.transform.y * scale);
            int shapeW = static_cast<int>(shape.transform.width * scale);
            int shapeH = static_cast<int>(shape.transform.height * scale);

            COLORREF fillColor = RGB((shape.fillColor >> 16) & 0xFF, (shape.fillColor >> 8) & 0xFF, shape.fillColor & 0xFF);
            COLORREF strokeColor = RGB((shape.strokeColor >> 16) & 0xFF, (shape.strokeColor >> 8) & 0xFF, shape.strokeColor & 0xFF);

            HBRUSH shapeBrush = CreateSolidBrush(fillColor);
            HPEN shapePen = CreatePen(PS_SOLID, static_cast<int>(shape.strokeWidth), strokeColor);
            HPEN oldPen = (HPEN)SelectObject(hdc, shapePen);
            HBRUSH oldBrush = (HBRUSH)SelectObject(hdc, shapeBrush);

            if (shape.type == ShapeType::Rectangle || shape.type == ShapeType::TextBox) {
                Rectangle(hdc, shapeX, shapeY, shapeX + shapeW, shapeY + shapeH);
            } else if (shape.type == ShapeType::Ellipse) {
                Ellipse(hdc, shapeX, shapeY, shapeX + shapeW, shapeY + shapeH);
            }

            SelectObject(hdc, oldBrush);
            SelectObject(hdc, oldPen);
            DeleteObject(shapeBrush);
            DeleteObject(shapePen);

            // Render Text Inside Shape
            if (!shape.text.empty()) {
                COLORREF txtColor = RGB((shape.textColor >> 16) & 0xFF, (shape.textColor >> 8) & 0xFF, shape.textColor & 0xFF);
                SetTextColor(hdc, txtColor);
                HFONT hFont = CreateFontW(static_cast<int>(shape.fontSize * scale), 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_GUI_FONT, shape.fontName.c_str());
                HFONT hOldFont = (HFONT)SelectObject(hdc, hFont);

                std::wstring wText(shape.text.begin(), shape.text.end());
                RECT txtRect = {shapeX + 10, shapeY + 10, shapeX + shapeW - 10, shapeY + shapeH - 10};
                DrawTextW(hdc, wText.c_str(), (int)wText.length(), &txtRect, DT_CENTER | DT_VCENTER | DT_WORDBREAK);

                SelectObject(hdc, hOldFont);
                DeleteObject(hFont);
            }

            // Draw Transform Handles if selected
            if (static_cast<int>(sIdx) == selectedShapeIndex) {
                drawTransformHandles(hdc, shapeX, shapeY, shapeW, shapeH);
            }
        }
    }
}

void SlidesView::handleKeyDown(WPARAM wParam, HWND hWnd) {
    if (wParam == VK_F5) {
        isPresenting = true;
    } else if (wParam == VK_ESCAPE) {
        isPresenting = false;
    } else if (wParam == VK_PAGE_DOWN || wParam == VK_RIGHT) {
        if (activeSlideIndex < presentation.slides.size() - 1) activeSlideIndex++;
    } else if (wParam == VK_PAGE_UP || wParam == VK_LEFT) {
        if (activeSlideIndex > 0) activeSlideIndex--;
    }
    InvalidateRect(hWnd, NULL, TRUE);
}

void SlidesView::handleClick(int x, int y, HWND hWnd) {
    int sidebarW = 200;
    if (x <= sidebarW) {
        // Handle Sidebar Click
        if (y >= 135 && y <= 165) {
            addSlide();
        } else if (y >= 180) {
            size_t clickedIdx = (y - 180) / 110;
            if (clickedIdx < presentation.slides.size()) {
                activeSlideIndex = clickedIdx;
            }
        }
        InvalidateRect(hWnd, NULL, TRUE);
    }
}

} // namespace GenOffice
