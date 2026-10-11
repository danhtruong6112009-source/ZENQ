#include <windows.h>
#include <stdint.h>
#include <cstdint>
#include "RibbonBar.hpp"
#include <vector>

namespace ZenQOffice {

RibbonBar::RibbonBar() {
    // Default 3 open tabs representing the 3 office modules (WPS Office style)
    docTabs.push_back({"Văn bản 1.docx", OfficeAppMode::Writer, true});
    docTabs.push_back({"Bảng tính 1.xlsx", OfficeAppMode::Calc, false});
    docTabs.push_back({"Trình chiếu 1.pptx", OfficeAppMode::Slides, false});

    ribbonTabs.push_back({L"Trang đầu", true});
    ribbonTabs.push_back({L"Chèn", false});
    ribbonTabs.push_back({L"Bố trí & Công thức", false});
    ribbonTabs.push_back({L"Xem", false});

    updateThemeColor();
}

void RibbonBar::updateThemeColor() {
    switch (currentMode) {
        case OfficeAppMode::Writer:
            activeColor = 0x2B579A; // Deep Blue
            break;
        case OfficeAppMode::Calc:
            activeColor = 0x217346; // Deep Green
            break;
        case OfficeAppMode::Slides:
            activeColor = 0xD24726; // Deep Orange-Red
            break;
    }
}

uint32_t RibbonBar::getPrimaryColor() const {
    return activeColor;
}

uint32_t RibbonBar::getSecondaryColor() const {
    switch (currentMode) {
        case OfficeAppMode::Writer: return 0x106EBE;
        case OfficeAppMode::Calc: return 0x107C41;
        case OfficeAppMode::Slides: return 0xC43E1C;
    }
    return 0x2B579A;
}

void RibbonBar::addDocTab(const std::string& title, OfficeAppMode mode) {
    for (auto& tab : docTabs) tab.active = false;
    docTabs.push_back({title, mode, true});
    activeDocIndex = docTabs.size() - 1;
    currentMode = mode;
    updateThemeColor();
}

void RibbonBar::switchDocTab(size_t index) {
    if (index < docTabs.size()) {
        for (size_t i = 0; i < docTabs.size(); ++i) {
            docTabs[i].active = (i == index);
        }
        activeDocIndex = index;
        currentMode = docTabs[index].mode;
        updateThemeColor();
    }
}

void RibbonBar::draw(HDC hdc, int width, int height) {
    // 1. Draw Title Bar (Top 36px)
    COLORREF themeColor = RGB((activeColor >> 16) & 0xFF, (activeColor >> 8) & 0xFF, activeColor & 0xFF);
    HBRUSH titleBrush = CreateSolidBrush(themeColor);
    RECT titleRect = {0, 0, width, 36};
    FillRect(hdc, &titleRect, titleBrush);
    DeleteObject(titleBrush);

    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, RGB(255, 255, 255));
    HFONT hTitleFont = CreateFontW(15, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_GUI_FONT, L"Segoe UI");
    HFONT hOldFont = (HFONT)SelectObject(hdc, hTitleFont);
    
    // Draw App Logo Title
    TextOutW(hdc, 12, 8, L"GenOffice", 9);

    // Draw Document Tabs (WPS Office style tab strip)
    int tabX = 120;
    for (size_t i = 0; i < docTabs.size(); ++i) {
        int tabWidth = 140;
        RECT docTabRect = {tabX, 4, tabX + tabWidth, 36};
        
        if (docTabs[i].active) {
            HBRUSH activeTabBrush = CreateSolidBrush(RGB(243, 242, 241));
            FillRect(hdc, &docTabRect, activeTabBrush);
            DeleteObject(activeTabBrush);
            SetTextColor(hdc, RGB(30, 30, 30));
        } else {
            HBRUSH inactiveTabBrush = CreateSolidBrush(RGB((activeColor >> 16) & 0xFF / 2, (activeColor >> 8) & 0xFF / 2, activeColor & 0xFF / 2));
            FillRect(hdc, &docTabRect, inactiveTabBrush);
            DeleteObject(inactiveTabBrush);
            SetTextColor(hdc, RGB(220, 220, 220));
        }

        std::wstring wTitle(docTabs[i].title.begin(), docTabs[i].title.end());
        TextOutW(hdc, tabX + 12, 10, wTitle.c_str(), (int)wTitle.length());
        tabX += tabWidth + 4;
    }

    // '+' New Document Button
    RECT newTabRect = {tabX, 8, tabX + 24, 32};
    HBRUSH btnBrush = CreateSolidBrush(RGB(255, 255, 255));
    FrameRect(hdc, &newTabRect, btnBrush);
    DeleteObject(btnBrush);
    SetTextColor(hdc, RGB(255, 255, 255));
    TextOutW(hdc, tabX + 7, 9, L"+", 1);

    // 2. Draw Ribbon Header Strip (Y: 36 to 64)
    RECT headerRect = {0, 36, width, 64};
    HBRUSH headerBrush = CreateSolidBrush(RGB(243, 242, 241));
    FillRect(hdc, &headerRect, headerBrush);
    DeleteObject(headerBrush);

    SelectObject(hdc, hOldFont);
    DeleteObject(hTitleFont);

    HFONT hRibbonFont = CreateFontW(14, 0, 0, 0, FW_SEMIBOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_GUI_FONT, L"Segoe UI");
    SelectObject(hdc, hRibbonFont);

    int rTabX = 16;
    for (size_t i = 0; i < ribbonTabs.size(); ++i) {
        int rTabWidth = 110;
        RECT rTabRect = {rTabX, 36, rTabX + rTabWidth, 64};

        if (i == activeRibbonTab) {
            HBRUSH rActiveBrush = CreateSolidBrush(RGB(255, 255, 255));
            FillRect(hdc, &rTabRect, rActiveBrush);
            DeleteObject(rActiveBrush);

            // Active underline indicator
            RECT lineRect = {rTabX, 61, rTabX + rTabWidth, 64};
            HBRUSH lineBrush = CreateSolidBrush(themeColor);
            FillRect(hdc, &lineRect, lineBrush);
            DeleteObject(lineBrush);

            SetTextColor(hdc, themeColor);
        } else {
            SetTextColor(hdc, RGB(80, 80, 80));
        }

        TextOutW(hdc, rTabX + 16, 42, ribbonTabs[i].title.c_str(), (int)ribbonTabs[i].title.length());
        rTabX += rTabWidth + 2;
    }

    // 3. Draw Ribbon Action Body Toolbar (Y: 64 to 125)
    RECT bodyRect = {0, 64, width, 125};
    HBRUSH bodyBrush = CreateSolidBrush(RGB(255, 255, 255));
    FillRect(hdc, &bodyRect, bodyBrush);
    DeleteObject(bodyBrush);

    // Separator line
    HPEN sepPen = CreatePen(PS_SOLID, 1, RGB(225, 223, 221));
    HPEN oldPen = (HPEN)SelectObject(hdc, sepPen);
    MoveToEx(hdc, 0, 124, NULL);
    LineTo(hdc, width, 124);

    // Draw Action Group Controls inside Ribbon Body
    SetTextColor(hdc, RGB(40, 40, 40));
    // Group 1: Clipboard
    TextOutW(hdc, 20, 72, L"Bảng tạm", 8);
    
    auto drawBtn = [&](int bx, int by, bool active, VectorIcon icon) {
        RECT r = {bx, by, bx + 28, by + 28};
        if (active) {
            HBRUSH actBrush = CreateSolidBrush(themeColor);
            FillRect(hdc, &r, actBrush);
            DeleteObject(actBrush);
        } else {
            HBRUSH normBrush = CreateSolidBrush(RGB(245, 245, 245));
            FillRect(hdc, &r, normBrush);
            DeleteObject(normBrush);
        }
        drawVectorIcon(hdc, icon, bx + 6, by + 6, 16, 16, active ? RGB(255, 255, 255) : RGB(80, 80, 80));
    };

    drawBtn(20, 92, false, VectorIcon::Undo);
    drawBtn(50, 92, false, VectorIcon::Redo);
    drawBtn(80, 92, false, VectorIcon::Copy);
    drawBtn(110, 92, false, VectorIcon::Cut);
    
    // Action Group Separator Line
    MoveToEx(hdc, 145, 70, NULL); LineTo(hdc, 145, 118);

    // Group 2: Font
    TextOutW(hdc, 155, 72, L"Định dạng chữ", 13);
    drawBtn(155, 92, isBold, VectorIcon::Bold);
    drawBtn(185, 92, isItalic, VectorIcon::Italic);
    drawBtn(215, 92, isUnderline, VectorIcon::Underline);
    
    MoveToEx(hdc, 250, 70, NULL); LineTo(hdc, 250, 118);

    // Group 3: Paragraph / Specific Module Controls
    if (currentMode == OfficeAppMode::Writer) {
        TextOutW(hdc, 260, 72, L"Đoạn văn", 8);
        drawBtn(260, 92, alignment == 0, VectorIcon::AlignLeft);
        drawBtn(290, 92, alignment == 1, VectorIcon::AlignCenter);
        drawBtn(320, 92, alignment == 2, VectorIcon::AlignRight);
        drawBtn(350, 92, alignment == 3, VectorIcon::AlignJustify);
    } else if (currentMode == OfficeAppMode::Calc) {
        TextOutW(hdc, 260, 72, L"Công thức chuẩn:", 16);
        TextOutW(hdc, 260, 98, L"=SUM()  =AVG()  =COUNT()  =IF()", 31);
    } else if (currentMode == OfficeAppMode::Slides) {
        TextOutW(hdc, 260, 72, L"Thuyết trình Slide:", 19);
        TextOutW(hdc, 260, 98, L"[+ Slide Mới]  [Trình chiếu F5]", 31);
    }

    SelectObject(hdc, oldPen);
    DeleteObject(sepPen);
    SelectObject(hdc, hOldFont);
    DeleteObject(hRibbonFont);
}

bool RibbonBar::handleClick(int x, int y, HWND hWnd) {
    if (y >= 0 && y <= 36) {
        // Document Tab Strip Click
        int tabX = 120;
        for (size_t i = 0; i < docTabs.size(); ++i) {
            int tabWidth = 140;
            if (x >= tabX && x <= tabX + tabWidth) {
                switchDocTab(i);
                InvalidateRect(hWnd, NULL, TRUE);
                return true;
            }
            tabX += tabWidth + 4;
        }

        // Click '+' New Document Tab
        if (x >= tabX && x <= tabX + 24) {
            static int newCount = 2;
            if (currentMode == OfficeAppMode::Writer) {
                addDocTab("Văn bản " + std::to_string(newCount++) + ".docx", OfficeAppMode::Writer);
            } else if (currentMode == OfficeAppMode::Calc) {
                addDocTab("Bảng tính " + std::to_string(newCount++) + ".xlsx", OfficeAppMode::Calc);
            } else {
                addDocTab("Trình chiếu " + std::to_string(newCount++) + ".pptx", OfficeAppMode::Slides);
            }
            InvalidateRect(hWnd, NULL, TRUE);
            return true;
        }
    } else if (y > 36 && y <= 64) {
        // Ribbon Tab Header Click
        int rTabX = 16;
        for (size_t i = 0; i < ribbonTabs.size(); ++i) {
            int rTabWidth = 110;
            if (x >= rTabX && x <= rTabX + rTabWidth) {
                activeRibbonTab = i;
                InvalidateRect(hWnd, NULL, TRUE);
                return true;
            }
            rTabX += rTabWidth + 2;
        }
    } else if (y > 64 && y <= 125) {
        // Handle click on buttons
        if (y >= 92 && y <= 120) {
            // Group 2: Font
            if (x >= 155 && x <= 183) { isBold = !isBold; InvalidateRect(hWnd, NULL, TRUE); return true; }
            if (x >= 185 && x <= 213) { isItalic = !isItalic; InvalidateRect(hWnd, NULL, TRUE); return true; }
            if (x >= 215 && x <= 243) { isUnderline = !isUnderline; InvalidateRect(hWnd, NULL, TRUE); return true; }
            
            // Group 3: Paragraph
            if (currentMode == OfficeAppMode::Writer) {
                if (x >= 260 && x <= 288) { alignment = 0; InvalidateRect(hWnd, NULL, TRUE); return true; }
                if (x >= 290 && x <= 318) { alignment = 1; InvalidateRect(hWnd, NULL, TRUE); return true; }
                if (x >= 320 && x <= 348) { alignment = 2; InvalidateRect(hWnd, NULL, TRUE); return true; }
                if (x >= 350 && x <= 378) { alignment = 3; InvalidateRect(hWnd, NULL, TRUE); return true; }
            }
        }
    }
    return false;
}

void RibbonBar::drawVectorIcon(HDC hdc, VectorIcon icon, int x, int y, int width, int height, COLORREF color, int strokeWidth) {
    LOGBRUSH lb;
    lb.lbStyle = BS_SOLID;
    lb.lbColor = color;
    lb.lbHatch = 0;
    HPEN hPen = ExtCreatePen(PS_GEOMETRIC | PS_SOLID | PS_ENDCAP_ROUND | PS_JOIN_ROUND, strokeWidth, &lb, 0, NULL);
    HPEN hOldPen = (HPEN)SelectObject(hdc, hPen);
    HBRUSH hBrush = CreateSolidBrush(color);
    HBRUSH hOldBrush = (HBRUSH)SelectObject(hdc, GetStockObject(NULL_BRUSH));
    SelectObject(hdc, GetStockObject(HOLLOW_BRUSH));

    int cx = x + width / 2;
    int cy = y + height / 2;

    switch (icon) {
        case VectorIcon::Undo:
            Arc(hdc, x + 2, y + height / 2 - 4, x + width - 2, y + height, x + width / 2, y + height, x + 2, y + height / 2);
            MoveToEx(hdc, x + 2, y + height / 2, NULL); LineTo(hdc, x + 6, y + height / 2 - 4);
            MoveToEx(hdc, x + 2, y + height / 2, NULL); LineTo(hdc, x + 6, y + height / 2 + 4);
            break;
        case VectorIcon::Redo:
            Arc(hdc, x + 2, y + height / 2 - 4, x + width - 2, y + height, x + width - 2, y + height / 2, x + width / 2, y + height);
            MoveToEx(hdc, x + width - 2, y + height / 2, NULL); LineTo(hdc, x + width - 6, y + height / 2 - 4);
            MoveToEx(hdc, x + width - 2, y + height / 2, NULL); LineTo(hdc, x + width - 6, y + height / 2 + 4);
            break;
        case VectorIcon::Copy:
            Rectangle(hdc, x + 6, y + 6, x + width, y + height);
            Rectangle(hdc, x, y, x + width - 6, y + height - 6);
            break;
        case VectorIcon::Cut:
            Ellipse(hdc, x, y + height - 6, x + 6, y + height);
            Ellipse(hdc, x + width - 6, y + height - 6, x + width, y + height);
            MoveToEx(hdc, x + 5, y + height - 5, NULL); LineTo(hdc, x + width, y);
            MoveToEx(hdc, x + width - 5, y + height - 5, NULL); LineTo(hdc, x, y);
            break;
        case VectorIcon::Bold:
            MoveToEx(hdc, x + 4, y, NULL); LineTo(hdc, x + 4, y + height);
            MoveToEx(hdc, x + 4, y, NULL); LineTo(hdc, x + width - 6, y);
            Arc(hdc, x + 4, y, x + width - 2, y + height / 2 + 2, x + width - 4, y, x + 4, y + height / 2);
            MoveToEx(hdc, x + 4, y + height / 2, NULL); LineTo(hdc, x + width - 4, y + height / 2);
            Arc(hdc, x + 4, y + height / 2, x + width, y + height, x + width - 4, y + height / 2, x + 4, y + height);
            MoveToEx(hdc, x + 4, y + height, NULL); LineTo(hdc, x + width - 4, y + height);
            break;
        case VectorIcon::Italic:
            MoveToEx(hdc, x + width - 4, y, NULL); LineTo(hdc, x + 6, y + height);
            MoveToEx(hdc, x + width - 8, y, NULL); LineTo(hdc, x + width, y);
            MoveToEx(hdc, x + 2, y + height, NULL); LineTo(hdc, x + 10, y + height);
            break;
        case VectorIcon::Underline:
            MoveToEx(hdc, x + 4, y, NULL); LineTo(hdc, x + 4, y + height - 4);
            MoveToEx(hdc, x + width - 4, y, NULL); LineTo(hdc, x + width - 4, y + height - 4);
            Arc(hdc, x + 4, y + height - 10, x + width - 4, y + height - 2, x + 4, y + height - 4, x + width - 4, y + height - 4);
            MoveToEx(hdc, x, y + height, NULL); LineTo(hdc, x + width, y + height);
            break;
        case VectorIcon::AlignLeft:
            MoveToEx(hdc, x, y + 2, NULL); LineTo(hdc, x + width, y + 2);
            MoveToEx(hdc, x, y + 6, NULL); LineTo(hdc, x + width - 6, y + 6);
            MoveToEx(hdc, x, y + 10, NULL); LineTo(hdc, x + width, y + 10);
            MoveToEx(hdc, x, y + 14, NULL); LineTo(hdc, x + width - 8, y + 14);
            break;
        case VectorIcon::AlignCenter:
            MoveToEx(hdc, x, y + 2, NULL); LineTo(hdc, x + width, y + 2);
            MoveToEx(hdc, x + 3, y + 6, NULL); LineTo(hdc, x + width - 3, y + 6);
            MoveToEx(hdc, x, y + 10, NULL); LineTo(hdc, x + width, y + 10);
            MoveToEx(hdc, x + 4, y + 14, NULL); LineTo(hdc, x + width - 4, y + 14);
            break;
        case VectorIcon::AlignRight:
            MoveToEx(hdc, x, y + 2, NULL); LineTo(hdc, x + width, y + 2);
            MoveToEx(hdc, x + 6, y + 6, NULL); LineTo(hdc, x + width, y + 6);
            MoveToEx(hdc, x, y + 10, NULL); LineTo(hdc, x + width, y + 10);
            MoveToEx(hdc, x + 8, y + 14, NULL); LineTo(hdc, x + width, y + 14);
            break;
        case VectorIcon::AlignJustify:
            MoveToEx(hdc, x, y + 2, NULL); LineTo(hdc, x + width, y + 2);
            MoveToEx(hdc, x, y + 6, NULL); LineTo(hdc, x + width, y + 6);
            MoveToEx(hdc, x, y + 10, NULL); LineTo(hdc, x + width, y + 10);
            MoveToEx(hdc, x, y + 14, NULL); LineTo(hdc, x + width, y + 14);
            break;
        default:
            break;
    }

    SelectObject(hdc, hOldPen);
    SelectObject(hdc, hOldBrush);
    DeleteObject(hPen);
    DeleteObject(hBrush);
}

} // namespace ZenQOffice
