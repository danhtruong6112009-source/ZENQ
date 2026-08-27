#ifndef GENOFFICE_CALC_VIEW_HPP
#ifndef GENOFFICE_CALC_VIEW_HPP
#define GENOFFICE_CALC_VIEW_HPP

#include <windows.h>
#include "../core/SparseMatrix.hpp"
#include "../core/FormulaEngine.hpp"
#include <string>

namespace GenOffice {

class CalcView {
public:
    SparseMatrix sheet;
    
    uint32_t selectedRow = 1;
    uint32_t selectedCol = 1;

    uint32_t scrollRow = 1;
    uint32_t scrollCol = 1;

    int defaultColWidth = 90;
    int defaultRowHeight = 24;

    bool isEditing = false;
    std::string editText;

    CalcView();

    void draw(HDC hdc, int width, int height);
    void handleChar(wchar_t ch, HWND hWnd);
    void handleKeyDown(WPARAM wParam, HWND hWnd);
    void handleClick(int x, int y, HWND hWnd);

private:
    void drawFormulaBar(HDC hdc, int width);
};

} // namespace GenOffice

#endif // GENOFFICE_CALC_VIEW_HPP
