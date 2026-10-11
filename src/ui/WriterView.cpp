#include "WriterView.hpp"
#include <vector>
#include <sstream>
#include <algorithm>

namespace ZenQOffice {

WriterView::WriterView() {
    std::string sampleText = 
        "CHÀO MỪNG BẠN ĐẾN VỚI GENOFFICE WRITER!\n\n"
        "GenOffice là bộ ứng dụng văn phòng đa năng thế hệ mới, được phát triển bằng ngôn ngữ C++ thuần với hiệu năng vượt trội, khởi động tức thì và dung lượng nhẹ nhàng.\n\n"
        "Các tính năng cốt lõi của GenOffice Writer:\n"
        "- Cấu trúc bộ đệm Piece Table O(1) tối ưu xử lý văn bản quy mô lớn.\n"
        "- Dựng trang giấy A4 chuẩn in ấn kèm thước đo trực quan (Interactive Ruler).\n"
        "- Định dạng phông chữ, cỡ chữ, căn lề, in đậm, in nghiêng linh hoạt.\n"
        "- Hỗ trợ gõ tiếng Việt Unicode có dấu mượt mà không bị mất hay trùng ký tự.\n\n"
        "Hãy bắt đầu soạn thảo tài liệu của bạn ngay tại đây!";
    
    pieceTable.setInitialText(sampleText);
    cursorPosition = sampleText.length();
}

void WriterView::drawRuler(HDC hdc, int pageX, int pageY, int pageW) {
    // Top Horizontal Ruler (24px height above page)
    RECT rulerRect = {pageX, pageY - 28, pageX + pageW, pageY - 4};
    HBRUSH rulerBrush = CreateSolidBrush(RGB(240, 240, 240));
    FillRect(hdc, &rulerRect, rulerBrush);
    DeleteObject(rulerBrush);

    HPEN rPen = CreatePen(PS_SOLID, 1, RGB(180, 180, 180));
    HPEN oldPen = (HPEN)SelectObject(hdc, rPen);
    MoveToEx(hdc, pageX, pageY - 4, NULL);
    LineTo(hdc, pageX + pageW, pageY - 4);

    // Draw ruler tick marks
    for (int x = 20; x < pageW - 20; x += 20) {
        int tickH = (x % 100 == 0) ? 12 : 6;
        MoveToEx(hdc, pageX + x, pageY - 4, NULL);
        LineTo(hdc, pageX + x, pageY - 4 - tickH);
    }
    SelectObject(hdc, oldPen);
    DeleteObject(rPen);
}

void WriterView::draw(HDC hdc, int width, int height) {
    // 1. Workspace Background (#E5E5E5)
    RECT bgRect = {0, 125, width, height - 28};
    HBRUSH bgBrush = CreateSolidBrush(RGB(229, 229, 229));
    FillRect(hdc, &bgRect, bgBrush);
    DeleteObject(bgBrush);

    // 2. Compute A4 Paper Bounds
    int pageW = static_cast<int>(595 * zoomLevel);
    int pageH = static_cast<int>(842 * zoomLevel);
    int pageX = (width - pageW) / 2;
    if (pageX < 40) pageX = 40;
    int pageY = 165 - scrollY;

    // Draw Interactive Top Ruler
    drawRuler(hdc, pageX, pageY, pageW);

    // 3. Draw A4 White Paper Canvas with subtle shadow
    RECT shadowRect = {pageX + 4, pageY + 4, pageX + pageW + 4, pageY + pageH + 4};
    HBRUSH shadowBrush = CreateSolidBrush(RGB(200, 200, 200));
    FillRect(hdc, &shadowRect, shadowBrush);
    DeleteObject(shadowBrush);

    RECT paperRect = {pageX, pageY, pageX + pageW, pageY + pageH};
    HBRUSH paperBrush = CreateSolidBrush(RGB(255, 255, 255));
    FillRect(hdc, &paperRect, paperBrush);
    DeleteObject(paperBrush);

    // Page Border
    HPEN borderPen = CreatePen(PS_SOLID, 1, RGB(210, 210, 210));
    HPEN oldPen = (HPEN)SelectObject(hdc, borderPen);
    FrameRect(hdc, &paperRect, borderBrush = CreateSolidBrush(RGB(210, 210, 210)));
    DeleteObject(borderBrush);
    SelectObject(hdc, oldPen);
    DeleteObject(borderPen);

    // 4. Render Text Lines from Piece Table
    std::string fullText = pieceTable.getText();
    std::wstring wText;
    int len = MultiByteToWideChar(CP_UTF8, 0, fullText.c_str(), -1, NULL, 0);
    if (len > 0) {
        wText.resize(len - 1);
        MultiByteToWideChar(CP_UTF8, 0, fullText.c_str(), -1, &wText[0], len);
    }

    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, RGB(20, 20, 20));

    HFONT hFont = CreateFontW(static_cast<int>(18 * zoomLevel), 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_GUI_FONT, L"Segoe UI");
    HFONT hOldFont = (HFONT)SelectObject(hdc, hFont);

    int textX = pageX + static_cast<int>(54 * zoomLevel);
    int textY = pageY + static_cast<int>(54 * zoomLevel);
    int maxTextW = pageW - static_cast<int>(108 * zoomLevel);

    std::wstringstream wss(wText);
    std::wstring line;
    int currentLineY = textY;
    int lineSpacing = static_cast<int>(24 * zoomLevel);

    size_t charIndex = 0;
    int cursorX = textX;
    int cursorY = textY;

    while (std::getline(wss, line)) {
        // Draw Text Line
        TextOutW(hdc, textX, currentLineY, line.c_str(), (int)line.length());

        // Check Cursor Position in this line
        if (cursorPosition >= charIndex && cursorPosition <= charIndex + line.length()) {
            size_t offsetInLine = cursorPosition - charIndex;
            std::wstring subLine = line.substr(0, offsetInLine);
            SIZE sizeStr;
            GetTextExtentPoint32W(hdc, subLine.c_str(), (int)subLine.length(), &sizeStr);
            cursorX = textX + sizeStr.cx;
            cursorY = currentLineY;
        }

        charIndex += line.length() + 1; // +1 for '\n'
        currentLineY += lineSpacing;
    }

    // 5. Draw Blinking Text Cursor
    HPEN cursorPen = CreatePen(PS_SOLID, 2, RGB(0, 102, 204));
    oldPen = (HPEN)SelectObject(hdc, cursorPen);
    MoveToEx(hdc, cursorX, cursorY, NULL);
    LineTo(hdc, cursorX, cursorY + static_cast<int>(20 * zoomLevel));
    SelectObject(hdc, oldPen);
    DeleteObject(cursorPen);

    SelectObject(hdc, hOldFont);
    DeleteObject(hFont);
}

void WriterView::handleChar(wchar_t ch, HWND hWnd) {
    if (ch == L'\r') ch = L'\n';
    if (ch == L'\b') {
        if (cursorPosition > 0) {
            std::string fullText = pieceTable.getText();
            // Simple deletion
            pieceTable.remove(cursorPosition - 1, 1);
            cursorPosition--;
        }
    } else if (ch >= 32 || ch == L'\n') {
        // Convert wchar_t to UTF-8
        wchar_t wstr[2] = {ch, 0};
        char utf8[8] = {0};
        WideCharToMultiByte(CP_UTF8, 0, wstr, 1, utf8, sizeof(utf8), NULL, NULL);

        pieceTable.insert(cursorPosition, std::string(utf8));
        cursorPosition += std::string(utf8).length();
    }
    InvalidateRect(hWnd, NULL, TRUE);
}

void WriterView::handleKeyDown(WPARAM wParam, HWND hWnd) {
    if (wParam == VK_LEFT) {
        if (cursorPosition > 0) cursorPosition--;
    } else if (wParam == VK_RIGHT) {
        if (cursorPosition < pieceTable.getLength()) cursorPosition++;
    } else if (wParam == VK_HOME) {
        cursorPosition = 0;
    } else if (wParam == VK_END) {
        cursorPosition = pieceTable.getLength();
    }
    InvalidateRect(hWnd, NULL, TRUE);
}

void WriterView::handleClick(int x, int y, HWND hWnd) {
    // Simple click focus positioning
    InvalidateRect(hWnd, NULL, TRUE);
}

} // namespace ZenQOffice
