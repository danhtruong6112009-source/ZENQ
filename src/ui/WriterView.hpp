#ifndef GENOFFICE_WRITER_VIEW_HPP
#define GENOFFICE_WRITER_VIEW_HPP

#include <windows.h>
#include "../core/PieceTable.hpp"
#include "../core/DocumentAST.hpp"
#include <string>

namespace GenOffice {

class WriterView {
public:
    PieceTable pieceTable;
    DocumentAST docAST;
    
    size_t cursorPosition = 0;
    size_t selectionStart = 0;
    size_t selectionLength = 0;

    int scrollY = 0;
    float zoomLevel = 1.0f;

    WriterView();

    void draw(HDC hdc, int width, int height);
    void handleChar(wchar_t ch, HWND hWnd);
    void handleKeyDown(WPARAM wParam, HWND hWnd);
    void handleClick(int x, int y, HWND hWnd);

private:
    void drawRuler(HDC hdc, int pageX, int pageY, int pageW);
};

} // namespace GenOffice

#endif // GENOFFICE_WRITER_VIEW_HPP
