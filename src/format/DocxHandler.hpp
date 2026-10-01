#ifndef GENOFFICE_DOCX_HANDLER_HPP
#ifndef GENOFFICE_DOCX_HANDLER_HPP
#define GENOFFICE_DOCX_HANDLER_HPP

#include "../core/DocumentAST.hpp"
#include "../core/PieceTable.hpp"
#include <fstream>
#include <sstream>
#include <string>

namespace GenOffice {

class DocxHandler {
public:
  static bool saveTextFile(const std::string &filepath,
                           const std::string &text) {
    std::ofstream file(filepath);
    if (!file.is_open())
      return false;
    file << text;
    return true;
  }

  static bool loadTextFile(const std::string &filepath, std::string &outText) {
    std::ifstream file(filepath);
    if (!file.is_open())
      return false;
    std::stringstream buffer;
    buffer << file.rdbuf();
    outText = buffer.str();
    return true;
  }

  static bool saveDocxXml(const std::string &filepath, const DocumentAST &doc,
                          const std::string &textContent) {
    std::ofstream file(filepath);
    if (!file.is_open())
      return false;

    file << "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n";
    file << "<w:document "
            "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/"
            "main\">\n";
    file << "  <w:body>\n";

    std::stringstream ss(textContent);
    std::string line;
    while (std::getline(ss, line)) {
      file << "    <w:p>\n";
      file << "      <w:r>\n";
      file << "        <w:t>" << escapeXml(line) << "</w:t>\n";
      file << "      </w:r>\n";
      file << "    </w:p>\n";
    }

    file << "  </w:body>\n";
    file << "</w:document>\n";
    return true;
  }

private:
  static std::string escapeXml(const std::string &input) {
    std::string res;
    for (char c : input) {
      if (c == '<')
        res += "&lt;";
      else if (c == '>')
        res += "&gt;";
      else if (c == '&')
        res += "&amp;";
      else if (c == '"')
        res += "&quot;";
      else
        res += c;
    }
    return res;
  }
};

} // namespace GenOffice

#endif // GENOFFICE_DOCX_HANDLER_HPP
