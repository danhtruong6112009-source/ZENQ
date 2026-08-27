#ifndef GENOFFICE_SLIDE_PRESENTER_HPP
#ifndef GENOFFICE_SLIDE_PRESENTER_HPP
#define GENOFFICE_SLIDE_PRESENTER_HPP

#include <windows.h>
#include "../core/SlideModel.hpp"

namespace GenOffice {

class SlidePresenter {
public:
    static void renderFullscreenSlide(HDC hdc, const SlideNode& slide, int screenW, int screenH) {
        // Black Fullscreen Background
        RECT bgRect = {0, 0, screenW, screenH};
        HBRUSH bgBrush = CreateSolidBrush(RGB(0, 0, 0));
        FillRect(hdc, &bgRect, bgBrush);
        DeleteObject(bgBrush);

        // Compute 16:9 Aspect Ratio Display Rect
        int slideW = screenW;
        int slideH = screenW * 9 / 16;
        if (slideH > screenH) {
            slideH = screenH;
            slideW = screenH * 16 / 9;
        }
        int slideX = (screenW - slideW) / 2;
        int slideY = (screenH - slideH) / 2;

        RECT slideCanvas = {slideX, slideY, slideX + slideW, slideY + slideH};
        HBRUSH canvasBrush = CreateSolidBrush(RGB(255, 255, 255));
        FillRect(hdc, &slideCanvas, canvasBrush);
        DeleteObject(canvasBrush);

        // Render Slide Shapes
        float scale = slideW / 1920.0f;
        SetBkMode(hdc, TRANSPARENT);

        for (const auto& shape : slide.shapes) {
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
        }
    }
};

} // namespace GenOffice

#endif // GENOFFICE_SLIDE_PRESENTER_HPP
