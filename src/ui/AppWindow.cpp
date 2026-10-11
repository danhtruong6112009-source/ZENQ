#include "AppWindow.hpp"
#include "SlidePresenter.hpp"
#include <string>

namespace ZenQOffice {

static AppWindow* g_pAppWindow = nullptr;

LRESULT CALLBACK WndProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
    if (g_pAppWindow) {
        return g_pAppWindow->handleMessage(uMsg, wParam, lParam);
    }
    return DefWindowProc(hWnd, uMsg, wParam, lParam);
}

AppWindow::AppWindow() {
    g_pAppWindow = this;
}

bool AppWindow::init(HINSTANCE hInstance, int nCmdShow) {
    WNDCLASSEXW wc = {0};
    wc.cbSize = sizeof(WNDCLASSEXW);
    wc.style = CS_HREDRAW | CS_VREDRAW;
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInstance;
    wc.hIcon = LoadIcon(NULL, IDI_APPLICATION);
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
    wc.lpszClassName = L"GenOfficeWindowClass";

    if (!RegisterClassExW(&wc)) {
        return false;
    }

    hWnd = CreateWindowExW(
        0,
        L"GenOfficeWindowClass",
        L"GenOffice - Bộ Ứng Dụng Văn Phòng Đa Năng",
        WS_OVERLAPPEDWINDOW | WS_CLIPCHILDREN,
        CW_USEDEFAULT, CW_USEDEFAULT,
        1280, 800,
        NULL, NULL, hInstance, NULL
    );

    if (!hWnd) return false;

    ShowWindow(hWnd, nCmdShow);
    UpdateWindow(hWnd);
    return true;
}

void AppWindow::runMessageLoop() {
    MSG msg;
    while (GetMessage(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
}

void AppWindow::drawStatusBar(HDC hdc, int width, int height) {
    // Bottom Status Bar (Y: height - 28 to height)
    RECT statusRect = {0, height - 28, width, height};
    HBRUSH statusBrush = CreateSolidBrush(RGB((ribbon.getPrimaryColor() >> 16) & 0xFF, (ribbon.getPrimaryColor() >> 8) & 0xFF, ribbon.getPrimaryColor() & 0xFF));
    FillRect(hdc, &statusRect, statusBrush);
    DeleteObject(statusBrush);

    SetBkMode(hdc, TRANSPARENT);
    SetTextColor(hdc, RGB(255, 255, 255));
    HFONT hFont = CreateFontW(13, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_GUI_FONT, L"Segoe UI");
    HFONT hOldFont = (HFONT)SelectObject(hdc, hFont);

    // Left Status Text
    std::wstring wStatus(statusText.begin(), statusText.end());
    TextOutW(hdc, 12, height - 20, wStatus.c_str(), (int)wStatus.length());

    // Module Info in Status Bar
    if (ribbon.currentMode == OfficeAppMode::Writer) {
        std::wstring infoStr = L"Trang 1 / 1   |   Từ: " + std::to_wstring(writerView.pieceTable.getLength()) + L"   |   Tiếng Việt (Unicode)";
        TextOutW(hdc, 200, height - 20, infoStr.c_str(), (int)infoStr.length());
    } else if (ribbon.currentMode == OfficeAppMode::Calc) {
        std::wstring infoStr = L"Ô chọn: " + std::wstring(SparseMatrix::colIndexToName(calcView.selectedCol).begin(), SparseMatrix::colIndexToName(calcView.selectedCol).end()) + std::to_wstring(calcView.selectedRow) + L"   |   Sẵn sàng";
        TextOutW(hdc, 200, height - 20, infoStr.c_str(), (int)infoStr.length());
    } else if (ribbon.currentMode == OfficeAppMode::Slides) {
        std::wstring infoStr = L"Slide " + std::to_wstring(slidesView.activeSlideIndex + 1) + L" / " + std::to_wstring(slidesView.presentation.slides.size()) + L"   |   Theme Standard";
        TextOutW(hdc, 200, height - 20, infoStr.c_str(), (int)infoStr.length());
    }

    // Right Zoom Slider Indicator
    std::wstring zoomStr = std::to_wstring(static_cast<int>(zoomLevel * 100)) + L"%  [ -  |||||||||  + ]";
    TextOutW(hdc, width - 180, height - 20, zoomStr.c_str(), (int)zoomStr.length());

    SelectObject(hdc, hOldFont);
    DeleteObject(hFont);
}

void AppWindow::renderDoubleBuffered(HDC hdc, int width, int height) {
    HDC memHDC = CreateCompatibleDC(hdc);
    HBITMAP memBitmap = CreateCompatibleBitmap(hdc, width, height);
    HBITMAP oldBitmap = (HBITMAP)SelectObject(memHDC, memBitmap);

    // Fullscreen Presenter Mode
    if (slidesView.isPresenting) {
        SlidePresenter::renderFullscreenSlide(memHDC, slidesView.presentation.slides[slidesView.activeSlideIndex], width, height);
    } else {
        // 1. Draw Active Viewport (Writer / Calc / Slides)
        if (ribbon.currentMode == OfficeAppMode::Writer) {
            writerView.draw(memHDC, width, height);
        } else if (ribbon.currentMode == OfficeAppMode::Calc) {
            calcView.draw(memHDC, width, height);
        } else if (ribbon.currentMode == OfficeAppMode::Slides) {
            slidesView.draw(memHDC, width, height);
        }

        // 2. Draw Ribbon Bar (Top UI)
        ribbon.draw(memHDC, width, height);

        // 3. Draw Status Bar (Bottom UI)
        drawStatusBar(memHDC, width, height);
    }

    // Copy Double Buffer to Screen
    BitBlt(hdc, 0, 0, width, height, memHDC, 0, 0, SRCCOPY);

    SelectObject(memHDC, oldBitmap);
    DeleteObject(memBitmap);
    DeleteDC(memHDC);
}

LRESULT AppWindow::handleMessage(UINT uMsg, WPARAM wParam, LPARAM lParam) {
    switch (uMsg) {
        case WM_PAINT: {
            PAINTSTRUCT ps;
            HDC hdc = BeginPaint(hWnd, &ps);
            RECT clientRect;
            GetClientRect(hWnd, &clientRect);
            renderDoubleBuffered(hdc, clientRect.right, clientRect.bottom);
            EndPaint(hWnd, &ps);
            return 0;
        }
        case WM_ERASEBKGND:
            return 1; // Prevent flicker in double buffering
        case WM_LBUTTONDOWN: {
            int x = LOWORD(lParam);
            int y = HIWORD(lParam);

            if (!slidesView.isPresenting) {
                if (ribbon.handleClick(x, y, hWnd)) {
                    return 0;
                }
                if (ribbon.currentMode == OfficeAppMode::Writer) {
                    writerView.handleClick(x, y, hWnd);
                } else if (ribbon.currentMode == OfficeAppMode::Calc) {
                    calcView.handleClick(x, y, hWnd);
                } else if (ribbon.currentMode == OfficeAppMode::Slides) {
                    slidesView.handleClick(x, y, hWnd);
                }
            }
            return 0;
        }
        case WM_CHAR: {
            wchar_t ch = static_cast<wchar_t>(wParam);
            if (!slidesView.isPresenting) {
                if (ribbon.currentMode == OfficeAppMode::Writer) {
                    writerView.handleChar(ch, hWnd);
                } else if (ribbon.currentMode == OfficeAppMode::Calc) {
                    calcView.handleChar(ch, hWnd);
                }
            }
            return 0;
        }
        case WM_KEYDOWN: {
            if (slidesView.isPresenting) {
                slidesView.handleKeyDown(wParam, hWnd);
            } else {
                if (wParam == VK_F5 && ribbon.currentMode == OfficeAppMode::Slides) {
                    slidesView.isPresenting = true;
                    InvalidateRect(hWnd, NULL, TRUE);
                    return 0;
                }
                if (ribbon.currentMode == OfficeAppMode::Writer) {
                    writerView.handleKeyDown(wParam, hWnd);
                } else if (ribbon.currentMode == OfficeAppMode::Calc) {
                    calcView.handleKeyDown(wParam, hWnd);
                } else if (ribbon.currentMode == OfficeAppMode::Slides) {
                    slidesView.handleKeyDown(wParam, hWnd);
                }
            }
            return 0;
        }
        case WM_SIZE:
            InvalidateRect(hWnd, NULL, TRUE);
            return 0;
        case WM_DESTROY:
            PostQuitMessage(0);
            return 0;
    }
    return DefWindowProc(hWnd, uMsg, wParam, lParam);
}

} // namespace ZenQOffice
