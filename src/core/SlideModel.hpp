#ifndef GENOFFICE_SLIDE_MODEL_HPP
#ifndef GENOFFICE_SLIDE_MODEL_HPP
#define GENOFFICE_SLIDE_MODEL_HPP

#include <string>
#include <vector>
#include <memory>
#include <cstdint>

namespace GenOffice {

enum class ShapeType { TextBox, Rectangle, Ellipse, Arrow, Line, Image };

struct Transform2D {
    float x = 100.0f;
    float y = 100.0f;
    float width = 200.0f;
    float height = 150.0f;
    float rotation = 0.0f; // degrees
};

struct ShapeNode {
    ShapeType type = ShapeType::Rectangle;
    Transform2D transform;
    std::string text;
    std::wstring fontName = L"Segoe UI";
    float fontSize = 18.0f;
    uint32_t fillColor = 0xE6F0FA; // Light Blue
    uint32_t strokeColor = 0x2B579A; // Dark Blue
    float strokeWidth = 2.0f;
    uint32_t textColor = 0x000000;
    bool selected = false;
};

class SlideNode {
public:
    std::string title = "Untitled Slide";
    uint32_t backgroundColor = 0xFFFFFF;
    std::vector<ShapeNode> shapes;

    void addShape(const ShapeNode& shape) {
        shapes.push_back(shape);
    }
};

class PresentationRoot {
public:
    float canvasWidth = 1920.0f;  // 16:9 HD Canvas
    float canvasHeight = 1080.0f;
    std::string title = "Untitled Presentation.pptx";
    std::vector<SlideNode> slides;

    PresentationRoot() {
        // Create default initial slide
        SlideNode slide1;
        slide1.title = "Slide 1";
        
        ShapeNode titleShape;
        titleShape.type = ShapeType::TextBox;
        titleShape.transform = {360.0f, 250.0f, 1200.0f, 150.0f, 0.0f};
        titleShape.text = "Click to add title";
        titleShape.fontSize = 44.0f;
        titleShape.fillColor = 0xFFFFFF;
        titleShape.strokeWidth = 0.0f;
        titleShape.textColor = 0x222222;

        ShapeNode subShape;
        subShape.type = ShapeType::TextBox;
        subShape.transform = {460.0f, 450.0f, 1000.0f, 100.0f, 0.0f};
        subShape.text = "GenOffice Presentation Subtitle";
        subShape.fontSize = 24.0f;
        subShape.fillColor = 0xFFFFFF;
        subShape.strokeWidth = 0.0f;
        subShape.textColor = 0x666666;

        slide1.addShape(titleShape);
        slide1.addShape(subShape);

        slides.push_back(slide1);
    }
};

} // namespace GenOffice

#endif // GENOFFICE_SLIDE_MODEL_HPP
