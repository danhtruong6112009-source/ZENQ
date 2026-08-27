#include "CalcView.hpp"
#include <vector>
#include <algorithm>

namespace GenOffice {

CalcView::CalcView() {
    // Populate sample spreadsheet data
    sheet.setCellText(1, 1, "Danh Mục Hàng");
    sheet.setCellText(1, 2, "Số Lượng");
    sheet.setCellText(1, 3, "Đơn Giá ($)");
    sheet.setCellText(1, 4, "Thành Tiền ($)");

    sheet.setCellText(2, 1, "Laptop GenBook");
    sheet.setCellText(2, 2, "5");
    sheet.setCellText(2, 3, "1200");
    sheet.setCellText(2, 4, "=B2*C2");

    sheet.setCellText(3, 1, "Màn Hình 4K 27\"");
    sheet.setCellText(3, 2, "10");
    sheet.setCellText(3, 3, "450");
    sheet.setCellText(3, 4, "=B3*C3");

    sheet.setCellText(4, 1, "Bàn Phím Cơ");
    sheet.setCellText(4, 2, "15");
    sheet.setCellText(4, 3, "85");
    sheet.setCellText(4, 4, "=B4*C4");

    sheet.setCellText(5, 1, "Chuột Không Dây");
    sheet.setCellText(5, 2, "20");
    sheet.setCellText(5, 3, "40");
    sheet.setCellText(5, 4, "=B5*C5");

    sheet.setCellText(7, 3, "TỔNG CỘNG:");
    sheet.setCellText(7, 4, "=SUM(D2:D5)");

    FormulaEngine::evaluateSheet(sheet);
}

void CalcView::drawFormulaBar(HDC hdc, int width) {
    // Formula Bar background (Y: 125 to 155)
    RECT barRect = {0, 125, width, 155};
    HBRUSH barBrush = CreateSolidBrush(RGB(245, 245, 245));
    FillRect(hdc, &barRect, barBrush);
    DeleteObject(barBrush);

    HPEN pen = CreatePen(PS_SOLID, 1, RGB(215, 215, 215));
    HPEN oldPen = (HPEN)SelectObject(hdc, pen);
    MoveToEx(hdc, 0, 155, NULL);
    LineTo(hdc, width, 155);

    // Cell Address Box (e.g. "A1")
    RECT nameBox = {8, 129, 68, 151};
    HBRUSH nameBrush = CreateSolidBrush(RGB(255, 255, 255));
    FillRect(hdc, &nameBox, nameBrush);
    FrameRect(hdc, &nameBox, nameBrush = CreateSolidBrush(RGB(200, 200, 200)));
    DeleteObject(nameBrush);

    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, RGB(20, 20, 20));

    HFONT hFont = CreateFontW(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_GUI_FONT, L"Segoe UI");
    HFONT hOldFont = (HFONT)SelectObject(hdc, hFont);

    std::string cellAddr = SparseMatrix::colIndexToName(selectedCol) + std::to_string(selectedRow);
    std::wstring wAddr(cellAddr.begin(), cellAddr.end());
    TextOutW(hdc, 16, 132, wAddr.c_str(), (int)wAddr.length());

    // Formula Icon 'fx'
    SetTextColor(hdc, RGB(0, 114, 54));
    TextOutW(hdc, 76, 132, L"fx", 2);

    // Formula Input Field Box
    RECT fBox = {100, 129, width - 12, 151};
    HBRUSH fBrush = CreateSolidBrush(RGB(255, 255, 255));
    FillRect(hdc, &fBox, fBrush);
    FrameRect(hdc, &fBox, fBrush = CreateSolidBrush(RGB(200, 200, 200)));
    DeleteObject(fBrush);

    SetTextColor(hdc, RGB(30, 30, 30));
    SelectObject(hdc, hOldFont);
    DeleteObject(hFont);

    HFONT hNormFont = CreateFontW(14, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_GUI_FONT, L"Segoe UI");
    SelectObject(hdc, hNormFont);

    const CellData* cell = sheet.getCell(selectedRow, selectedCol);
    std::string formulaDisplay = isEditing ? editText : (cell ? (cell->formulaStr.empty() ? cell->rawValue : cell->formulaStr) : "");
    std::wstring wFormula(formulaDisplay.begin(), formulaDisplay.end());
    TextOutW(hdc, 108, 132, wFormula.c_str(), (int)wFormula.length());

    SelectObject(hdc, hOldFont);
    DeleteObject(hNormFont);
    SelectObject(hdc, oldPen);
    DeleteObject(pen);
}

void CalcView::draw(HDC hdc, int width, int height) {
    // 1. Draw Formula Bar
    drawFormulaBar(hdc, width);

    // 2. Draw Grid Header (Column A, B, C...)
    int headerH = 26;
    int headerW = 48; // Row Number Header Width
    int startY = 156;

    // Top Left Corner Box
    RECT cornerRect = {0, startY, headerW, startY + headerH};
    HBRUSH cornerBrush = CreateSolidBrush(RGB(235, 235, 235));
    FillRect(hdc, &cornerRect, cornerBrush);
    DeleteObject(cornerBrush);

    HPEN gridPen = CreatePen(PS_SOLID, 1, RGB(220, 220, 220));
    HPEN oldPen = (HPEN)SelectObject(hdc, gridPen);

    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, RGB(80, 80, 80));
    HFONT hHeaderFont = CreateFontW(13, 0, 0, 0, FW_SEMIBOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_GUI_FONT, L"Segoe UI");
    HFONT hOldFont = (HFONT)SelectObject(hdc, hHeaderFont);

    // Render Column Headers
    int curX = headerW;
    uint32_t colIdx = scrollCol;
    std::vector<int> colPositions;

    while (curX < width) {
        colPositions.push_back(curX);
        RECT cRect = {curX, startY, curX + defaultColWidth, startY + headerH};
        HBRUSH cBrush = CreateSolidBrush((colIdx == selectedCol) ? RGB(215, 235, 220) : RGB(242, 242, 242));
        FillRect(hdc, &cRect, cBrush);
        DeleteObject(cBrush);

        FrameRect(hdc, &cRect, cBrush = CreateSolidBrush(RGB(215, 215, 215)));
        DeleteObject(cBrush);

        std::string colName = SparseMatrix::colIndexToName(colIdx);
        std::wstring wColName(colName.begin(), colName.end());
        TextOutW(hdc, curX + defaultColWidth / 2 - 6, startY + 5, wColName.c_str(), (int)wColName.length());

        curX += defaultColWidth;
        colIdx++;
    }

    // Render Row Headers & Grid Cells
    int curY = startY + headerH;
    uint32_t rowIdx = scrollRow;
    HFONT hCellFont = CreateFontW(14, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_GUI_FONT, L"Segoe UI");

    while (curY < height - 28) {
        // Row Header
        RECT rRect = {0, curY, headerW, curY + defaultRowHeight};
        HBRUSH rBrush = CreateSolidBrush((rowIdx == selectedRow) ? RGB(215, 235, 220) : RGB(242, 242, 242));
        FillRect(hdc, &rRect, rBrush);
        DeleteObject(rBrush);

        FrameRect(hdc, &rRect, rBrush = CreateSolidBrush(RGB(215, 215, 215)));
        DeleteObject(rBrush);

        std::wstring wRowStr = std::to_wstring(rowIdx);
        TextOutW(hdc, 10, curY + 4, wRowStr.c_str(), (int)wRowStr.length());

        // Grid Cells in this row
        curX = headerW;
        colIdx = scrollCol;

        SelectObject(hdc, hCellFont);
        SetTextColor(hdc, RGB(20, 20, 20));

        while (curX < width) {
            RECT cellRect = {curX, curY, curX + defaultColWidth, curY + defaultRowHeight};

            // Fill cell background
            HBRUSH cellBg = CreateSolidBrush(RGB(255, 255, 255));
            FillRect(hdc, &cellRect, cellBg);
            DeleteObject(cellBg);

            // Draw grid line
            MoveToEx(hdc, curX, curY + defaultRowHeight, NULL);
            LineTo(hdc, curX + defaultColWidth, curY + defaultRowHeight);
            MoveToEx(hdc, curX + defaultColWidth, curY, NULL);
            LineTo(hdc, curX + defaultColWidth, curY + defaultRowHeight);

            // Draw Cell Text
            const CellData* cell = sheet.getCell(rowIdx, colIdx);
            if (cell && !cell->evaluatedStr.empty()) {
                std::wstring wCellText;
                int len = MultiByteToWideChar(CP_UTF8, 0, cell->evaluatedStr.c_str(), -1, NULL, 0);
                if (len > 0) {
                    wCellText.resize(len - 1);
                    MultiByteToWideChar(CP_UTF8, 0, cell->evaluatedStr.c_str(), -1, &wCellText[0], len);
                }
                TextOutW(hdc, curX + 6, curY + 4, wCellText.c_str(), (int)wCellText.length());
            }

            curX += defaultColWidth;
            colIdx++;
        }

        SelectObject(hdc, hHeaderFont);
        SetTextColor(hdc, RGB(80, 80, 80));

        curY += defaultRowHeight;
        rowIdx++;
    }

    // 3. Highlight Selected Cell Box with thick green border
    int selX = headerW + (selectedCol - scrollCol) * defaultColWidth;
    int selY = startY + headerH + (selectedRow - scrollRow) * defaultRowHeight;

    if (selX >= headerW && selY >= startY + headerH) {
        RECT selBorder = {selX, selY, selX + defaultColWidth + 1, selY + defaultRowHeight + 1};
        HPEN activePen = CreatePen(PS_SOLID, 2, RGB(33, 115, 70));
        HPEN oldActivePen = (HPEN)SelectObject(hdc, activePen);
        HBRUSH nullBrush = (HBRUSH)GetStockObject(NULL_BRUSH);
        HBRUSH oldBrush = (HBRUSH)SelectObject(hdc, nullBrush);

        Rectangle(hdc, selBorder.left, selBorder.top, selBorder.right, selBorder.bottom);

        // Auto-fill handle dot (small 5x5 green square at bottom right)
        RECT fillHandle = {selBorder.right - 4, selBorder.bottom - 4, selBorder.right + 2, selBorder.bottom + 2};
        HBRUSH greenBrush = CreateSolidBrush(RGB(33, 115, 70));
        FillRect(hdc, &fillHandle, greenBrush);
        DeleteObject(greenBrush);

        SelectObject(hdc, oldBrush);
        SelectObject(hdc, oldActivePen);
        DeleteObject(activePen);
    }

    SelectObject(hdc, hOldFont);
    DeleteObject(hHeaderFont);
    DeleteObject(hCellFont);
    SelectObject(hdc, oldPen);
    DeleteObject(gridPen);
}

void CalcView::handleChar(wchar_t ch, HWND hWnd) {
    if (ch == L'\r') {
        // Press Enter to confirm cell entry
        if (isEditing) {
            sheet.setCellText(selectedRow, selectedCol, editText);
            FormulaEngine::evaluateSheet(sheet);
            isEditing = false;
        }
        selectedRow++;
    } else if (ch == L'\b') {
        if (!isEditing) {
            const CellData* cell = sheet.getCell(selectedRow, selectedCol);
            editText = cell ? (cell->formulaStr.empty() ? cell->rawValue : cell->formulaStr) : "";
            isEditing = true;
        }
        if (!editText.empty()) editText.pop_back();
    } else if (ch >= 32) {
        if (!isEditing) {
            editText.clear();
            isEditing = true;
        }
        wchar_t wstr[2] = {ch, 0};
        char utf8[8] = {0};
        WideCharToMultiByte(CP_UTF8, 0, wstr, 1, utf8, sizeof(utf8), NULL, NULL);
        editText += utf8;
    }
    InvalidateRect(hWnd, NULL, TRUE);
}

void CalcView::handleKeyDown(WPARAM wParam, HWND hWnd) {
    if (wParam == VK_UP && selectedRow > 1) {
        selectedRow--;
        isEditing = false;
    } else if (wParam == VK_DOWN) {
        selectedRow++;
        isEditing = false;
    } else if (wParam == VK_LEFT && selectedCol > 1) {
        selectedCol--;
        isEditing = false;
    } else if (wParam == VK_RIGHT) {
        selectedCol++;
        isEditing = false;
    }
    InvalidateRect(hWnd, NULL, TRUE);
}

void CalcView::handleClick(int x, int y, HWND hWnd) {
    int startY = 156;
    int headerH = 26;
    int headerW = 48;

    if (x > headerW && y > startY + headerH) {
        uint32_t colClicked = scrollCol + (x - headerW) / defaultColWidth;
        uint32_t rowClicked = scrollRow + (y - (startY + headerH)) / defaultRowHeight;

        selectedCol = colClicked;
        selectedRow = rowClicked;
        isEditing = false;
        InvalidateRect(hWnd, NULL, TRUE);
    }
}

} // namespace GenOffice
