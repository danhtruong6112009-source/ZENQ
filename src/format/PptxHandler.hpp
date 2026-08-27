#ifndef GENOFFICE_PPTX_HANDLER_HPP
#ifndef GENOFFICE_PPTX_HANDLER_HPP
#define GENOFFICE_PPTX_HANDLER_HPP

#include "../core/SlideModel.hpp"
#include <string>
#include <fstream>

namespace GenOffice {

class PptxHandler {
public:
    static bool savePptxXml(const std::string& filepath, const PresentationRoot& pres) {
        std::ofstream file(filepath);
        if (!file.is_open()) return false;

        file << "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n";
        file << "<p:sld xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">\n";
        file << "  <p:cSld>\n";
        file << "    <p:spTree>\n";

        for (size_t sIdx = 0; sIdx < pres.slides.size(); ++sIdx) {
            const auto& slide = pres.slides[sIdx];
            for (const auto& shape : slide.shapes) {
                file << "      <p:sp>\n";
                file << "        <p:spPr>\n";
                file << "          <a:xfrm>\n";
                file << "            <a:off x=\"" << static_cast<int>(shape.transform.x) << "\" y=\"" << static_cast<int>(shape.transform.y) << "\"/>\n";
                file << "            <a:ext cx=\"" << static_cast<int>(shape.transform.width) << "\" cy=\"" << static_cast<int>(shape.transform.height) << "\"/>\n";
                file << "          </a:xfrm>\n";
                file << "        </p:spPr>\n";
                file << "        <p:txBody>\n";
                file << "          <a:p><a:r><a:t>" << escapeXml(shape.text) << "</a:t></a:r></a:p>\n";
                file << "        </p:txBody>\n";
                file << "      </p:sp>\n";
            }
        }

        file << "    </p:spTree>\n";
        file << "  </p:cSld>\n";
        file << "</p:sld>\n";
        return true;
    }

private:
    static std::string escapeXml(const std::string& input) {
        std::string res;
        for (char c : input) {
            if (c == '<') res += "&lt;";
            else if (c == '>') res += "&gt;";
            else if (c == '&') res += "&amp;";
            else if (c == '"') res += "&quot;";
            else res += c;
        }
        return res;
    }
};

} // namespace GenOffice

#endif // GENOFFICE_PPTX_HANDLER_HPP
