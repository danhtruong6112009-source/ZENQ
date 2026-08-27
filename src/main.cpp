#include <windows.h>
#include "ui/AppWindow.hpp"

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    // Set process DPI awareness for high-resolution displays
    SetProcessDPIAware();

    GenOffice::AppWindow app;
    if (!app.init(hInstance, nCmdShow)) {
        MessageBoxW(NULL, L"Không thể khởi tạo cửa sổ ứng dụng GenOffice!", L"Lỗi hệ thống", MB_ICONERROR | MB_OK);
        return 1;
    }

    app.runMessageLoop();
    return 0;
}
