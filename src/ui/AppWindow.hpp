#ifndef GENOFFICE_APP_WINDOW_HPP
#ifndef GENOFFICE_APP_WINDOW_HPP
#define GENOFFICE_APP_WINDOW_HPP

#include <windows.h>
#include "RibbonBar.hpp"
#include "WriterView.hpp"
#include "CalcView.hpp"
#include "SlidesView.hpp"

namespace GenOffice {

class AppWindow {
public:
    HWND hWnd = NULL;
    RibbonBar ribbon;
    WriterView writerView;
    CalcView calcView;
    SlidesView slidesView;

    float zoomLevel = 1.0f;
    std::string statusText = "Sẵn sàng (Ready)";

    AppWindow();

    bool init(HINSTANCE hInstance, int nCmdShow);
    void runMessageLoop();

    LRESULT handleMessage(UINT uMsg, WPARAM wParam, LPARAM lParam);

private:
    void drawStatusBar(HDC hdc, int width, int height);
    void renderDoubleBuffered(HDC hdc, int width, int height);
};

} // namespace GenOffice

#endif // GENOFFICE_APP_WINDOW_HPP
