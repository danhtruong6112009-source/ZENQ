#ifndef GENOFFICE_RIBBON_BAR_HPP
#define GENOFFICE_RIBBON_BAR_HPP

#include <windows.h>
#include <stdint.h>
#include <cstdint>
#include <string>
#include <vector>

namespace GenOffice {

enum class OfficeAppMode { Writer, Calc, Slides };

struct RibbonTab {
    std::wstring title;
    bool active = false;
};

struct DocTab {
    std::string title;
    OfficeAppMode mode;
    bool active = false;
};

class RibbonBar {
public:
    OfficeAppMode currentMode = OfficeAppMode::Writer;
    std::vector<DocTab> docTabs;
    size_t activeDocIndex = 0;

    std::vector<RibbonTab> ribbonTabs;
    size_t activeRibbonTab = 0;

    // Formatting state
    std::wstring selectedFont = L"Segoe UI";
    float fontSize = 12.0f;
    bool isBold = false;
    bool isItalic = false;
    bool isUnderline = false;
    int alignment = 0; // 0: Left, 1: Center, 2: Right, 3: Justify
    uint32_t activeColor = 0x2B579A;

    RibbonBar();

    void updateThemeColor();
    uint32_t getPrimaryColor() const;
    uint32_t getSecondaryColor() const;

    void draw(HDC hdc, int width, int height);
    bool handleClick(int x, int y, HWND hWnd);

    void addDocTab(const std::string& title, OfficeAppMode mode);
    void switchDocTab(size_t index);

private:
    enum class VectorIcon {
        Undo, Redo, Copy, Cut,
        Bold, Italic, Underline, Strikethrough,
        AlignLeft, AlignCenter, AlignRight, AlignJustify,
        ListBulleted, ListNumbered, H1, H2,
        PageBreak, Table, ImageIcon
    };
    void drawVectorIcon(HDC hdc, VectorIcon icon, int x, int y, int width, int height, COLORREF color, int strokeWidth = 2);
};

} // namespace GenOffice

#endif // GENOFFICE_RIBBON_BAR_HPP
