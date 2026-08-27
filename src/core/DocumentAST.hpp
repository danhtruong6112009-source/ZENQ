#ifndef GENOFFICE_DOCUMENT_AST_HPP
#define GENOFFICE_DOCUMENT_AST_HPP

#include <string>
#include <vector>
#include <memory>
#include <cstdint>

namespace GenOffice {

enum class Alignment { Left, Center, Right, Justify };

struct RunNode {
    std::string text;
    std::wstring fontName = L"Segoe UI";
    float fontSize = 12.0f;
    bool bold = false;
    bool italic = false;
    bool underline = false;
    uint32_t textColor = 0x000000;
    uint32_t bgColor = 0xFFFFFF;

    RunNode(const std::string& t = "") : text(t) {}
};

struct ParagraphNode {
    Alignment alignment = Alignment::Left;
    float indentLeft = 0.0f;
    float indentRight = 0.0f;
    float indentFirstLine = 0.0f;
    float spaceBefore = 0.0f;
    float spaceAfter = 6.0f;
    float lineSpacing = 1.15f;
    std::vector<RunNode> runs;

    void addRun(const RunNode& run) { runs.push_back(run); }
};

struct SectionNode {
    float pageWidth = 595.0f;   // A4 Width in points (~8.27 in)
    float pageHeight = 842.0f;  // A4 Height in points (~11.69 in)
    float marginTop = 54.0f;    // ~0.75 in
    float marginBottom = 54.0f;
    float marginLeft = 54.0f;
    float marginRight = 54.0f;
    std::string headerText;
    std::string footerText;
    std::vector<ParagraphNode> paragraphs;
};

class DocumentAST {
public:
    std::vector<SectionNode> sections;
    std::string documentTitle = "Untitled Document.docx";

    DocumentAST() {
        sections.emplace_back(); // Default initial section
        sections[0].paragraphs.emplace_back(); // Default initial paragraph
    }

    void clear() {
        sections.clear();
        sections.emplace_back();
        sections[0].paragraphs.emplace_back();
    }
};

} // namespace GenOffice

#endif // GENOFFICE_DOCUMENT_AST_HPP
