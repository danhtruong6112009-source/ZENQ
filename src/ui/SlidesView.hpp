#ifndef GENOFFICE_SLIDES_VIEW_HPP
#define GENOFFICE_SLIDES_VIEW_HPP

#include <windows.h>
#include "../core/SlideModel.hpp"
#include <string>

namespace GenOffice {

class SlidesView {
public:
    PresentationRoot presentation;
    size_t activeSlideIndex = 0;
    int selectedShapeIndex = 0;

    bool isPresenting = false;
    float zoomLevel = 0.5f;

    SlidesView();

    void draw(HDC hdc, int width, int height);
    void handleKeyDown(WPARAM wParam, HWND hWnd);
    void handleClick(int x, int y, HWND hWnd);
    void addSlide();
    void addShape(ShapeType type);

private:
    void drawThumbnailSidebar(HDC hdc, int height);
    void drawTransformHandles(HDC hdc, int x, int y, int w, int h);
};

} // namespace GenOffice

#endif // GENOFFICE_SLIDES_VIEW_HPP
