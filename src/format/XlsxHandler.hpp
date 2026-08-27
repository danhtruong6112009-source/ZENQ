#ifndef GENOFFICE_XLSX_HANDLER_HPP
#ifndef GENOFFICE_XLSX_HANDLER_HPP
#define GENOFFICE_XLSX_HANDLER_HPP

#include "../core/SparseMatrix.hpp"
#include <string>
#include <fstream>
#include <sstream>

namespace GenOffice {

class XlsxHandler {
public:
    static bool saveCsvFile(const std::string& filepath, const SparseMatrix& sheet) {
        std::ofstream file(filepath);
        if (!file.is_open()) return false;

        uint32_t maxR = 1, maxC = 1;
        for (const auto& [key, cell] : sheet.getAllCells()) {
            auto [r, c] = SparseMatrix::decodeKey(key);
            if (r > maxR) maxR = r;
            if (c > maxC) maxC = c;
        }

        for (uint32_t r = 1; r <= maxR; ++r) {
            for (uint32_t c = 1; c <= maxC; ++c) {
                const CellData* cell = sheet.getCell(r, c);
                if (cell) {
                    file << "\"" << escapeCsv(cell->evaluatedStr) << "\"";
                }
                if (c < maxC) file << ",";
            }
            file << "\n";
        }
        return true;
    }

    static bool loadCsvFile(const std::string& filepath, SparseMatrix& sheet) {
        std::ifstream file(filepath);
        if (!file.is_open()) return false;

        sheet.clear();
        std::string line;
        uint32_t r = 1;
        while (std::getline(file, line)) {
            std::stringstream ss(line);
            std::string item;
            uint32_t c = 1;
            while (std::getline(ss, item, ',')) {
                if (!item.empty() && item.front() == '"' && item.back() == '"') {
                    item = item.substr(1, item.length() - 2);
                }
                if (!item.empty()) {
                    sheet.setCellText(r, c, item);
                }
                c++;
            }
            r++;
        }
        return true;
    }

    static bool saveXlsxXml(const std::string& filepath, const SparseMatrix& sheet) {
        std::ofstream file(filepath);
        if (!file.is_open()) return false;

        file << "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n";
        file << "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">\n";
        file << "  <sheetData>\n";

        for (const auto& [key, cell] : sheet.getAllCells()) {
            auto [r, c] = SparseMatrix::decodeKey(key);
            std::string cellRef = SparseMatrix::colIndexToName(c) + std::to_string(r);

            file << "    <row r=\"" << r << "\">\n";
            file << "      <c r=\"" << cellRef << "\" t=\"inlineStr\">\n";
            file << "        <is><t>" << escapeXml(cell.evaluatedStr) << "</t></is>\n";
            if (!cell.formulaStr.empty()) {
                file << "        <f>" << escapeXml(cell.formulaStr.substr(1)) << "</f>\n";
            }
            file << "      </c>\n";
            file << "    </row>\n";
        }

        file << "  </sheetData>\n";
        file << "</worksheet>\n";
        return true;
    }

private:
    static std::string escapeCsv(const std::string& input) {
        std::string res;
        for (char c : input) {
            if (c == '"') res += "\"\"";
            else res += c;
        }
        return res;
    }

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

#endif // GENOFFICE_XLSX_HANDLER_HPP
